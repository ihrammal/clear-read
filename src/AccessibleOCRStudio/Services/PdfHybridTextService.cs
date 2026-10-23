using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace AccessibleOCRStudio.Services;

/// <summary>Combines native PDF text and raster OCR in the same page coordinates.</summary>
public sealed class PdfHybridTextService
{
    public sealed record Word(string Text, double Left, double Top, double Right, double Bottom);
    public sealed record Line(IReadOnlyList<Word> Words, string? NativeText = null)
    {
        public double Top => Words.Min(w => w.Top);
        public double Left => Words.Min(w => w.Left);
        public string Text => NativeText ?? string.Join(" ", Words.Select(w => w.Text));
    }
    public sealed record Page(double Width, double Height, IReadOnlyList<Line> Lines, int Rotation = 0);

    public static IReadOnlyList<Page> ParseLayout(string xml)
    {
        // Poppler includes an XHTML doctype. Never fetch its external DTD.
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
        var document = XDocument.Load(reader);
        return document.Descendants().Where(e => e.Name.LocalName == "page").Select(page =>
            new Page(Number(page, "width"), Number(page, "height"),
                page.Descendants().Where(e => e.Name.LocalName == "line").Select(line =>
                    new Line(line.Elements().Where(e => e.Name.LocalName == "word" && !string.IsNullOrWhiteSpace(e.Value))
                        .Select(word => new Word(word.Value, Number(word, "xMin"), Number(word, "yMin"),
                            Number(word, "xMax"), Number(word, "yMax"))).ToArray()))
                    .Where(line => line.Words.Count > 0).ToArray())).ToArray();
    }

    public static string Merge(Page native, string nativeText, string tsv, int pixelWidth, int pixelHeight)
    {
        if (native.Width <= 0 || native.Height <= 0 || pixelWidth <= 0 || pixelHeight <= 0)
            throw new InvalidOperationException("Invalid PDF page dimensions.");
        var canvasWidth = native.Rotation is 90 or 270 ? native.Height : native.Width;
        var canvasHeight = native.Rotation is 90 or 270 ? native.Width : native.Height;
        Word Upright(Word word) => native.Rotation switch
        {
            90 => word with { Left = word.Top, Top = canvasWidth - word.Right, Right = word.Bottom, Bottom = canvasWidth - word.Left },
            180 => word with { Left = canvasWidth - word.Right, Top = canvasHeight - word.Bottom, Right = canvasWidth - word.Left, Bottom = canvasHeight - word.Top },
            270 => word with { Left = canvasHeight - word.Bottom, Top = word.Left, Right = canvasHeight - word.Top, Bottom = word.Right },
            _ => word
        };
        var nativeLines = native.Lines.Select(line => new Line(line.Words.Select(Upright).ToArray()))
            .OrderBy(line => line.Top).ThenBy(line => line.Left).ToArray();
        var nativeWords = nativeLines.SelectMany(line => line.Words).ToArray();
        // Use Poppler's logical text output, not its glyph-level bounding-box
        // strings. This preserves Arabic shaping/order and exact native Unicode.
        var exactLines = nativeText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).ToArray();
        IReadOnlyList<Line> exactNative;
        if (exactLines.Length == nativeLines.Length)
            exactNative = nativeLines.Select((line, i) => line with { NativeText = exactLines[i] }).ToArray();
        else
            // A layout line may combine several columns or paragraphs. Preserve
            // all native text as one block rather than reconstructing its glyphs.
            exactNative = new[] { new Line(nativeWords, string.Join(Environment.NewLine, exactLines)) };
        var groups = new Dictionary<string, List<Word>>();
        foreach (var row in tsv.Split('\n').Skip(1))
        {
            var columns = row.TrimEnd('\r').Split('\t');
            if (columns.Length < 12 || columns[0] != "5" || string.IsNullOrWhiteSpace(columns[11])) continue;
            // The OCR image has already had its PDF page rotation undone.
            var left = Parse(columns[6]) * native.Width / pixelWidth;
            var top = Parse(columns[7]) * native.Height / pixelHeight;
            var right = left + Parse(columns[8]) * native.Width / pixelWidth;
            var bottom = top + Parse(columns[9]) * native.Height / pixelHeight;
            var word = new Word(columns[11], left, top, right, bottom);
            // Suppress OCR at native text positions, even when OCR spelling differs.
            // Repeated words at other positions must remain in the output.
            if (nativeWords.Any(existing => Covers(existing, word)) || nativeLines.Any(line =>
                Covers(new Word(string.Empty, line.Words.Min(w => w.Left), line.Words.Min(w => w.Top),
                    line.Words.Max(w => w.Right), line.Words.Max(w => w.Bottom)), word))) continue;
            var key = string.Join("/", columns.Skip(1).Take(4));
            if (!groups.TryGetValue(key, out var words)) groups[key] = words = new();
            words.Add(word);
        }
        var lines = exactNative.Concat(groups.Values.Select(words => new Line(words)))
            .OrderBy(line => line.Top).ThenBy(line => line.Left);
        return string.Join(Environment.NewLine, lines.Select(line => line.Text));
    }

    private static bool Covers(Word existing, Word candidate)
    {
        const double tolerance = 2;
        var centerX = (candidate.Left + candidate.Right) / 2;
        var centerY = (candidate.Top + candidate.Bottom) / 2;
        if (centerX >= existing.Left - tolerance && centerX <= existing.Right + tolerance &&
            centerY >= existing.Top - tolerance && centerY <= existing.Bottom + tolerance) return true;
        var intersection = Math.Max(0, Math.Min(existing.Right, candidate.Right) - Math.Max(existing.Left, candidate.Left)) *
            Math.Max(0, Math.Min(existing.Bottom, candidate.Bottom) - Math.Max(existing.Top, candidate.Top));
        var area = (candidate.Right - candidate.Left) * (candidate.Bottom - candidate.Top);
        return area > 0 && intersection / area >= 0.5;
    }

    private static double Number(XElement element, string name) => Parse(element.Attribute(name)?.Value ?? throw new FormatException($"Missing PDF {name}."));
    private static double Parse(string value)
    {
        var number = double.Parse(value, CultureInfo.InvariantCulture);
        return double.IsFinite(number) ? number : throw new FormatException("Invalid PDF coordinate.");
    }
}

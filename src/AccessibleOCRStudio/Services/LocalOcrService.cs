using System.Diagnostics;
using System.Text;
using System.Windows.Media.Imaging;

namespace AccessibleOCRStudio.Services;

public sealed class LocalOcrService
{
    private readonly ImagePreprocessingService _preprocessing = new();
    public async Task<string> RecognizeAsync(string input, AppSettings settings, IProgress<string>? progress = null)
    {
        var ext = Path.GetExtension(input).ToLowerInvariant();
        if (ext == ".pdf") return await RecognizePdfAsync(input, settings, progress);
        progress?.Report("Recognizing image.");
        return await RecognizeImageAsync(input, settings);
    }


    public async Task<RecognitionDocument> RecognizeWithConfidenceAsync(string input, AppSettings settings, IProgress<string>? progress = null)
    {
        var document = new RecognitionDocument();
        document.PlainText = await RecognizeAsync(input, settings, progress);
        var ext = Path.GetExtension(input).ToLowerInvariant();
        if (ext != ".pdf")
        {
            try
            {
                var prepared = await _preprocessing.PrepareAsync(input, settings);
                try
                {
                    var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "ocr", "tesseract.exe"), "tesseract");
                    var psi = NewProcess(exe);
                    psi.ArgumentList.Add(prepared.Path); psi.ArgumentList.Add("stdout");
                    psi.ArgumentList.Add("-l"); psi.ArgumentList.Add(settings.OcrLanguages);
                    AddRecognitionOptions(psi, settings);
                    psi.ArgumentList.Add("tsv");
                    var tsv = await RunCheckedAsync(psi, "The OCR confidence pass failed.", true);
                    foreach (var line in tsv.Split('\n').Skip(1))
                    {
                        var c = line.TrimEnd('\r').Split('\t');
                        if (c.Length < 12 || !int.TryParse(c[0], out var level) || level != 5 || string.IsNullOrWhiteSpace(c[11])) continue;
                        _ = int.TryParse(c[1], out var page); _ = int.TryParse(c[4], out var lineNo); _ = int.TryParse(c[6], out var left); _ = int.TryParse(c[7], out var top); _ = int.TryParse(c[8], out var width); _ = int.TryParse(c[9], out var height); _ = double.TryParse(c[10], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var conf);
                        document.Words.Add(new OcrWord(c[11], conf, Math.Max(1,page), lineNo, left, top, width, height));
                    }
                }
                finally { if (prepared.Temporary) try { File.Delete(prepared.Path); } catch { } }
            } catch { /* Confidence is supplementary; plain OCR remains usable. */ }
        }
        document.Blocks.AddRange(new DocumentStructureService().Analyze(document.PlainText));
        return document;
    }

    private async Task<string> RecognizePdfAsync(string input, AppSettings settings, IProgress<string>? progress)
    {
        string[]? digitalPages = null;
        HashSet<int>? imagePages = null;
        IReadOnlyList<PdfHybridTextService.Page>? layout = null;
        if (settings.PreferDigitalPdfText)
        {
            var digital = await TryExtractDigitalPdfTextAsync(input);
            if (!string.IsNullOrWhiteSpace(digital))
            {
                digitalPages = digital.Split('\f');
                if (digital.EndsWith('\f')) digitalPages = digitalPages[..^1];
                imagePages = await TryGetPdfImagePagesAsync(input);
                if (digitalPages.Length > 0 && digitalPages.All(HasText) && imagePages is { Count: 0 })
                {
                    progress?.Report("Digital PDF text detected. OCR was not required.");
                    return JoinPages(digitalPages);
                }
                progress?.Report("Digital PDF text detected. Checking pages for scanned content.");
                if (imagePages is null || imagePages.Count > 0) layout = await TryExtractPdfLayoutAsync(input);
            }
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "AccessibleOCRStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var prefix = Path.Combine(tempDir, "page");
            var renderer = FindExecutable(Path.Combine(AppContext.BaseDirectory, "pdf", "pdftoppm.exe"), "pdftoppm");
            var psi = NewProcess(renderer);
            psi.ArgumentList.Add("-r"); psi.ArgumentList.Add("300");
            psi.ArgumentList.Add("-cropbox");
            psi.ArgumentList.Add("-png"); psi.ArgumentList.Add(input); psi.ArgumentList.Add(prefix);
            await RunCheckedAsync(psi, "Unable to render the PDF for OCR.");

            var pages = Directory.GetFiles(tempDir, "page-*.png").OrderBy(PageNumberFromName).ToArray();
            if (pages.Length == 0) throw new InvalidOperationException("No PDF pages were rendered.");

            var result = new StringBuilder();
            for (var i = 0; i < pages.Length; i++)
            {
                if (i > 0) result.AppendLine().AppendLine($"--- Page {i + 1} ---").AppendLine();
                var hasDigital = digitalPages is not null && i < digitalPages.Length && HasText(digitalPages[i]);
                if (hasDigital && imagePages is not null && !imagePages.Contains(i + 1))
                {
                    progress?.Report($"Using digital PDF text for page {i + 1} of {pages.Length}.");
                    result.Append(digitalPages![i].Trim());
                }
                else if (hasDigital && layout is not null && i < layout.Count && layout[i].Lines.Count > 0)
                {
                    progress?.Report($"Recognizing scanned content and preserving digital PDF text for page {i + 1} of {pages.Length}.");
                    result.Append(await RecognizeHybridPageAsync(pages[i], layout[i], digitalPages![i], settings));
                }
                else
                {
                    progress?.Report($"Recognizing page {i + 1} of {pages.Length}.");
                    result.Append(await RecognizeImageAsync(pages[i], settings));
                }
            }
            return result.ToString();
        }
        finally { try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { } }
    }

    private static bool HasText(string text) => text.Any(char.IsLetterOrDigit);
    private static string JoinPages(IEnumerable<string> pages) => string.Join(Environment.NewLine + Environment.NewLine, pages.Select((text, i) => i == 0 ? text.Trim() : $"--- Page {i + 1} ---{Environment.NewLine}{text.Trim()}"));

    private async Task<string> TryExtractDigitalPdfTextAsync(string input)
    {
        try
        {
            var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "pdf", "pdftotext.exe"), "pdftotext");
            var psi = NewProcess(exe);
            psi.ArgumentList.Add("-layout"); psi.ArgumentList.Add("-enc"); psi.ArgumentList.Add("UTF-8");
            psi.ArgumentList.Add("-cropbox");
            psi.ArgumentList.Add(input); psi.ArgumentList.Add("-");
            return await RunCheckedAsync(psi, "Digital PDF text extraction failed.", true);
        }
        catch { return string.Empty; }
    }

    private async Task<HashSet<int>?> TryGetPdfImagePagesAsync(string input)
    {
        try
        {
            var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "pdf", "pdfimages.exe"), "pdfimages");
            var psi = NewProcess(exe);
            psi.ArgumentList.Add("-list"); psi.ArgumentList.Add(input);
            var output = await RunCheckedAsync(psi, "Unable to inspect PDF images.", true);
            if (!output.Contains("page", StringComparison.Ordinal) || !output.Contains("type", StringComparison.Ordinal)) return null;
            var pages = new HashSet<int>();
            foreach (var row in output.Split('\n'))
            {
                var columns = row.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length >= 3 && int.TryParse(columns[0], out var page) && int.TryParse(columns[1], out _)) pages.Add(page);
            }
            return pages;
        }
        catch { return null; } // Unknown image coverage must not bypass OCR.
    }

    private async Task<IReadOnlyList<PdfHybridTextService.Page>?> TryExtractPdfLayoutAsync(string input)
    {
        try
        {
            var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "pdf", "pdftotext.exe"), "pdftotext");
            var psi = NewProcess(exe);
            psi.ArgumentList.Add("-bbox-layout"); psi.ArgumentList.Add("-cropbox");
            psi.ArgumentList.Add("-enc"); psi.ArgumentList.Add("UTF-8");
            psi.ArgumentList.Add(input); psi.ArgumentList.Add("-");
            var layout = PdfHybridTextService.ParseLayout(await RunCheckedAsync(psi, "Unable to extract PDF text positions.", true));
            var info = NewProcess(FindExecutable(Path.Combine(AppContext.BaseDirectory, "pdf", "pdfinfo.exe"), "pdfinfo"));
            info.ArgumentList.Add("-f"); info.ArgumentList.Add("1");
            info.ArgumentList.Add("-l"); info.ArgumentList.Add(layout.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
            info.ArgumentList.Add(input);
            var metadata = await RunCheckedAsync(info, "Unable to inspect PDF page rotation.", true);
            var rotations = System.Text.RegularExpressions.Regex.Matches(metadata, @"Page\s+(\d+)\s+rot:\s+(-?\d+)")
                .ToDictionary(match => int.Parse(match.Groups[1].Value), match => ((int.Parse(match.Groups[2].Value) % 360) + 360) % 360);
            if (rotations.Count != layout.Count) return null;
            return layout.Select((page, i) => page with { Rotation = rotations[i + 1] }).ToArray();
        }
        catch { return null; } // Full-page OCR remains available if layout extraction fails.
    }

    private async Task<string> RecognizeHybridPageAsync(string image, PdfHybridTextService.Page native, string nativeText, AppSettings settings)
    {
        BitmapSource bitmap;
        using (var stream = File.OpenRead(image))
            bitmap = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        // PDF rotation is known, so undo it before OCR rather than depending on
        // orientation detection on a sparse page. Keep deskew disabled here so
        // native text and OCR geometry remain aligned.
        if (native.Rotation != 0)
        {
            bitmap = new TransformedBitmap(bitmap, new System.Windows.Media.RotateTransform(-native.Rotation));
            var upright = Path.ChangeExtension(image, ".upright.png");
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var output = File.Create(upright)) encoder.Save(output);
            image = upright;
        }
        var pixelWidth = bitmap.PixelWidth;
        var pixelHeight = bitmap.PixelHeight;
        var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "ocr", "tesseract.exe"), "tesseract");
        var psi = NewProcess(exe);
        psi.ArgumentList.Add(image); psi.ArgumentList.Add("stdout");
        psi.ArgumentList.Add("-l"); psi.ArgumentList.Add(settings.OcrLanguages);
        AddRecognitionOptions(psi, settings);
        psi.ArgumentList.Add("tsv");
        var tsv = await RunCheckedAsync(psi, "The OCR engine failed while recognizing scanned PDF content.", true);
        return PdfHybridTextService.Merge(native, nativeText, tsv, pixelWidth, pixelHeight);
    }

    private async Task<string> RecognizeImageAsync(string input, AppSettings settings)
    {
        var prepared = await _preprocessing.PrepareAsync(input, settings);
        try
        {
            var exe = FindExecutable(Path.Combine(AppContext.BaseDirectory, "ocr", "tesseract.exe"), "tesseract");
            var psi = NewProcess(exe);
            psi.ArgumentList.Add(prepared.Path); psi.ArgumentList.Add("stdout");
            psi.ArgumentList.Add("-l"); psi.ArgumentList.Add(settings.OcrLanguages);
            AddRecognitionOptions(psi, settings);
            return await RunCheckedAsync(psi, "The OCR engine failed.", true);
        }
        finally { if (prepared.Temporary) try { File.Delete(prepared.Path); } catch { } }
    }

    internal static void AddRecognitionOptions(ProcessStartInfo psi, AppSettings settings)
    {
        psi.ArgumentList.Add("--psm");
        psi.ArgumentList.Add(settings.AutoRotate ? "1" : settings.OcrMode == "fast" ? "6" : "3");
        if (settings.PreserveTextLayout)
        { psi.ArgumentList.Add("-c"); psi.ArgumentList.Add("preserve_interword_spaces=1"); }
        // Connected Arabic letter shapes can be misclassified as an entire noise row.
        if (settings.OcrLanguages.Split('+').Contains("ara", StringComparer.OrdinalIgnoreCase))
        {
            psi.ArgumentList.Add("-c"); psi.ArgumentList.Add("textord_noise_rejrows=0");
        }
    }

    private static ProcessStartInfo NewProcess(string exe) => new(exe)
    { RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8, UseShellExecute = false, CreateNoWindow = true };

    private static async Task<string> RunCheckedAsync(ProcessStartInfo psi, string message, bool returnStdout = false)
    {
        using var p = Process.Start(psi) ?? throw new InvalidOperationException(message);
        var stdoutTask = p.StandardOutput.ReadToEndAsync(); var stderrTask = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync(); var stdout = await stdoutTask; var stderr = await stderrTask;
        if (p.ExitCode != 0) throw new InvalidOperationException($"{message}\n{stderr}".Trim());
        return returnStdout ? stdout : string.Empty;
    }

    private static string FindExecutable(string bundled, string fallback) => File.Exists(bundled) ? bundled : fallback;
    private static int PageNumberFromName(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path); var tail = name[(name.LastIndexOf('-') + 1)..];
        return int.TryParse(tail, out var n) ? n : int.MaxValue;
    }
}


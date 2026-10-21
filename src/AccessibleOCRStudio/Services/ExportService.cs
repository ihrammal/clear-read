using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace AccessibleOCRStudio.Services;

public sealed class ExportService
{
    private readonly DocumentStructureService _structure = new();

    public Task SaveTextAsync(string path, string text) => File.WriteAllTextAsync(path, text, new UTF8Encoding(true));

    public async Task SaveHtmlAsync(string path, string text, string title = "Recognized document", bool preserveLayout = false)
    {
        var encTitle = System.Net.WebUtility.HtmlEncode(title);
        var body = new StringBuilder();
        var inList = false;
        var blocks = _structure.Analyze(text, preserveWhitespace: preserveLayout);
        foreach (var block in blocks)
        {
            var content = System.Net.WebUtility.HtmlEncode(block.Text);
            var dir = block.RightToLeft ? "rtl" : "auto";
            var lang = LanguageTag(block.Text, block.RightToLeft);
            if (block.Kind == "ListItem")
            {
                if (!inList) { body.AppendLine("<ul>"); inList = true; }
                var item = System.Text.RegularExpressions.Regex.Replace(block.Text, @"^\s*([-*•]|\d+[.)])\s+", "");
                body.AppendLine($"<li dir=\"{dir}\" lang=\"{lang}\">{System.Net.WebUtility.HtmlEncode(item)}</li>");
                continue;
            }
            if (inList) { body.AppendLine("</ul>"); inList = false; }
            body.Append(block.Kind switch
            {
                "Heading" => $"<h2 dir=\"{dir}\" lang=\"{lang}\">{content}</h2>\n",
                _ => $"<p dir=\"{dir}\" lang=\"{lang}\">{content}</p>\n"
            });
        }
        if (inList) body.AppendLine("</ul>");
        var documentLang = blocks.Count == 0
            ? "en"
            : blocks.GroupBy(b => LanguageTag(b.Text, b.RightToLeft))
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        var html = $"<!doctype html><html lang=\"{documentLang}\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>{encTitle}</title><style>p,h2,li{{white-space:pre-wrap;tab-size:4}}</style></head><body><main><h1>{encTitle}</h1>{body}</main></body></html>";
        await File.WriteAllTextAsync(path, html, new UTF8Encoding(false));
    }

    public Task SaveMarkdownAsync(string path, string text, string title = "Recognized document", bool preserveLayout = false)
    {
        var safeTitle = title.Replace("#", string.Empty).Trim();
        return File.WriteAllTextAsync(path, $"# {safeTitle}\n\n{text}", new UTF8Encoding(false));
    }

    public void SaveAccessibleDocx(string path, string text, string title = "Recognized document", bool preserveLayout = false)
    {
        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        AddStyles(main);
        var body = main.Document.AppendChild(new Body());
        AddParagraph(body, title, "Title", ContainsArabic(title));

        foreach (var block in _structure.Analyze(text, preserveWhitespace: preserveLayout))
        {
            var style = block.Kind == "Heading" ? "Heading1" : null;
            AddParagraph(body, block.Text, style, block.RightToLeft);
        }
        main.Document.Save();
    }

    private static void AddParagraph(Body body, string text, string? style, bool rtl)
    {
        var p = body.AppendChild(new Paragraph());
        var pp = new ParagraphProperties();
        if (!string.IsNullOrWhiteSpace(style)) pp.Append(new ParagraphStyleId { Val = style });
        if (rtl) pp.Append(new BiDi());
        if (pp.HasChildren) p.ParagraphProperties = pp;
        var run = new Run();
        var runProps = new RunProperties();
        if (rtl) runProps.Append(new RightToLeftText());
        runProps.AddChild(rtl ? new Languages { Val = "ar-SA", Bidi = "ar-SA" }
            : ContainsFrench(text) ? new Languages { Val = "fr-FR" }
            : new Languages { Val = "en-US" });
        run.RunProperties = runProps;
        run.Append(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        p.Append(run);
    }

    private static void AddStyles(MainDocumentPart main)
    {
        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();
        styles.Append(MakeStyle("Normal", "Normal", false, 22));
        styles.Append(MakeStyle("Title", "Title", true, 32));
        styles.Append(MakeStyle("Heading1", "Heading 1", true, 28));
        stylesPart.Styles = styles;
        stylesPart.Styles.Save();
    }

    private static Style MakeStyle(string id, string name, bool bold, int halfPoints)
    {
        var style = new Style { Type = StyleValues.Paragraph, StyleId = id, Default = id == "Normal" };
        style.Append(new StyleName { Val = name });
        var runProps = new StyleRunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = halfPoints.ToString() });
        style.Append(runProps);
        return style;
    }

    private static bool ContainsArabic(string s) => s.Any(c =>
        (c >= '\u0600' && c <= '\u06FF') || (c >= '\u0750' && c <= '\u077F') || (c >= '\u08A0' && c <= '\u08FF'));

    private static bool ContainsFrench(string s) => s.Any(c => c is
        'é' or 'è' or 'ê' or 'ë' or 'à' or 'â' or 'ç' or 'î' or 'ï' or 'ô' or 'ù' or 'û' or 'ü' or 'œ' or 'æ' or 'É' or 'È' or 'Ê' or 'À' or 'Â' or 'Ç' or 'Î' or 'Ï' or 'Ô' or 'Ù' or 'Û' or 'Ü');

    private static string LanguageTag(string s, bool rightToLeft) => rightToLeft || ContainsArabic(s) ? "ar" : ContainsFrench(s) ? "fr" : "en";
}


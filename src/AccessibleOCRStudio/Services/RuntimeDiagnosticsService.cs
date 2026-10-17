namespace AccessibleOCRStudio.Services;

public sealed record RuntimeDiagnosticResult(bool CoreOcrReady, string Summary, string Details);

public sealed class RuntimeDiagnosticsService
{
    public RuntimeDiagnosticResult Check()
    {
        var baseDir = AppContext.BaseDirectory;
        var items = new List<(string Name, bool Ok, string Path)>();
        void Add(string name, string bundled, string fallback)
        {
            var p = Path.Combine(baseDir, bundled);
            var ok = File.Exists(p) || CommandOnPath(fallback);
            items.Add((name, ok, File.Exists(p) ? p : fallback));
        }
        Add("Tesseract OCR", Path.Combine("ocr","tesseract.exe"), "tesseract.exe");
        Add("PDF renderer", Path.Combine("pdf","pdftoppm.exe"), "pdftoppm.exe");
        Add("PDF text extractor", Path.Combine("pdf","pdftotext.exe"), "pdftotext.exe");
        Add("PDF image inspector", Path.Combine("pdf","pdfimages.exe"), "pdfimages.exe");
        Add("PDF page information", Path.Combine("pdf","pdfinfo.exe"), "pdfinfo.exe");
        Add("PDF merger", Path.Combine("pdf","pdfunite.exe"), "pdfunite.exe");
        Add("Image preprocessing", Path.Combine("image", "magick.exe"), "magick.exe");
        var tess = Path.Combine(baseDir, "ocr", "tessdata");
        foreach (var lang in new[] { "ara", "eng", "fra", "osd" })
            items.Add(($"OCR language {lang}", File.Exists(Path.Combine(tess, lang + ".traineddata")), Path.Combine(tess, lang + ".traineddata")));
        var core = items.Where(i => i.Name == "Tesseract OCR" || i.Name.StartsWith("OCR language")).All(i => i.Ok);
        var details = string.Join(Environment.NewLine, items.Select(i => $"{(i.Ok ? "Available" : "Missing")}: {i.Name}. {i.Path}"));
        return new(core, core ? "Local OCR runtime is ready." : "Local OCR runtime is incomplete. Open Help, Runtime Diagnostics for details.", details);
    }
    private static bool CommandOnPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Any(d => File.Exists(Path.Combine(d.Trim('"'), name)));
    }
}

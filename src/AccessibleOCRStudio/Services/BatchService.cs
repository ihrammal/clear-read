namespace AccessibleOCRStudio.Services;

public sealed class BatchService
{
    private readonly LocalOcrService _ocr = new();
    private readonly ExportService _export = new();

    public async Task<int> ConvertToTextAsync(IEnumerable<string> files, string outputFolder, AppSettings settings, IProgress<string>? progress = null)
    {
        Directory.CreateDirectory(outputFolder);
        var list = files.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var file = list[i];
            progress?.Report($"Batch file {i + 1} of {list.Count}: {Path.GetFileName(file)}");
            var text = await _ocr.RecognizeAsync(file, settings, progress);
            var output = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(file) + ".txt");
            var suffix = 2;
            while (File.Exists(output))
                output = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(file)} ({suffix++}).txt");
            await _export.SaveTextAsync(output, text);
        }
        return list.Count;
    }
}

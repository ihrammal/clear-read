namespace AccessibleOCRStudio.Services;

public sealed class FolderBatchService
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp" };
    public string[] FindSupportedFiles(string folder, bool recursive = false) => Directory.EnumerateFiles(folder, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Where(p => Extensions.Contains(Path.GetExtension(p))).OrderBy(p => p).ToArray();
}

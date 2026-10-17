using System.Security.Cryptography;
using System.Text;

namespace AccessibleOCRStudio.Services;

public sealed class CredentialService
{
    private readonly string Folder;
    public CredentialService(string? folder = null)
    {
        Folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clear Read", "credentials");
        if (folder is not null) return;
        var legacyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Accessible OCR Studio", "credentials");
        MigrateFromLegacy(legacyFolder, Folder);
    }

    public static void MigrateFromLegacy(string legacyFolder, string newFolder)
    {
        if (Directory.Exists(newFolder) || !Directory.Exists(legacyFolder)) return;
        Directory.CreateDirectory(newFolder);
        foreach (var file in Directory.GetFiles(legacyFolder)) File.Copy(file, Path.Combine(newFolder, Path.GetFileName(file)));
    }
    public void Save(string provider, string apiKey)
    {
        Directory.CreateDirectory(Folder);
        var protectedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(Path.Combine(Folder, Safe(provider) + ".bin"), protectedBytes);
    }
    public string? Load(string provider)
    {
        var path = Path.Combine(Folder, Safe(provider) + ".bin");
        if (!File.Exists(path)) return null;
        try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser)); }
        catch { return null; }
    }
    public void Delete(string provider) { var p = Path.Combine(Folder, Safe(provider) + ".bin"); if (File.Exists(p)) File.Delete(p); }
    private static string Safe(string value) => string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();
}

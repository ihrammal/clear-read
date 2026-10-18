using System.Text.Json;

namespace AccessibleOCRStudio.Services;

public sealed class AppSettings
{
    public string GitHubRepositoryUrl { get; set; } = "https://github.com/ihrammal/clear-read";
    public string MessageLanguage { get; set; } = "auto";
    public bool PlayStartupSound { get; set; } = true;
    public bool PlayExitSound { get; set; } = true;
    public bool ShowWelcomeMessage { get; set; } = true;
    public bool AnnounceOcrProgress { get; set; } = true;
    public bool CheckUpdatesOnStartup { get; set; } = true;
    public bool ScreenReaderOptimization { get; set; } = true;
    public bool AccessibleReadingMode { get; set; } = true;
    public bool FollowWindowsHighContrast { get; set; } = true;
    public bool PreferDigitalPdfText { get; set; } = true;
    public bool Deskew { get; set; } = true;
    public bool AutoRotate { get; set; } = true;
    public bool Grayscale { get; set; } = false;
    public string OcrLanguages { get; set; } = "ara+eng+fra";
    public string OcrMode { get; set; } = "accurate";
    public bool OfflineOnlyMode { get; set; } = true;
    public bool AllowApiDocumentContent { get; set; } = false;
    public bool AllowApiOcrCorrection { get; set; } = false;
    public bool AllowImageDescription { get; set; } = false;
    public bool AllowTableReconstruction { get; set; } = false;
    public double LowConfidenceThreshold { get; set; } = 70;
    public string GeminiModel { get; set; } = "gemini-3.5-flash-lite";
    public string GroqModel { get; set; } = "openai/gpt-oss-20b";
    public string OpenAiModel { get; set; } = OpenAiCorrectionService.DefaultModel;
    public string ApiProvider { get; set; } = "None";
    public string UpdateFeedUrl { get; set; } = string.Empty;

    private static string AppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clear Read");

    private static string LegacyAppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Accessible OCR Studio");

    private static string SettingsPath => Path.Combine(AppDataRoot, "settings.json");

    public static void MigrateFromLegacy(string legacyDirectory, string newDirectory, string fileName)
    {
        var legacyFile = Path.Combine(legacyDirectory, fileName);
        if (!File.Exists(legacyFile)) return;
        var newFile = Path.Combine(newDirectory, fileName);
        if (File.Exists(newFile)) return;
        Directory.CreateDirectory(newDirectory);
        File.Copy(legacyFile, newFile);
    }

    public static AppSettings Load(string? path = null)
    {
        path ??= SettingsPath;
        if (!File.Exists(path)) MigrateFromLegacy(LegacyAppDataRoot, AppDataRoot, "settings.json");
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new();
            if (string.IsNullOrWhiteSpace(settings.GitHubRepositoryUrl)) settings.GitHubRepositoryUrl = new AppSettings().GitHubRepositoryUrl;
            return settings;
        }
        catch { return new(); }
    }
    public void Save(string? path = null)
    {
        path ??= SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}



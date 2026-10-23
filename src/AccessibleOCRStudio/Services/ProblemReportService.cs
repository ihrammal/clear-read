using System.Globalization;
using System.Text;

namespace AccessibleOCRStudio.Services;

public static class ProblemReportService
{
    public const string SupportEmail = "iman.rammal@gmail.com";
    public static string Version => typeof(ProblemReportService).Assembly.GetName().Version?.ToString(3) ?? "unknown";

    public static string Create(string summary, string steps, string expected, string actual,
        bool includeDiagnostics, AppSettings settings, DateTimeOffset createdAt)
    {
        var report = new StringBuilder();
        report.AppendLine("TextBridge problem report");
        report.AppendLine($"Application version: {Version}");
        report.AppendLine($"Created (UTC): {createdAt.ToUniversalTime():O}");
        report.AppendLine();
        foreach (var section in new[] { ("Summary", summary), ("Steps to reproduce", steps),
            ("Expected behavior", expected), ("Actual behavior", actual) })
        {
            report.AppendLine(section.Item1 + ":");
            report.AppendLine(section.Item2.Trim());
            report.AppendLine();
        }
        if (includeDiagnostics)
        {
            report.AppendLine("Optional basic diagnostics:");
            report.AppendLine($"Windows: {Environment.OSVersion.VersionString}");
            report.AppendLine($"64-bit process: {Environment.Is64BitProcess}");
            report.AppendLine($".NET: {Environment.Version}");
            report.AppendLine($"Windows UI language: {CultureInfo.CurrentUICulture.Name}");
            var profile = new[] { "ara", "eng", "fra", "ara+eng", "ara+fra", "eng+fra", "ara+eng+fra" }.Contains(settings.OcrLanguages) ? settings.OcrLanguages : "Custom (value omitted)";
            report.AppendLine($"OCR profile: {profile}");
            report.AppendLine(new RuntimeDiagnosticsService().Check().Summary);
        }
        return report.ToString();
    }

    public static Uri? CreateGitHubDraft(string repositoryUrl, string summary, string report)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var repository) ||
            repository.Scheme != "https" || !string.Equals(repository.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            repository.Port != 443 || repository.UserInfo.Length != 0 || repository.Query.Length != 0 || repository.Fragment.Length != 0) return null;
        var parts = repository.AbsolutePath.Trim('/').Split('/');
        if (parts.Length != 2 || parts.Any(p => p is "." or ".." || !System.Text.RegularExpressions.Regex.IsMatch(p, @"^[A-Za-z0-9_.-]+$"))) return null;
        var title = summary.Replace("\r", " ").Replace("\n", " ").Trim();
        if (title.Length > 80) title = title[..80];
        if (title.Length > 0 && char.IsHighSurrogate(title[^1])) title = title[..^1];
        return new Uri($"https://github.com/{parts[0]}/{parts[1]}/issues/new?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(report)}");
    }

    public static Uri CreateEmailDraft(string summary, string report)
    {
        var title = summary.Replace("\r", " ").Replace("\n", " ").Trim();
        if (title.Length > 80) title = title[..80];
        if (title.Length > 0 && char.IsHighSurrogate(title[^1])) title = title[..^1];
        var subject = $"TextBridge {Version} problem: {title}";
        return new Uri($"mailto:{SupportEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(report)}");
    }
}



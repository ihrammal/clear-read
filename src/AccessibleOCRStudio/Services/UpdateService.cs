using System.Net.Http.Headers;
using System.Text.Json;

namespace AccessibleOCRStudio.Services;

public sealed record UpdateCheckResult(bool Configured, bool UpdateAvailable, string CurrentVersion, string? LatestVersion, string? ReleaseUrl, string Message);

public sealed class UpdateService
{
    private readonly HttpClient _http = new();

    public async Task<UpdateCheckResult> CheckAsync(string? feedUrl, string currentVersion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(feedUrl))
            return new(false, false, currentVersion, null, null, "Update checking is not configured yet. A GitHub release feed will be added before the public release.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AccessibleOCRStudio", currentVersion));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using var response = await _http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() : null;
            if (string.IsNullOrWhiteSpace(tag)) return new(true, false, currentVersion, null, url, "The update service returned no version number.");
            var normalized = tag.Trim().TrimStart('v', 'V');
            var available = Version.TryParse(normalized, out var latest) && Version.TryParse(currentVersion, out var current) && latest > current;
            return new(true, available, currentVersion, normalized, url, available ? $"Clear Read {normalized} is available." : "You are using the latest available version.");
        }
        catch (Exception ex)
        {
            return new(true, false, currentVersion, null, null, $"Unable to check for updates. {ex.Message}");
        }
    }
}

namespace AccessibleOCRStudio.Services;

// Provider-neutral configuration. API keys are intentionally not written to settings.json.
// CredentialService stores per-user secrets with Windows DPAPI.
public sealed class ApiProviderSettings
{
    public string Provider { get; set; } = "None";
    public string Model { get; set; } = string.Empty;
    public bool AllowOcrCorrection { get; set; }
    public bool AllowImageDescription { get; set; }
    public bool AllowTableReconstruction { get; set; }
    public bool AllowDocumentContentToLeaveComputer { get; set; }
}


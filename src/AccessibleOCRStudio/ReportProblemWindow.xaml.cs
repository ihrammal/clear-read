using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using AccessibleOCRStudio.Services;

namespace AccessibleOCRStudio;

public partial class ReportProblemWindow : Window
{
    private readonly AppSettings _settings;
    private readonly DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private string _exportedReport = string.Empty;
    private bool _confirmingClose;
    public ReportProblemWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        LiveRegionAnnouncements.Attach(ReportStatus);
        foreach (var text in new[] { Summary, Steps, Expected, Actual }) text.TextChanged += (_, _) => RefreshPreview();
        IncludeDiagnostics.Checked += (_, _) => RefreshPreview();
        IncludeDiagnostics.Unchecked += (_, _) => RefreshPreview();
        RefreshPreview();
        Loaded += (_, _) => Summary.Focus();
    }
    private string BuildReport() => ProblemReportService.Create(Summary.Text, Steps.Text, Expected.Text, Actual.Text,
        IncludeDiagnostics.IsChecked == true, _settings, _createdAt);
    private bool HasRequiredFields => !string.IsNullOrWhiteSpace(Summary.Text) && !string.IsNullOrWhiteSpace(Actual.Text);
    private bool HasUnsavedDraft => new[] { Summary.Text, Steps.Text, Expected.Text, Actual.Text }.Any(t => !string.IsNullOrWhiteSpace(t)) && BuildReport() != _exportedReport;
    private void RefreshPreview()
    {
        ReportPreview.Text = BuildReport();
        EmailReport.IsEnabled = SaveReport.IsEnabled = CopyReport.IsEnabled = HasRequiredFields;
        var githubReady = ProblemReportService.CreateGitHubDraft(_settings.GitHubRepositoryUrl, "", "") is not null;
        GitHubReport.IsEnabled = HasRequiredFields && githubReady;
        GitHubReport.ToolTip = githubReady ? "Open a GitHub issue draft for review. You submit it yourself." : "GitHub reporting is not available yet. Use Email report, Save or Copy.";
        GitHubAvailability.Text = githubReady ? "GitHub reporting opens an issue draft. Issues may be public; review the preview before sharing." : "GitHub reporting is not available yet. You can email, save or copy the report.";
        ReportStatus.Text = HasRequiredFields ? "Report ready. Review the preview, then email, save or copy it." : "Complete the summary and actual behavior to enable report actions.";
    }
    private bool SaveToFile()
    {
        if (!HasRequiredFields) { ReportStatus.Text = "Enter a summary and actual behavior first."; Summary.Focus(); return false; }
        var picker = new SaveFileDialog { Title = "Save problem report", Filter = "Text report|*.txt", DefaultExt = ".txt",
            FileName = $"ClearRead-problem-{_createdAt:yyyyMMdd-HHmmss}.txt", OverwritePrompt = true };
        if (picker.ShowDialog(this) != true) { ReportStatus.Text = "Save cancelled. Your report is still open."; return false; }
        try
        {
            var report = BuildReport();
            File.WriteAllText(picker.FileName, report, new UTF8Encoding(false));
            _exportedReport = report;
            ReportStatus.Text = "Report saved locally. You can attach it to an email to " + ProblemReportService.SupportEmail + ".";
            return true;
        }
        catch (Exception ex) { ReportStatus.Text = "Could not save report: " + ex.Message; return false; }
    }
    private void Save_Click(object sender, RoutedEventArgs e) => SaveToFile();
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!HasRequiredFields) return;
        try { var report = BuildReport(); Clipboard.SetText(report); _exportedReport = report; ReportStatus.Text = "Report copied. Paste it into an email to " + ProblemReportService.SupportEmail + "."; }
        catch { ReportStatus.Text = "Clipboard is unavailable. Use Save report instead."; }
    }
    private void Email_Click(object sender, RoutedEventArgs e)
    {
        if (!HasRequiredFields) return;
        var report = BuildReport();
        var draft = ProblemReportService.CreateEmailDraft(Summary.Text, report);
        // Long mailto URLs can be silently truncated by email clients. Save the
        // complete report and use a short draft with an attachment reminder.
        if (draft.AbsoluteUri.Length > 1800)
        {
            if (!SaveToFile()) return;
            draft = ProblemReportService.CreateEmailDraft(Summary.Text,
                "Please attach the saved Clear Read text report before sending. The full details are in that file.");
        }
        try
        {
            Process.Start(new ProcessStartInfo(draft.AbsoluteUri) { UseShellExecute = true });
            ReportStatus.Text = "Email draft opened. Review it and choose Send in your email app. Nothing is sent automatically.";
        }
        catch { ReportStatus.Text = "Could not open an email application. Save or copy your report and send it to " + ProblemReportService.SupportEmail + "."; }
    }
    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        if (!HasRequiredFields) return;
        var draft = ProblemReportService.CreateGitHubDraft(_settings.GitHubRepositoryUrl, Summary.Text, BuildReport());
        if (draft is null) { ReportStatus.Text = "GitHub reporting is not available yet. Email, save or copy the report instead."; return; }
        if (MessageBox.Show(this, "GitHub issues may be public. The report preview will be included in an issue draft. Open the draft for review? Nothing will be submitted automatically.",
            "Clear Read — GitHub report", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        if (draft.AbsoluteUri.Length > 6000)
        {
            if (!SaveToFile()) return;
            draft = ProblemReportService.CreateGitHubDraft(_settings.GitHubRepositoryUrl, Summary.Text,
                "Please attach the saved Clear Read text report after reviewing it for private information. The full details are in that file.")!;
        }
        try
        {
            Process.Start(new ProcessStartInfo(draft.AbsoluteUri) { UseShellExecute = true });
            ReportStatus.Text = "GitHub issue draft opened. Review it and submit it yourself. Nothing was posted automatically.";
        }
        catch { ReportStatus.Text = "Could not open the browser. Email, save or copy your report instead."; }
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!HasUnsavedDraft) return;
        e.Cancel = true;
        if (_confirmingClose) return;
        _confirmingClose = true;
        try
        {
            var answer = MessageBox.Show(this, "Save this problem report before closing? Choose No to discard it or Cancel to continue editing.",
                "Clear Read — Unsaved problem report", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
            if (answer == MessageBoxResult.No || (answer == MessageBoxResult.Yes && SaveToFile())) e.Cancel = false;
        }
        finally { _confirmingClose = false; }
    }
}


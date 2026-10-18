using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccessibleOCRStudio.Services;

namespace AccessibleOCRStudio;

public partial class MainWindow : Window
{
    private const string AppVersion = "1.0.0";
    private readonly WelcomeService _welcome = new();
    private readonly LocalOcrService _ocr = new();
    private readonly ExportService _export = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private readonly RecognitionReviewService _review = new();
    private readonly UpdateService _updates = new();
    private readonly TableAccessibilityService _tables = new();
    private readonly RuntimeDiagnosticsService _diagnostics = new();
    private readonly SearchablePdfService _searchablePdf = new();
    private RecognitionDocument? _lastRecognition;
    private int _lowConfidenceIndex = -1;
    private string? _currentFile;
    private string? _lastTextSavePath;
    private bool _allowClose;
    private bool _altPressed;
    private bool _altUsedWithOtherKey;
    private UIElement? _menuReturnFocus;
    private CancellationTokenSource? _apiCancellation;
    private bool _busy;
    private bool _closing;
    private string _savedText = string.Empty;
    private bool HasUnsavedText => DocumentText.Text != _savedText;

    public MainWindow()
    {
        InitializeComponent(); LiveRegionAnnouncements.Attach(StatusText);
        foreach (var item in new[] { UndoMenu, RedoMenu, CutMenu, CopyMenu, PasteMenu, SelectAllMenu }) item.CommandTarget = DocumentText;
        Deactivated += (_, _) => { _altPressed = false; _altUsedWithOtherKey = false; };
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        DocumentText.Focus();
        SyncAccessibilityMenu();
        var runtime = _diagnostics.Check();
        if (!runtime.CoreOcrReady) StatusText.Text = runtime.Summary;
        if (_settings.CheckUpdatesOnStartup && !_settings.OfflineOnlyMode && !string.IsNullOrWhiteSpace(_settings.UpdateFeedUrl))
        {
            var result = await _updates.CheckAsync(_settings.UpdateFeedUrl, AppVersion);
            if (result.UpdateAvailable) StatusText.Text = result.Message;
        }
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) { StatusText.Text = "Wait for the current conversion to finish."; return; }
        var d = new OpenFileDialog { Filter = "Supported documents|*.pdf;*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp|PDF files|*.pdf|Image files|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp|All files|*.*" };
        if (DialogGuard.Run(d.ShowDialog) == true)
        {
            if (!await ConfirmUnsavedTextAsync()) return;
            _currentFile = d.FileName;
            _lastTextSavePath = null;
            _lastRecognition = null;
            _lowConfidenceIndex = -1;
            DocumentText.Clear();
            _savedText = string.Empty;
            StatusText.Text = $"Opened {System.IO.Path.GetFileName(_currentFile)}. Ready for recognition.";
            DocumentText.Focus();
        }
    }

    private async void Recognize_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_currentFile is null) { DialogGuard.Show(this, ExperienceMessages.Get("openFirst", _settings.MessageLanguage), "Clear Read", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        if (!await ConfirmUnsavedTextAsync()) return;
        _busy = true;
        StatusText.Text = ExperienceMessages.Get("started", _settings.MessageLanguage);
        try
        {
            var progress = new Progress<string>(m => StatusText.Text = m);
            _lastRecognition = await _ocr.RecognizeWithConfidenceAsync(_currentFile, _settings, progress);
            DocumentText.Text = _lastRecognition.PlainText;
            var firstLetter = DocumentText.Text.FirstOrDefault(char.IsLetter);
            var rtl = firstLetter >= '\u0600' && firstLetter <= '\u06ff';
            DocumentText.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            DocumentText.Language = System.Windows.Markup.XmlLanguage.GetLanguage(rtl ? "ar" : _settings.OcrLanguages == "fra" ? "fr" : "en");
            _lowConfidenceIndex = -1;
            StatusText.Text = ExperienceMessages.Get("complete", _settings.MessageLanguage);
            DocumentText.CaretIndex = 0;
            DocumentText.ScrollToHome();
            DocumentText.Focus();
        }
        catch (Exception ex)
        {
            StatusText.Text = ExperienceMessages.Get("failed", _settings.MessageLanguage);
            DocumentText.Focus();
            DialogGuard.Show(this, ex.Message, "OCR Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { _busy = false; }
    }

    private async void OpenMultiple_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var d = new OpenFileDialog { Multiselect = true, Filter = "Supported documents|*.pdf;*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp|All files|*.*" };
        if (DialogGuard.Run(d.ShowDialog) != true || d.FileNames.Length == 0) return;
        var folder = new Microsoft.Win32.OpenFolderDialog { Title = "Choose batch output folder", Multiselect = false };
        if (DialogGuard.Run(folder.ShowDialog) != true) return;
        var output = folder.FolderName;
        _busy = true;
        try
        {
            var batch = new BatchService(); var progress = new Progress<string>(m => StatusText.Text = m);
            var count = await batch.ConvertToTextAsync(d.FileNames, output, _settings, progress);
            StatusText.Text = $"Batch conversion complete. {count} files exported to {output}.";
            DocumentText.Focus();
        }
        catch (Exception ex) { DialogGuard.Show(this, ex.Message, "Batch OCR Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _busy = false; }
    }

    private async void OpenAiCorrection_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var key = new CredentialService().Load(_settings.ApiProvider) ?? string.Empty;
        if (ApiCorrectionService.ConfigurationError(_settings, key) is { } error)
        {
            if (DialogGuard.Show(this, error + "\n\nOpen API settings now? After saving, this correction request will continue and ask for confirmation before sending text.", "API correction setup", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            var settingsWindow = new ApiSettingsWindow(_settings) { Owner = this };
            if (ShowDialogReturningFocus(settingsWindow) != true) return;
            key = new CredentialService().Load(_settings.ApiProvider) ?? string.Empty;
            if (ApiCorrectionService.ConfigurationError(_settings, key) is { } remainingError)
            {
                StatusText.Text = "API settings saved, but correction has not started. " + remainingError;
                DialogGuard.Show(this, StatusText.Text, "API correction setup", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        var selected = DocumentText.SelectionLength > 0;
        var start = selected ? DocumentText.SelectionStart : 0;
        var original = selected ? DocumentText.SelectedText : DocumentText.Text;
        if (string.IsNullOrWhiteSpace(original) || original.Length > OpenAiCorrectionService.MaximumCharacters)
        {
            DialogGuard.Show(this, $"Recognize a document or enter text, then select up to {OpenAiCorrectionService.MaximumCharacters:N0} characters. With no selection, the whole editor is sent.", "API correction");
            return;
        }
        if (DialogGuard.Show(this, $"Send {(selected ? "the selected" : "all editor")} text ({original.Length:N0} characters) to {_settings.ApiProvider} using your API account and model {ApiProviderCatalog.Model(_settings, _settings.ApiProvider)}? API charges may apply. No images or file paths will be sent. You will review the result before applying it. {ApiProviderCatalog.Notice(_settings.ApiProvider)}", "Send text to API provider?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _busy = true; DocumentText.IsReadOnly = true; CancelApiMenu.IsEnabled = true;
        using var cancellation = new CancellationTokenSource(); _apiCancellation = cancellation;
        StatusText.Text = "Requesting API correction. Use OCR > Cancel API request to cancel. Local text is preserved.";
        try
        {
            var corrected = await new ApiCorrectionService().CorrectAsync(original, _settings, key, true, cancellation.Token);
            var review = new ApiCorrectionReviewWindow(original, corrected) { Owner = this };
            if (ShowDialogReturningFocus(review) == true)
            {
                DocumentText.IsReadOnly = false;
                ApplyApiCorrection(start, original.Length, corrected);
                StatusText.Text = "API correction applied. Review the text; Undo restores the original.";
            }
            else StatusText.Text = "API correction discarded. Original local text retained.";
        }
        catch (OperationCanceledException) { StatusText.Text = "API request cancelled. Original local text retained."; }
        catch (Exception ex)
        {
            StatusText.Text = "API correction failed. Original local text retained; offline OCR remains available.";
            DialogGuard.Show(this, ex.Message, "API correction", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            _apiCancellation = null; CancelApiMenu.IsEnabled = false; DocumentText.IsReadOnly = false; _busy = false; DocumentText.Focus();
        }
    }

    internal void ApplyApiCorrection(int start, int length, string corrected)
    {
        DocumentText.BeginChange();
        try { DocumentText.Select(start, length); DocumentText.SelectedText = corrected; }
        finally { DocumentText.EndChange(); }
        _lastRecognition = null; _lowConfidenceIndex = -1;
    }

    private void CancelApi_Click(object sender, RoutedEventArgs e) => _apiCancellation?.Cancel();

    private void ApiSettings_Click(object sender, RoutedEventArgs e)
    {
        var w = new ApiSettingsWindow(_settings) { Owner = this };
        var saved = ShowDialogReturningFocus(w);
        if (saved == true) StatusText.Text = _settings.OfflineOnlyMode ? "API settings saved. Offline only mode is enabled." : "API settings saved. No correction has run. To correct editor text, choose OCR > Correct text with selected API.";
    }

    private void Preferences_Click(object sender, RoutedEventArgs e)
    {
        var w = new SettingsWindow(_settings) { Owner = this };
        var saved = ShowDialogReturningFocus(w);
        if (saved == true) { SyncAccessibilityMenu(); StatusText.Text = "Settings saved."; }
    }

    private bool? ShowDialogReturningFocus(Window window)
    {
        var previous = Keyboard.FocusedElement as UIElement;
        var result = DialogGuard.ShowDialog(window);
        if (previous is not null && previous.Focusable && previous.IsEnabled && previous.IsVisible && previous.Focus()) return result;
        DocumentText.Focus();
        return result;
    }

    private void Language_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem m && m.Tag is string lang)
        {
            _settings.OcrLanguages = lang; _settings.Save();
            StatusText.Text = $"OCR language profile set to {m.Header}.";
        }
    }

    private async void SaveText_Click(object sender, RoutedEventArgs e)
    {
        await SaveDocumentTextAsync();
    }

    private async Task<bool> SaveDocumentTextAsync(bool saveAs = false)
    {
        var path = saveAs ? null : _lastTextSavePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var d = new SaveFileDialog { Filter = "Text file|*.txt", DefaultExt = ".txt", FileName = SuggestedName("txt") };
            if (DialogGuard.Run(d.ShowDialog) != true) return false;
            path = d.FileName;
        }
        try
        {
            var text = DocumentText.Text;
            await _export.SaveTextAsync(path, text);
            _lastTextSavePath = path;
            _savedText = text;
            StatusText.Text = $"Saved {System.IO.Path.GetFileName(path)}.";
            return true;
        }
        catch (Exception ex) { ShowExportError(ex); return false; }
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        await SaveDocumentTextAsync(saveAs: true);
    }

    private void ExportWord_Click(object sender, RoutedEventArgs e)
    {
        var d = new SaveFileDialog { Filter = "Word document|*.docx", DefaultExt = ".docx", FileName = SuggestedName("docx") };
        if (DialogGuard.Run(d.ShowDialog) != true) return;
        try
        {
            _export.SaveAccessibleDocx(d.FileName, DocumentText.Text, _currentFile is null ? "Recognized document" : System.IO.Path.GetFileNameWithoutExtension(_currentFile));
            StatusText.Text = $"Exported accessible Word document {System.IO.Path.GetFileName(d.FileName)}.";
        }
        catch (Exception ex) { ShowExportError(ex); }
    }


    private async void ExportHtml_Click(object sender, RoutedEventArgs e)
    {
        var d = new SaveFileDialog { Filter = "HTML document|*.html", DefaultExt = ".html", FileName = SuggestedName("html") };
        if (DialogGuard.Run(d.ShowDialog) != true) return;
        try
        {
            await _export.SaveHtmlAsync(d.FileName, DocumentText.Text, _currentFile is null ? "Recognized document" : System.IO.Path.GetFileNameWithoutExtension(_currentFile));
            StatusText.Text = $"Exported accessible HTML {System.IO.Path.GetFileName(d.FileName)}.";
        }
        catch (Exception ex) { ShowExportError(ex); }
    }

    private async void ExportMarkdown_Click(object sender, RoutedEventArgs e)
    {
        var d = new SaveFileDialog { Filter = "Markdown document|*.md", DefaultExt = ".md", FileName = SuggestedName("md") };
        if (DialogGuard.Run(d.ShowDialog) != true) return;
        try
        {
            await _export.SaveMarkdownAsync(d.FileName, DocumentText.Text, _currentFile is null ? "Recognized document" : System.IO.Path.GetFileNameWithoutExtension(_currentFile));
            StatusText.Text = $"Exported Markdown {System.IO.Path.GetFileName(d.FileName)}.";
        }
        catch (Exception ex) { ShowExportError(ex); }
    }

    private void ShowExportError(Exception ex)
    {
        StatusText.Text = "Export failed. Choose a writable location and try again.";
        DialogGuard.Show(this, ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ReviewRecognition_Click(object sender, RoutedEventArgs e)
    {
        var items = _lastRecognition is not null && _lastRecognition.Words.Count > 0
            ? _lastRecognition.LowConfidenceWords(_settings.LowConfidenceThreshold)
                .Select(w => new LowConfidenceItem(w.Text, w.Confidence, Math.Max(1, w.Line))).ToList()
            : _review.FindSuspiciousText(DocumentText.Text).ToList();
        if (items.Count == 0)
        {
            DialogGuard.Show(this, "No obvious suspicious text was found by the basic review rules. This does not guarantee that OCR is error-free.", "Recognition Review", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var w = new RecognitionReviewWindow(items, (line, text) =>
        {
            var index = DocumentText.Text.IndexOf(text, StringComparison.Ordinal);
            if (index >= 0) { DocumentText.Focus(); DocumentText.Select(index, text.Length); DocumentText.ScrollToLine(Math.Max(0, line - 1)); }
        }) { Owner = this };
        ShowDialogReturningFocus(w);
    }

    private void Find_Click(object sender, RoutedEventArgs e) => ShowDialogReturningFocus(new FindReplaceWindow(DocumentText, false) { Owner = this });
    private void Replace_Click(object sender, RoutedEventArgs e) => ShowDialogReturningFocus(new FindReplaceWindow(DocumentText, true) { Owner = this });

    private void SyncAccessibilityMenu()
    {
        System.Windows.Automation.AutomationProperties.SetLiveSetting(StatusText, _settings.AnnounceOcrProgress ? System.Windows.Automation.AutomationLiveSetting.Polite : System.Windows.Automation.AutomationLiveSetting.Off);
        StatusText.Language = System.Windows.Markup.XmlLanguage.GetLanguage(ExperienceMessages.Language(_settings.MessageLanguage));
        // UI Automation and system contrast remain available to every user.
        ScreenReaderMenu.IsChecked = true;
        DocumentText.FontSize = _settings.AccessibleReadingMode ? Math.Max(18, SystemFonts.MessageFontSize) : SystemFonts.MessageFontSize;
        DocumentText.Padding = new Thickness(_settings.AccessibleReadingMode ? 8 : 2);
        AnnounceProgressMenu.IsChecked = _settings.AnnounceOcrProgress;
        HighContrastMenu.IsChecked = true;
        AccessibleReadingMenu.IsChecked = _settings.AccessibleReadingMode;
        StartupSoundMenu.IsChecked = _settings.PlayStartupSound;
        ExitSoundMenu.IsChecked = _settings.PlayExitSound;
        WelcomeMessageMenu.IsChecked = _settings.ShowWelcomeMessage;
    }
    private void AccessibilityToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem m || m.Tag is not string key) return;
        var value = m.IsChecked;
        switch (key)
        {
            case "ScreenReaderOptimization": _settings.ScreenReaderOptimization = value; break;
            case "AnnounceOcrProgress": _settings.AnnounceOcrProgress = value; break;
            case "FollowWindowsHighContrast": _settings.FollowWindowsHighContrast = value; break;
            case "AccessibleReadingMode": _settings.AccessibleReadingMode = value; break;
            case "PlayStartupSound": _settings.PlayStartupSound = value; break;
            case "PlayExitSound": _settings.PlayExitSound = value; break;
            case "ShowWelcomeMessage": _settings.ShowWelcomeMessage = value; break;
        }
        _settings.Save(); SyncAccessibilityMenu(); StatusText.Text = $"{m.Header}: {(value ? "On" : "Off")}.";
    }

    private string SuggestedName(string ext) => (_currentFile is null ? "recognized-document" : System.IO.Path.GetFileNameWithoutExtension(_currentFile)) + "." + ext;
    private void Help_Click(object s, RoutedEventArgs e) => OpenDoc("help/index.html");
    private void Shortcuts_Click(object s, RoutedEventArgs e) => OpenDoc("help/shortcuts.html");
    private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.OfflineOnlyMode)
        {
            StatusText.Text = "Update checking is disabled while offline-only mode is on.";
            DialogGuard.Show(this, "Update checking is disabled while offline-only mode is on. Turn it off in Tools > API and Privacy Settings, then try again.", "Clear Read Updates", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        StatusText.Text = "Checking for updates.";
        var result = await _updates.CheckAsync(_settings.UpdateFeedUrl, AppVersion);
        StatusText.Text = result.Message;
        DialogGuard.Show(this, result.Message, "Clear Read Updates", MessageBoxButton.OK, result.UpdateAvailable ? MessageBoxImage.Information : MessageBoxImage.None);
        if (result.UpdateAvailable && !string.IsNullOrWhiteSpace(result.ReleaseUrl))
        {
            var answer = DialogGuard.Show(this, "Open the release page in your default browser?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes) Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
        }
    }


    private async void ExportSearchablePdf_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_currentFile is null) { DialogGuard.Show(this, ExperienceMessages.Get("openFirst", _settings.MessageLanguage), "Clear Read", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var d = new SaveFileDialog { Filter = "Searchable PDF|*.pdf", DefaultExt = ".pdf", FileName = SuggestedName("searchable.pdf") };
        if (DialogGuard.Run(d.ShowDialog) != true) return;
        _busy = true;
        try { var progress = new Progress<string>(m => StatusText.Text = m); await _searchablePdf.CreateAsync(_currentFile, d.FileName, _settings, progress); StatusText.Text = "Searchable PDF created."; }
        catch (Exception ex) { DialogGuard.Show(this, ex.Message, "Searchable PDF Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _busy = false; }
    }

    private void ReportProblem_Click(object sender, RoutedEventArgs e) => ShowDialogReturningFocus(new ReportProblemWindow(_settings) { Owner = this });

    private void RuntimeCheck_Click(object sender, RoutedEventArgs e)
    {
        var r = _diagnostics.Check();
        DialogGuard.Show(this, r.Details, "Runtime Diagnostics", MessageBoxButton.OK, r.CoreOcrReady ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void AccessibilityGuide_Click(object sender, RoutedEventArgs e) => OpenDoc("help/accessibility.html");

    private void LinearizeTable_Click(object sender, RoutedEventArgs e)
    {
        var selected = DocumentText.SelectedText;
        if (string.IsNullOrWhiteSpace(selected))
        {
            DialogGuard.Show(this, "Select the table text first. Tab-separated, pipe-separated, or aligned columns are supported.", "Linearize Table", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var linear = _tables.Linearize(selected);
        DocumentText.SelectedText = linear;
        StatusText.Text = "Selected table text was converted to an accessible linear representation.";
    }

    private void NextLowConfidence_Click(object sender, RoutedEventArgs e) => MoveLowConfidence(1);
    private void PreviousLowConfidence_Click(object sender, RoutedEventArgs e) => MoveLowConfidence(-1);
    private void MoveLowConfidence(int direction)
    {
        var words = _lastRecognition?.LowConfidenceWords(_settings.LowConfidenceThreshold).ToList();
        if (words is null || words.Count == 0)
        {
            StatusText.Text = "No word-level low-confidence data is available for this document.";
            return;
        }
        _lowConfidenceIndex = (_lowConfidenceIndex + direction + words.Count) % words.Count;
        var word = words[_lowConfidenceIndex];
        var start = DocumentText.Text.IndexOf(word.Text, StringComparison.Ordinal);
        if (start >= 0) { DocumentText.Focus(); DocumentText.Select(start, word.Text.Length); }
        StatusText.Text = $"Low-confidence word {_lowConfidenceIndex + 1} of {words.Count}: {word.Text}, confidence {word.Confidence:0} percent.";
    }

    private void FocusDocument_Click(object sender, RoutedEventArgs e) => DocumentText.Focus();
    private void FocusStatus_Click(object sender, RoutedEventArgs e) => StatusText.Focus();

    private bool IsMenuActive => ApplicationMenu.IsKeyboardFocusWithin ||
        ApplicationMenu.Items.OfType<MenuItem>().Any(item => item.IsSubmenuOpen);

    private void LeaveMenu()
    {
        foreach (var item in ApplicationMenu.Items.OfType<MenuItem>()) item.IsSubmenuOpen = false;
        if (_menuReturnFocus is not null && _menuReturnFocus.IsVisible && _menuReturnFocus.IsEnabled && _menuReturnFocus.Focus()) return;
        DocumentText.Focus();
    }

    private void ToggleMenu()
    {
        if (IsMenuActive) { LeaveMenu(); return; }
        _menuReturnFocus = Keyboard.FocusedElement as UIElement;
        FileMenu.Focus();
    }

    private void ApplicationMenu_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Alt or Escape leaves menu navigation. Tab must not jump to the editor/status.
        if (e.Key == Key.Tab) e.Handled = true;
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is not (Key.LeftAlt or Key.RightAlt) || !_altPressed) return;
        var toggle = !_altUsedWithOtherKey;
        _altPressed = false;
        _altUsedWithOtherKey = false;
        if (toggle) ToggleMenu();
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftAlt or Key.RightAlt)
        {
            // Preserve AltGr (Ctrl+Alt) and screen-reader modifier combinations.
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Windows)) != 0) return;
            if (!e.IsRepeat) { _altPressed = true; _altUsedWithOtherKey = false; }
            e.Handled = true;
            return;
        }
        if (_altPressed) _altUsedWithOtherKey = true;
        // Tab visits the document and status; only Alt/F10/mnemonics enter the menu.
        if (key == Key.Tab && !IsMenuActive &&
            (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
        {
            if (DocumentText.IsKeyboardFocusWithin) StatusText.Focus();
            else DocumentText.Focus();
            e.Handled = true;
            return;
        }
        if (key == Key.Escape && IsMenuActive)
        {
            LeaveMenu(); e.Handled = true; return;
        }
        if (key == Key.F10 && Keyboard.Modifiers == ModifierKeys.None)
        {
            ToggleMenu(); e.Handled = true; return;
        }
        if (key == Key.A && Keyboard.Modifiers == ModifierKeys.Alt)
        {
            var menu = ApplicationMenu.Items.OfType<MenuItem>().First(item => Equals(item.Header, "_Accessibility"));
            menu.Focus();
            menu.IsSubmenuOpen = true;
            e.Handled = true;
            return;
        }
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        if (ctrl && e.Key == Key.O) { Open_Click(sender, e); e.Handled = true; }
        else if (ctrl && shift && e.Key == Key.S) { SaveAs_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.S) { SaveText_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.F) { Find_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.H) { Replace_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.R) { Recognize_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.E) { ExportWord_Click(sender, e); e.Handled = true; }
        else if (ctrl && e.Key == Key.OemComma) { Preferences_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.Shift) { FocusStatus_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.F6 && Keyboard.Modifiers == ModifierKeys.None) { FocusDocument_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.F1) { Help_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.F4 && shift) { PreviousLowConfidence_Click(sender, e); e.Handled = true; }
        else if (e.Key == Key.F4) { NextLowConfidence_Click(sender, e); e.Handled = true; }
    }

    private void About_Click(object s, RoutedEventArgs e) => DialogGuard.Show(this, "Clear Read " + AppVersion + "\nAccessibility-first OCR for Windows\nBy Iman Rammal", "About Clear Read");
    private void OpenDoc(string rel) { var p = System.IO.Path.Combine(AppContext.BaseDirectory, rel); if (System.IO.File.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); else DialogGuard.Show(this, "Help file is not installed yet."); }
    private async void Exit_Click(object s, RoutedEventArgs e) { await CloseWithFarewellAsync(); }
    private async void Window_Closing(object? s, CancelEventArgs e) { if (_allowClose) return; e.Cancel = true; await CloseWithFarewellAsync(); }
    private async Task<bool> ConfirmUnsavedTextAsync()
    {
        if (!HasUnsavedText) return true;
        var answer = DialogGuard.Show(this,
            ExperienceMessages.Get("unsaved", _settings.MessageLanguage),
            "Clear Read — Unsaved text", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        return answer == MessageBoxResult.No || (answer == MessageBoxResult.Yes && await SaveDocumentTextAsync() && !HasUnsavedText);
    }

    private async Task CloseWithFarewellAsync()
    {
        if (_allowClose || _closing) return;
        if (_busy) { StatusText.Text = "Wait for the current conversion to finish before closing."; return; }
        _closing = true;
        try
        {
            if (!await ConfirmUnsavedTextAsync()) return;
            IsEnabled = false;
            StatusText.Text = ExperienceMessages.Get("goodbye", _settings.MessageLanguage);
            await _welcome.PlayExitAsync(this, _settings.PlayExitSound, _settings.MessageLanguage);
            _allowClose = true;
            Close();
        }
        finally { _closing = false; }
    }
}






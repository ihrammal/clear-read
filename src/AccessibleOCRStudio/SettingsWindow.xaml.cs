using System.Windows;
using System.Windows.Controls;
using AccessibleOCRStudio.Services;

namespace AccessibleOCRStudio;
public partial class SettingsWindow : Window
{
    private readonly AppSettings _s;
    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent(); _s = settings;
        ScreenReader.IsChecked = true; AnnounceProgress.IsChecked = _s.AnnounceOcrProgress;
        AccessibleReading.IsChecked = _s.AccessibleReadingMode; HighContrast.IsChecked = true;
        StartupSound.IsChecked = _s.PlayStartupSound; ExitSound.IsChecked = _s.PlayExitSound; WelcomeMessage.IsChecked = _s.ShowWelcomeMessage;
        PreferDigital.IsChecked = _s.PreferDigitalPdfText; AutoRotate.IsChecked = _s.AutoRotate; Deskew.IsChecked = _s.Deskew; Grayscale.IsChecked = _s.Grayscale; CheckUpdates.IsChecked = _s.CheckUpdatesOnStartup;
        SelectByTag(MessageLanguage, _s.MessageLanguage);
        SelectByTag(Languages, _s.OcrLanguages); SelectByTag(OcrMode, _s.OcrMode);
    }
    private static void SelectByTag(ComboBox box, string value)
    { foreach (var item in box.Items.OfType<ComboBoxItem>()) if ((string?)item.Tag == value) { box.SelectedItem = item; return; } box.SelectedIndex = 0; }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _s.MessageLanguage = (MessageLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "auto";
        _s.ScreenReaderOptimization = ScreenReader.IsChecked == true; _s.AnnounceOcrProgress = AnnounceProgress.IsChecked == true;
        _s.AccessibleReadingMode = AccessibleReading.IsChecked == true; _s.FollowWindowsHighContrast = HighContrast.IsChecked == true;
        _s.PlayStartupSound = StartupSound.IsChecked == true; _s.PlayExitSound = ExitSound.IsChecked == true; _s.ShowWelcomeMessage = WelcomeMessage.IsChecked == true;
        _s.PreferDigitalPdfText = PreferDigital.IsChecked == true; _s.AutoRotate = AutoRotate.IsChecked == true; _s.Deskew = Deskew.IsChecked == true; _s.Grayscale = Grayscale.IsChecked == true; _s.CheckUpdatesOnStartup = CheckUpdates.IsChecked == true;
        _s.OcrLanguages = (Languages.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "ara+eng+fra";
        _s.OcrMode = (OcrMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "accurate"; _s.Save(); DialogResult = true;
    }
}

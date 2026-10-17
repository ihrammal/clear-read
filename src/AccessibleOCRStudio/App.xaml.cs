using System.Windows;
namespace AccessibleOCRStudio;
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        ToastAnnouncements.RegisterBranding();
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(FitWindowToDisplay));
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        try
        {
            var settings = Services.AppSettings.Load();
            if (!await new Services.WelcomeService().ShowStartupAsync(settings)) { Shutdown(); return; }
            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            main.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Clear Read — Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void FitWindowToDisplay(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window || !ReferenceEquals(e.OriginalSource, window)) return;
        var area = SystemParameters.WorkArea;
        window.MinWidth = Math.Min(window.MinWidth, area.Width);
        window.MinHeight = Math.Min(window.MinHeight, area.Height);
        if (!double.IsNaN(window.Width)) window.Width = Math.Min(window.Width, area.Width);
        if (!double.IsNaN(window.Height)) window.Height = Math.Min(window.Height, area.Height);
    }
}

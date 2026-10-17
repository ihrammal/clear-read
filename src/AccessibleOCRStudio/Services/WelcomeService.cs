using System.Media;
using System.Windows;
namespace AccessibleOCRStudio.Services;
public sealed class WelcomeService
{
    public static TimeSpan StartupDuration => TimeSpan.FromMilliseconds(2500);

    public async Task<bool> ShowStartupAsync(AppSettings settings)
    {
        if (settings.PlayStartupSound) TryPlay("resources/sounds/startup.wav");
        if (!settings.ShowWelcomeMessage) return true;
        var welcome = new WelcomeWindow(settings.MessageLanguage);
        using var closed = new CancellationTokenSource();
        welcome.Closed += (_, _) => closed.Cancel();
        welcome.Show();
        try { await Task.Delay(StartupDuration, closed.Token); }
        catch (OperationCanceledException) { return false; }
        welcome.Close();
        return true;
    }
    public async Task PlayExitAsync(Window owner, bool playSound = true, string? language = null)
    {
        if (playSound) TryPlay("resources/sounds/exit.wav");
        owner.Title = ExperienceMessages.Get("goodbye", language);
        await Task.Delay(1200);
    }
    private static void TryPlay(string rel) { try { var p = Path.Combine(AppContext.BaseDirectory, rel); if (File.Exists(p)) new SoundPlayer(p).Play(); } catch { } }
}

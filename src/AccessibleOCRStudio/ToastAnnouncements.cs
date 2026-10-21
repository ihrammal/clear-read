using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AccessibleOCRStudio;

internal static class ToastAnnouncements
{
    private static long _lastTick;
    private static string _lastText = string.Empty;

    public static void RegisterBranding()
    {
    }

    public static void Attach(TextBlock text)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        EventHandler changed = (_, _) =>
            {
                if (!text.IsLoaded) return;
                var t = text.Text ?? string.Empty;
                if (!IsMilestone(t)) return;
                if (System.Windows.Automation.AutomationProperties.GetLiveSetting(text) == System.Windows.Automation.AutomationLiveSetting.Off) return;
                AnnounceMilestone(t, text);
            };
        text.Loaded += (_, _) => descriptor.AddValueChanged(text, changed);
        text.Unloaded += (_, _) => descriptor.RemoveValueChanged(text, changed);
    }

    internal static void AnnounceMilestone(string message, TextBlock statusText)
    {
        if (System.Windows.Automation.AutomationProperties.GetLiveSetting(statusText) == System.Windows.Automation.AutomationLiveSetting.Off) return;
        var now = Environment.TickCount64;
        if (message == _lastText && now - _lastTick < 5000) return;
        if (now - _lastTick < 800) return;
        _lastTick = now;
        _lastText = message;
        try { new ToastContentBuilder().AddText("TextBridge").AddText(message).Show(); } catch { }
    }

    internal static bool IsMilestone(string t) =>
        t.StartsWith("Opened ", StringComparison.Ordinal) ||
        t == "Recognition started." ||
        t == "Recognition complete." ||
        t == "Recognition failed." ||
        t.StartsWith("Batch conversion complete", StringComparison.Ordinal) ||
        t.StartsWith("Exported ", StringComparison.Ordinal) ||
        t.StartsWith("Saved ", StringComparison.Ordinal) ||
        t == "Searchable PDF created." ||
        new[] { "ar", "fr", "en" }.Any(language =>
            new[] { "welcome", "goodbye", "started", "complete", "failed" }.Any(key =>
                t == Services.ExperienceMessages.Get(key, language)));
}

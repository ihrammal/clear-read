using System.ComponentModel;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace AccessibleOCRStudio;

internal static class LiveRegionAnnouncements
{
    public static void Attach(TextBlock text)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        EventHandler changed = (_, _) =>
            {
                if (!text.IsLoaded) return;
                var message = text.Text ?? string.Empty;
                if (AutomationProperties.GetLiveSetting(text) == AutomationLiveSetting.Off) return;
                var peer = UIElementAutomationPeer.FromElement(text) ?? UIElementAutomationPeer.CreatePeerForElement(text);
                if (peer is null) return;
                var raised = false;
                try
                {
                    peer.RaiseNotificationEvent(AutomationNotificationKind.Other, AutomationNotificationProcessing.ImportantMostRecent, message, "Clear Read status");
                    raised = true;
                }
                catch { }
                if (raised) return;
                if (ToastAnnouncements.IsMilestone(message)) ToastAnnouncements.AnnounceMilestone(message, text);
                else peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            };
        text.Loaded += (_, _) => descriptor.AddValueChanged(text, changed);
        text.Unloaded += (_, _) => descriptor.RemoveValueChanged(text, changed);
    }
}

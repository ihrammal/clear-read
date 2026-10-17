using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using AccessibleOCRStudio.Services;

namespace AccessibleOCRStudio;

public sealed class WelcomeWindow : Window
{
    public WelcomeWindow(string? language)
    {
        var message = ExperienceMessages.Get("welcome", language);
        Title = message;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        Width = 520;
        Language = XmlLanguage.GetLanguage(ExperienceMessages.Language(language));
        FlowDirection = ExperienceMessages.Language(language) == "ar" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var text = new TextBlock { Text = message, FontSize = 24, TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(28), Focusable = true };
        AutomationProperties.SetName(text, message);
        Content = text;
        Loaded += (_, _) => text.Focus();
    }
}

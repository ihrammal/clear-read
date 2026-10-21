using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AccessibleOCRStudio.Services;

internal static class ReadingAppearance
{
    internal static double Size(double value, double fallback, double minimum, double maximum) =>
        double.IsFinite(value) && value >= minimum && value <= maximum ? value : fallback;

    internal static (Brush Background, Brush Foreground) Colors(string theme) => theme switch
    {
        "light" => (Brushes.White, Brushes.Black),
        "dark" => (Brushes.Black, Brushes.White),
        "yellow" => (Brushes.Black, Brushes.Yellow),
        "cream" => (new SolidColorBrush(Color.FromRgb(255, 248, 220)), Brushes.Black),
        _ => (SystemColors.WindowBrush, SystemColors.WindowTextBrush)
    };

    internal static void Apply(Window window, AppSettings settings)
    {
        window.FontSize = Size(settings.InterfaceFontSize, 14, 12, 32);
        var keys = new[] { SystemColors.WindowBrushKey, SystemColors.WindowTextBrushKey,
            SystemColors.ControlBrushKey, SystemColors.ControlTextBrushKey, SystemColors.MenuBrushKey,
            SystemColors.MenuTextBrushKey, SystemColors.InfoBrushKey, SystemColors.InfoTextBrushKey,
            SystemColors.HighlightBrushKey, SystemColors.HighlightTextBrushKey };
        foreach (var key in keys) window.Resources.Remove(key);
        window.ClearValue(Control.BackgroundProperty);
        window.ClearValue(Control.ForegroundProperty);
        // Always honor Windows high contrast, including changes while the app is open.
        if (SystemParameters.HighContrast || settings.ReadingColors == "system") return;
        var (background, foreground) = Colors(settings.ReadingColors);
        foreach (var key in new[] { SystemColors.WindowBrushKey, SystemColors.ControlBrushKey,
            SystemColors.MenuBrushKey, SystemColors.InfoBrushKey, SystemColors.HighlightTextBrushKey })
            window.Resources[key] = background;
        foreach (var key in new[] { SystemColors.WindowTextBrushKey, SystemColors.ControlTextBrushKey,
            SystemColors.MenuTextBrushKey, SystemColors.InfoTextBrushKey, SystemColors.HighlightBrushKey })
            window.Resources[key] = foreground;
        window.Background = background;
        window.Foreground = foreground;
    }

    internal static void ApplyDocument(TextBox document, AppSettings settings)
    {
        try { document.FontFamily = new FontFamily(string.IsNullOrWhiteSpace(settings.DocumentFontFamily) ? "Segoe UI" : settings.DocumentFontFamily); }
        catch (ArgumentException) { document.FontFamily = SystemFonts.MessageFontFamily; }
        document.FontSize = Size(settings.DocumentFontSize, 18, 10, 72) * 96 / 72;
        if (settings.AccessibleReadingMode) document.FontSize = Math.Max(18, document.FontSize);
        document.FontWeight = settings.DocumentBold ? FontWeights.Bold : FontWeights.Normal;
        document.Padding = new Thickness(settings.AccessibleReadingMode ? 8 : 2);
        foreach (var property in new[] { Control.BackgroundProperty, Control.ForegroundProperty, TextBox.CaretBrushProperty,
            System.Windows.Controls.Primitives.TextBoxBase.SelectionBrushProperty,
            System.Windows.Controls.Primitives.TextBoxBase.SelectionTextBrushProperty }) document.ClearValue(property);
        if (SystemParameters.HighContrast || settings.ReadingColors == "system") return;
        var (background, foreground) = Colors(settings.ReadingColors);
        document.Background = background;
        document.Foreground = foreground;
        document.CaretBrush = foreground;
        document.SelectionBrush = foreground;
        document.SelectionTextBrush = background;
        document.SelectionOpacity = 1;
    }
}

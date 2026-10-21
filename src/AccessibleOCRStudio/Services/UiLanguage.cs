using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace AccessibleOCRStudio.Services;

/// <summary>Translates interface labels only; document and user-entered text are never translated.</summary>
public static class UiLanguage
{
    private static readonly Dictionary<string, string> Arabic = Load();
    private static readonly Dictionary<string, string> English = Arabic.GroupBy(p => p.Value).ToDictionary(g => g.Key, g => g.First().Key);
    private static readonly ConditionalWeakTable<TextBlock, object> Observed = new();
    private static readonly (Regex Pattern, string Translation)[] Templates = Arabic.Where(p => p.Key.Contains("{0}"))
        .Select(p => (new Regex("^" + Regex.Replace(Regex.Escape(p.Key), @"\\\{(\d+)}", m => "(?<p" + m.Groups[1].Value + ">.*?)") + "$", RegexOptions.Singleline), p.Value)).ToArray();
    public static bool IsArabic { get; private set; }
    public static void Configure(AppSettings settings) => IsArabic = settings.InterfaceLanguage == "ar" ||
        (settings.InterfaceLanguage == "auto" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar");

    private static Dictionary<string, string> Load()
    {
        using var stream = typeof(UiLanguage).Assembly.GetManifestResourceStream("AccessibleOCRStudio.Resources.ar.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    public static string T(string text)
    {
        var source = English.GetValueOrDefault(text, text);
        if (!IsArabic) return source;
        if (Arabic.TryGetValue(source, out var translated)) return translated;
        foreach (var (pattern, template) in Templates)
        {
            var match = pattern.Match(text);
            if (match.Success) return Regex.Replace(template, @"\{(\d+)\}", m => match.Groups["p" + m.Groups[1].Value].Value);
        }
        return text;
    }

    public static void Apply(Window window)
    {
        if (window is WelcomeWindow) return;
        window.Title = T(window.Title);
        window.FlowDirection = IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        window.Language = System.Windows.Markup.XmlLanguage.GetLanguage(IsArabic ? "ar" : "en");
        Visit(window, new HashSet<DependencyObject>());
    }

    private static void Visit(DependencyObject item, HashSet<DependencyObject> seen)
    {
        if (!seen.Add(item)) return;
        // Preserve data-bound accessible names (especially the status live region).
        if (!System.Windows.Data.BindingOperations.IsDataBound(item, AutomationProperties.NameProperty))
        {
            var name = AutomationProperties.GetName(item);
            if (!string.IsNullOrEmpty(name)) AutomationProperties.SetName(item, T(name));
        }
        var help = AutomationProperties.GetHelpText(item);
        if (!string.IsNullOrEmpty(help)) AutomationProperties.SetHelpText(item, T(help));
        if (item is HeaderedItemsControl hi && hi.Header is string header) hi.Header = T(header);
        else if (item is HeaderedContentControl hc && hc.Header is string title) hc.Header = T(title);
        // Keep language choices in their native spelling so users can recover from an unwanted language.
        if (item is ContentControl content && content.Content is string label &&
            item is not ComboBoxItem { Tag: "ar" or "en" or "fr" or "auto" }) content.Content = T(label);
        if (item is TextBlock text)
        {
            text.SetCurrentValue(TextBlock.TextProperty, T(text.Text));
            if (!Observed.TryGetValue(text, out _))
            {
                Observed.Add(text, new object());
                DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock)).AddValueChanged(text, TranslateChanged);
                text.Unloaded += Unobserve;
            }
        }
        foreach (var child in LogicalTreeHelper.GetChildren(item).OfType<DependencyObject>()) Visit(child, seen);
        if (item is Visual || item is System.Windows.Media.Media3D.Visual3D)
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++) Visit(VisualTreeHelper.GetChild(item, i), seen);
    }
    private static void TranslateChanged(object? sender, EventArgs e)
    {
        if (sender is TextBlock text && T(text.Text) is var translated && translated != text.Text)
            text.SetCurrentValue(TextBlock.TextProperty, translated);
    }
    private static void Unobserve(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock text) return;
        DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock)).RemoveValueChanged(text, TranslateChanged);
        Observed.Remove(text);
        text.Unloaded -= Unobserve;
    }
}




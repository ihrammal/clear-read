using System.Windows;
using System.Windows.Controls;

namespace AccessibleOCRStudio;

public partial class FindReplaceWindow : Window
{
    private readonly TextBox _editor;
    public FindReplaceWindow(TextBox editor, bool replaceMode)
    {
        InitializeComponent();
        LiveRegionAnnouncements.Attach(ResultText);
        _editor = editor;
        ReplaceText.IsEnabled = replaceMode;
        ReplaceButton.IsEnabled = replaceMode;
        ReplaceAllButton.IsEnabled = replaceMode;
        Title = replaceMode ? "Find and Replace" : "Find";
        Loaded += (_, _) => { if (!string.IsNullOrWhiteSpace(_editor.SelectedText)) FindText.Text = _editor.SelectedText; FindText.Focus(); FindText.SelectAll(); };
    }
    private StringComparison Comparison => MatchCase.IsChecked == true ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        var needle = FindText.Text;
        if (string.IsNullOrEmpty(needle)) { ResultText.Text = "Enter text to find."; return; }
        var start = Math.Min(_editor.Text.Length, _editor.SelectionStart + _editor.SelectionLength);
        var index = _editor.Text.IndexOf(needle, start, Comparison);
        if (index < 0 && start > 0) index = _editor.Text.IndexOf(needle, 0, Comparison);
        if (index < 0) { ResultText.Text = "Text not found."; return; }
        _editor.Focus(); _editor.Select(index, needle.Length); _editor.ScrollToLine(_editor.GetLineIndexFromCharacterIndex(index));
        ResultText.Text = "Match selected in the document.";
    }
    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var replaced = _editor.SelectionLength > 0 && string.Equals(_editor.SelectedText, FindText.Text, Comparison);
        if (replaced) _editor.SelectedText = ReplaceText.Text;
        FindNext_Click(sender, e);
        if (replaced) ResultText.Text = ResultText.Text == "Text not found."
            ? "One occurrence replaced. No further matches."
            : "One occurrence replaced. " + ResultText.Text;
    }
    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var needle = FindText.Text; if (string.IsNullOrEmpty(needle)) { ResultText.Text = "Enter text to find."; return; }
        var source = _editor.Text; var sb = new System.Text.StringBuilder(); var pos = 0; var count = 0;
        while (pos < source.Length)
        {
            var i = source.IndexOf(needle, pos, Comparison); if (i < 0) { sb.Append(source, pos, source.Length-pos); break; }
            sb.Append(source, pos, i-pos); sb.Append(ReplaceText.Text); pos = i + needle.Length; count++;
        }
        if (pos == source.Length) { }
        _editor.Text = sb.ToString(); ResultText.Text = count == 1 ? "1 occurrence replaced." : $"{count} occurrences replaced.";
    }
}

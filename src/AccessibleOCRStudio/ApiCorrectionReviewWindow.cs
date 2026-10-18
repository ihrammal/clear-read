using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace AccessibleOCRStudio;

internal sealed class ApiCorrectionReviewWindow : Window
{
    internal TextBox Proposal { get; }
    public ApiCorrectionReviewWindow(string original, string corrected)
    {
        Title = "Review API correction"; Width = 800; Height = 650; MinWidth = 360; MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ShowInTaskbar = false;
        var panel = new DockPanel { Margin = new Thickness(16) };
        var explanation = new TextBlock { Text = "Review both versions. AI correction can change facts or numbers. Apply replaces only the text you sent; Cancel keeps the original. You can Undo after applying.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,12) };
        DockPanel.SetDock(explanation, Dock.Top); panel.Children.Add(explanation);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var apply = new Button { Content = "_Apply correction", MinWidth = 140, Margin = new Thickness(4) };
        apply.Click += (_, _) => DialogResult = true;
        var cancel = new Button { Content = "_Cancel", IsCancel = true, IsDefault = true, MinWidth = 90, Margin = new Thickness(4) };
        buttons.Children.Add(apply); buttons.Children.Add(cancel); DockPanel.SetDock(buttons, Dock.Bottom); panel.Children.Add(buttons);
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); grid.RowDefinitions.Add(new RowDefinition());
        var beforeLabel = new TextBlock { Text = "Original text", Margin = new Thickness(0,4,0,4) }; grid.Children.Add(beforeLabel);
        var before = MakeText(original, "Original text"); Grid.SetRow(before,1); grid.Children.Add(before);
        var afterLabel = new TextBlock { Text = "Proposed correction", Margin = new Thickness(0,12,0,4) }; Grid.SetRow(afterLabel,2); grid.Children.Add(afterLabel);
        Proposal = MakeText(corrected, "Proposed correction"); Grid.SetRow(Proposal,3); grid.Children.Add(Proposal);
        panel.Children.Add(grid); Content = panel;
        Loaded += (_, _) => Proposal.Focus();
    }
    private static TextBox MakeText(string text, string label)
    {
        var box = new TextBox { Text = text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        AutomationProperties.SetName(box,label);
        var first = text.FirstOrDefault(char.IsLetter);
        box.FlowDirection = first >= '\u0600' && first <= '\u06ff' ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        return box;
    }
}


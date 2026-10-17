using System.Windows;
using System.Windows.Controls;
using AccessibleOCRStudio.Services;
namespace AccessibleOCRStudio;
public partial class RecognitionReviewWindow : Window
{
    private readonly Action<int,string> _goTo;
    public sealed record ReviewRow(int LineNumber,string Text,double Confidence){ public string DisplayText => $"Line {LineNumber}, estimated confidence {Confidence:0}: {Text}"; }
    public RecognitionReviewWindow(IEnumerable<LowConfidenceItem> items, Action<int,string> goTo){InitializeComponent();_goTo=goTo;Items.ItemsSource=items.Select(x=>new ReviewRow(x.LineNumber,x.Text,x.Confidence)).ToList();Items.SelectedIndex=Items.Items.Count>0?0:-1;}
    private void Items_SelectionChanged(object s, SelectionChangedEventArgs e){if(Items.SelectedItem is ReviewRow r)SelectedText.Text=r.Text;}
    private void GoTo_Click(object s,RoutedEventArgs e){if(Items.SelectedItem is ReviewRow r){_goTo(r.LineNumber,r.Text);Close();}}
}

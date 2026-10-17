using System.Windows;
using System.Windows.Controls;
using AccessibleOCRStudio.Services;
namespace AccessibleOCRStudio;
public partial class ApiSettingsWindow : Window
{
    private readonly AppSettings _settings; private readonly CredentialService _credentials=new();
    public ApiSettingsWindow(AppSettings settings){InitializeComponent();_settings=settings;OfflineOnly.IsChecked=settings.OfflineOnlyMode;AllowContent.IsChecked=settings.AllowApiDocumentContent;OcrCorrection.IsChecked=settings.AllowApiOcrCorrection;ImageDescription.IsChecked=settings.AllowImageDescription;TableReconstruction.IsChecked=settings.AllowTableReconstruction;foreach(ComboBoxItem i in Provider.Items)if((i.Content?.ToString()??"").Equals(settings.ApiProvider,StringComparison.OrdinalIgnoreCase)){Provider.SelectedItem=i;break;}Provider.SelectedIndex=Provider.SelectedIndex<0?0:Provider.SelectedIndex;LoadProviderCredential();Provider.SelectionChanged+=(_,_)=>LoadProviderCredential();}
    private void LoadProviderCredential() { var provider=(Provider.SelectedItem as ComboBoxItem)?.Content?.ToString()??"None"; ApiKey.Password=provider=="None"?string.Empty:_credentials.Load(provider)??string.Empty; }
    private void Save_Click(object s,RoutedEventArgs e){var provider=(Provider.SelectedItem as ComboBoxItem)?.Content?.ToString()??"None";_settings.OfflineOnlyMode=OfflineOnly.IsChecked==true;_settings.AllowApiDocumentContent=AllowContent.IsChecked==true;_settings.AllowApiOcrCorrection=OcrCorrection.IsChecked==true;_settings.AllowImageDescription=ImageDescription.IsChecked==true;_settings.AllowTableReconstruction=TableReconstruction.IsChecked==true;_settings.ApiProvider=provider;if(provider!="None"){if(!string.IsNullOrWhiteSpace(ApiKey.Password))_credentials.Save(provider,ApiKey.Password);else _credentials.Delete(provider);}_settings.Save();DialogResult=true;}
}

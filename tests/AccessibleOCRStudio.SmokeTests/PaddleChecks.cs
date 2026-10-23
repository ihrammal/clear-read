using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using AccessibleOCRStudio;
using AccessibleOCRStudio.Services;

internal static class PaddleChecks
{
    public static void MakeScreenshots(string output)
    {
        foreach (var language in new[]{"en","ar"})
        {
            UiLanguage.Configure(new AppSettings { InterfaceLanguage=language });
            var window=new MainWindow();
            ((TextBox)window.FindName("DocumentText")).Text="القراءة حق للجميع\nعدد الطلاب ١٢٥ طالبا\n\nUne école française : Noël, été, forêt et cœur.\n\nThe quick brown fox has 125 documents.";
            UiLanguage.Apply(window);
            var root=(FrameworkElement)window.Content;
            if(root is Panel panel) panel.Background=SystemColors.WindowBrush;
            root.Measure(new Size(1200,760));root.Arrange(new Rect(0,0,1200,760));root.UpdateLayout();
            var background=new DrawingVisual();using(var drawing=background.RenderOpen())drawing.DrawRectangle(SystemColors.WindowBrush,null,new Rect(0,0,1200,760));
            var bitmap=new RenderTargetBitmap(1200,760,96,96,PixelFormats.Pbgra32);bitmap.Render(background);bitmap.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var stream=File.Create(Path.Combine(output,$"textbridge-app-{language}.png"))) encoder.Save(stream);
            ((TextBox)window.FindName("DocumentText")).Clear();window.Close();
            var settings=new SettingsWindow(new AppSettings { InterfaceLanguage=language });
            ((TabControl)settings.FindName("Tabs")).SelectedIndex=3;
            UiLanguage.Apply(settings);
            var settingsRoot=(FrameworkElement)settings.Content;
            if(settingsRoot is Panel settingsPanel)settingsPanel.Background=SystemColors.WindowBrush;
            settingsRoot.Measure(new Size(900,760));settingsRoot.Arrange(new Rect(0,0,900,760));settingsRoot.UpdateLayout();
            var settingsBackground=new DrawingVisual();using(var drawing=settingsBackground.RenderOpen())drawing.DrawRectangle(SystemColors.WindowBrush,null,new Rect(0,0,900,760));
            var settingsBitmap=new RenderTargetBitmap(900,760,96,96,PixelFormats.Pbgra32);settingsBitmap.Render(settingsBackground);settingsBitmap.Render(settingsRoot);
            var settingsEncoder=new PngBitmapEncoder();settingsEncoder.Frames.Add(BitmapFrame.Create(settingsBitmap));
            using(var stream=File.Create(Path.Combine(output,$"textbridge-settings-{language}.png")))settingsEncoder.Save(stream);
            settings.Close();
        }
    }
    public static void Run(string temp, Action<bool,string> check)
    {
        var directory = Path.Combine(temp, "Paddle العربية Français");
        Directory.CreateDirectory(directory);
        var image = Path.Combine(directory, "mixed page's text.png");
        Draw(image, new[] {
            ("القراءة حق للجميع", true), ("عدد الطلاب ١٢٥ طالبا", true),
            ("Une école française : Noël, été, forêt et cœur.", false),
            ("The quick brown fox has 125 documents.", false)
        });
        var settings = new AppSettings { OcrLanguages="ara+eng+fra", Deskew=false, AutoRotate=false };
        var ocr = new LocalOcrService();
        var result = ocr.RecognizeWithConfidenceAsync(image,settings).GetAwaiter().GetResult();
        check(result.PlainText.Contains("القراءة حق للجميع") && result.PlainText.Contains("١٢٥"), "Paddle combined profile preserves Arabic lines and Arabic-Indic numerals");
        check(result.PlainText.Contains("école française") && result.PlainText.Contains("cœur") && result.PlainText.Contains("quick brown fox"), "Paddle combined profile recognizes French accents, ligatures and English on the same page");
        check(result.PlainText.IndexOf("القراءة",StringComparison.Ordinal) < result.PlainText.IndexOf("école",StringComparison.Ordinal), "Paddle mixed-language page retains top-to-bottom reading order");
        check(result.Words.Any(word=>word.Text.Contains("القراءة حق للجميع")) && result.Words.All(word=>word.Width>0 && word.Height>0), "Arabic review retains logical text in honest line spans with positive geometry");
        check(result.ReviewLocations(result.PlainText,101).Count == result.Words.Count, "Review navigation can locate every Paddle word or Arabic line span in the actual document");
        var arabic = Path.Combine(directory,"Arabic numerals.png");
        Draw(arabic,new[]{("القراءة حق للجميع",true),("عدد الطلاب ١٢٥ طالبا",true)});
        var pdf = Path.Combine(directory,"Arabic searchable.pdf");
        new SearchablePdfService().CreateAsync(arabic,pdf,new AppSettings { OcrLanguages="ara", Deskew=false, AutoRotate=false }).GetAwaiter().GetResult();
        var extracted = ocr.RecognizeAsync(pdf,new AppSettings { OcrLanguages="ara",PreferDigitalPdfText=true }).GetAwaiter().GetResult();
        Directory.CreateDirectory("artifacts");
        File.WriteAllText("artifacts/paddle-arabic-pdf-extracted.txt",extracted);
        File.Copy(pdf,"artifacts/paddle-arabic-searchable.pdf",true);
        check(extracted.Contains("القراءة حق للجميع") && extracted.Contains("١٢٥"), "Paddle searchable PDF extraction preserves logical Arabic Unicode and numerals");
        var spaced = Path.Combine(directory,"spacing.png");
        Draw(spaced,new[]{("Name                 Amount",false),("School               125",false)});
        var layout = ocr.RecognizeAsync(spaced,new AppSettings { OcrLanguages="eng",Deskew=false,AutoRotate=false,PreserveTextLayout=true }).GetAwaiter().GetResult();
        File.WriteAllText("artifacts/paddle-spacing.txt",layout);
        check(layout.Contains("Name  ") && layout.Contains("Amount") && layout.Contains("125"), "Optional Paddle spacing reconstruction preserves separated columns");
    }

    internal static void Draw(string path, IEnumerable<(string Text,bool Rtl)> lines, string family="Arial", double size=44)
    {
        var visual = new DrawingVisual();
        using(var drawing=visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White,null,new Rect(0,0,1800,700));
            var y=60d;
            foreach(var line in lines)
            {
                var text=new FormattedText(line.Text,CultureInfo.GetCultureInfo(line.Rtl?"ar":"fr"),line.Rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight,new Typeface(family),size,Brushes.Black,1);
                drawing.DrawText(text,new Point(line.Rtl?1700:60,y)); y+=100;
            }
        }
        var bitmap=new RenderTargetBitmap(1800,700,96,96,PixelFormats.Pbgra32); bitmap.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=File.Create(path);encoder.Save(stream);
    }
}

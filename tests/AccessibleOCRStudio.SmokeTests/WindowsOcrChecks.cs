using System.IO;
using AccessibleOCRStudio.Services;

internal static class WindowsOcrChecks
{
    public static void Run(string temp,Action<bool,string> check)
    {
        var profiles=WindowsOcrService.AvailableProfiles;
        if(!profiles.Contains("eng"))
        {
            var actionable=false;
            try { new WindowsOcrService().RecognizeAsync("missing.png",new AppSettings { OcrLanguages="eng" }).GetAwaiter().GetResult(); }
            catch(Exception exception) { actionable=exception.Message.Contains("language pack") || exception.Message.Contains("class not registered",StringComparison.OrdinalIgnoreCase); }
            check(actionable,"Unavailable Windows OCR reports its OS dependency instead of silently switching engines");
            Console.WriteLine("SKIP: Windows OCR image checks require the Windows English OCR pack on this runner.");
            return;
        }
        var image=Path.Combine(temp,"windows-english.png");
        PaddleChecks.Draw(image,new[]{("The quick brown fox has 125 documents.",false)});
        var settings=new AppSettings { OcrEngine="windows",OcrLanguages="eng",Deskew=false,AutoRotate=false };
        var ocr=new LocalOcrService();
        var result=ocr.RecognizeWithConfidenceAsync(image,settings).GetAwaiter().GetResult();
        check(new RuntimeDiagnosticsService().Check(settings).CoreOcrReady,"Windows diagnostics depend on installed Windows language packs rather than the optional Paddle component");
        check(result.PlainText.Contains("quick brown fox") && result.PlainText.Contains("125"),"Windows backend recognizes English through the application adapter");
        check(result.Words.Count>0 && result.Words.All(word=>word.Confidence==-1),"Windows OCR retains word positions without inventing confidence scores");
        var pdf=Path.Combine(temp,"windows-english.pdf");
        new SearchablePdfService().CreateAsync(image,pdf,settings).GetAwaiter().GetResult();
        check(ocr.RecognizeAsync(pdf,settings).GetAwaiter().GetResult().Contains("quick brown fox"),"Windows-only recognition can create and extract a searchable PDF without a Paddle request");
        if(profiles.Contains("ara"))
        {
            var arabic=Path.Combine(temp,"windows-arabic.png");
            PaddleChecks.Draw(arabic,new[]{("القراءة حق للجميع",true),("عدد الطلاب ١٢٥ طالبا",true)});
            var arabicSettings=new AppSettings { OcrEngine="windows",OcrLanguages="ara",Deskew=false,AutoRotate=false };
            var text=ocr.RecognizeAsync(arabic,arabicSettings).GetAwaiter().GetResult();
            check(text.Contains("القراءة حق للجميع") && text.Contains("١٢٥"),"Windows backend reconstructs logical Arabic word order and numerals");
            var arabicPdf=Path.Combine(temp,"windows-arabic.pdf");
            new SearchablePdfService().CreateAsync(arabic,arabicPdf,arabicSettings).GetAwaiter().GetResult();
            var extracted=ocr.RecognizeAsync(arabicPdf,arabicSettings).GetAwaiter().GetResult();
            File.WriteAllText("artifacts/windows-arabic-pdf-extracted.txt",extracted);
            check(extracted.Contains("القراءة حق للجميع") && extracted.Contains("١٢٥"),"Shared PDF writer preserves Windows Arabic text and numerals during extraction");
        }
        if(profiles.Contains("fra"))
        {
            var french=Path.Combine(temp,"windows-french.png");
            PaddleChecks.Draw(french,new[]{("Une école française : Noël, été et forêt.",false)});
            var frenchSettings=new AppSettings { OcrEngine="windows",OcrLanguages="fra",Deskew=false,AutoRotate=false };
            check(ocr.RecognizeAsync(french,frenchSettings).GetAwaiter().GetResult().Contains("école française"),"Windows backend recognizes French accents");
        }
    }
}

using System.Diagnostics;
namespace AccessibleOCRStudio.Services;

public sealed class SearchablePdfService
{
    public async Task CreateAsync(string input, string output, AppSettings settings, IProgress<string>? progress = null)
    {
        var baseDir = AppContext.BaseDirectory;
        var paddle = new OcrBackendService();
        var ext = Path.GetExtension(input).ToLowerInvariant();
        var temp = Path.Combine(Path.GetTempPath(), "AccessibleOCRStudio", "pdf-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        try
        {
            var images = new List<string>();
            if (ext == ".pdf")
            {
                var render = File.Exists(Path.Combine(baseDir,"pdf","pdftoppm.exe")) ? Path.Combine(baseDir,"pdf","pdftoppm.exe") : "pdftoppm";
                await Run(render, new[]{"-r","300","-png",input,Path.Combine(temp,"page")});
                images.AddRange(Directory.GetFiles(temp,"page-*.png").OrderBy(PageNumber));
            }
            else images.Add(input);
            if (images.Count == 0) throw new InvalidOperationException("No pages were available for searchable PDF creation.");
            var pdfs = new List<string>();
            for (var i=0;i<images.Count;i++)
            {
                progress?.Report($"Creating searchable PDF page {i+1} of {images.Count}.");
                var stem=Path.Combine(temp,$"ocr-{i+1:0000}");
                var prepared = await new ImagePreprocessingService().PrepareAsync(images[i], settings);
                try
                {
                    var recognized = await paddle.RecognizeAsync(prepared.Path, settings);
                    await paddle.CreatePdfPageAsync(prepared.Path, stem + ".pdf", recognized);
                    pdfs.Add(stem+".pdf");
                }
                finally { if (prepared.Temporary) File.Delete(prepared.Path); }
            }
            if (pdfs.Count==1) File.Copy(pdfs[0],output,true);
            else
            {
                var merge=File.Exists(Path.Combine(baseDir,"pdf","pdfunite.exe"))?Path.Combine(baseDir,"pdf","pdfunite.exe"):"pdfunite";
                var merged = Path.Combine(temp, "merged.pdf");
                await Run(merge,pdfs.Concat(new[]{merged}).ToArray());
                File.Copy(merged, output, true);
            }
        }
        finally { try { Directory.Delete(temp,true); } catch {} }
    }
    internal static int PageNumber(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(name[(name.LastIndexOf('-') + 1)..], out var page) ? page : int.MaxValue;
    }
    private static async Task Run(string exe, IEnumerable<string> args)
    {
        var psi=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true}; foreach(var a in args) psi.ArgumentList.Add(a);
        using var p=Process.Start(psi)??throw new InvalidOperationException($"Could not start {exe}."); var err=await p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync(); if(p.ExitCode!=0) throw new InvalidOperationException($"{exe} failed. {err}".Trim());
    }
}

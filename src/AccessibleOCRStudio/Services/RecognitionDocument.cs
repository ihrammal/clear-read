namespace AccessibleOCRStudio.Services;

public sealed record OcrWord(string Text, double Confidence, int Page, int Line, int Left, int Top, int Width, int Height);
public sealed record DocumentBlock(string Kind, string Text, int Page, bool RightToLeft = false);

public sealed class RecognitionDocument
{
    public string PlainText { get; set; } = string.Empty;
    public List<OcrWord> Words { get; } = new();
    public List<DocumentBlock> Blocks { get; } = new();
    public double AverageConfidence => Words.Count == 0 ? 0 : Words.Average(w => w.Confidence);
    public IEnumerable<OcrWord> LowConfidenceWords(double threshold = 70) => Words.Where(w => w.Confidence >= 0 && w.Confidence < threshold);
}

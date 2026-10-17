using System.Globalization;

namespace AccessibleOCRStudio.Services;

public sealed record LowConfidenceItem(string Text, double Confidence, int LineNumber);

public sealed class RecognitionReviewService
{
    public IReadOnlyList<LowConfidenceItem> FindSuspiciousText(string text)
    {
        var items = new List<LowConfidenceItem>();
        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length < 2) continue;
            var odd = line.Count(c => c == '\uFFFD' || c == '|' || c == '~');
            var letters = line.Count(char.IsLetterOrDigit);
            var score = letters == 0 ? 40 : Math.Max(40, 98 - (odd * 12));
            if (odd > 0 || (letters > 0 && letters < line.Length / 4))
                items.Add(new(line, score, i + 1));
        }
        return items;
    }
}

using System.Text.RegularExpressions;

namespace AccessibleOCRStudio.Services;

public sealed class DocumentStructureService
{
    private static readonly Regex Bullet = new(@"^\s*([-*•]|\d+[.)])\s+", RegexOptions.Compiled);

    public List<DocumentBlock> Analyze(string text, int page = 1, bool preserveWhitespace = false)
    {
        var blocks = new List<DocumentBlock>();
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 && !preserveWhitespace) continue;
            var kind = Bullet.IsMatch(line) ? "ListItem" : IsHeading(line) ? "Heading" : "Paragraph";
            blocks.Add(new DocumentBlock(kind, preserveWhitespace ? raw : line, page, ContainsArabic(line)));
        }
        return blocks;
    }

    private static bool IsHeading(string line)
    {
        if (line.Length > 100 || line.EndsWith('.') || line.EndsWith('،') || line.EndsWith(';')) return false;
        var letters = line.Where(char.IsLetter).ToArray();
        if (letters.Length == 0) return false;
        var upperRatio = letters.Count(char.IsUpper) / (double)letters.Length;
        return upperRatio > .75 || line.StartsWith("Chapter ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("الفصل ");
    }
    private static bool ContainsArabic(string s) => s.Any(c => c is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF');
}


using System.Text;
using System.Text.RegularExpressions;

namespace AccessibleOCRStudio.Services;

public sealed class TableAccessibilityService
{
    private static readonly Regex MultiSpace = new(@"\s{2,}", RegexOptions.Compiled);

    public string Linearize(string text)
    {
        var rows = text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(SplitRow).Where(r => r.Length >= 2).ToList();
        if (rows.Count < 2) return text;
        var headers = rows[0];
        var sb = new StringBuilder("Accessible table representation").AppendLine().AppendLine();
        for (var r = 1; r < rows.Count; r++)
        {
            sb.Append("Row ").Append(r).AppendLine();
            for (var c = 0; c < rows[r].Length; c++)
            {
                var header = c < headers.Length && !string.IsNullOrWhiteSpace(headers[c]) ? headers[c] : $"Column {c + 1}";
                sb.Append(header).Append(": ").AppendLine(rows[r][c]);
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static string[] SplitRow(string line)
    {
        if (line.Contains('\t')) return line.Split('\t').Select(x => x.Trim()).ToArray();
        if (line.Contains('|')) return line.Trim('|').Split('|').Select(x => x.Trim()).ToArray();
        return MultiSpace.Split(line.Trim()).Select(x => x.Trim()).ToArray();
    }
}

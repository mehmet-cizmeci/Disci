using System.Text;

namespace Disci.LegacyImport.Import;

/// <summary>Minimal RTF to plain text: enough for the short notes Delphi's TRichEdit saved.</summary>
public static class RtfText
{
    private static readonly HashSet<string> SkippedGroups =
        ["fonttbl", "colortbl", "stylesheet", "info", "pict", "header", "footer", "listtable", "listoverridetable"];

    public static string ToPlainText(string rtf, Encoding encoding)
    {
        if (!rtf.StartsWith(@"{\rtf", StringComparison.Ordinal))
            return rtf.Trim();

        var sb = new StringBuilder();
        var hexBytes = new List<byte>();
        var skipDepth = new Stack<bool>();
        bool skipping = false;
        int i = 0;

        void FlushHex()
        {
            if (hexBytes.Count == 0) return;
            if (!skipping) sb.Append(encoding.GetString(hexBytes.ToArray()));
            hexBytes.Clear();
        }

        while (i < rtf.Length)
        {
            char c = rtf[i];
            if (c == '{')
            {
                FlushHex();
                skipDepth.Push(skipping);
                i++;
            }
            else if (c == '}')
            {
                FlushHex();
                skipping = skipDepth.Count > 0 && skipDepth.Pop();
                i++;
            }
            else if (c == '\\')
            {
                i++;
                if (i >= rtf.Length) break;
                char n = rtf[i];
                if (n == '\'' && i + 2 < rtf.Length)
                {
                    hexBytes.Add(Convert.ToByte(rtf.Substring(i + 1, 2), 16));
                    i += 3;
                    continue;
                }
                FlushHex();
                if (n is '\\' or '{' or '}')
                {
                    if (!skipping) sb.Append(n);
                    i++;
                    continue;
                }
                if (n == '*')
                {
                    skipping = true;
                    i++;
                    continue;
                }
                int start = i;
                while (i < rtf.Length && char.IsAsciiLetter(rtf[i])) i++;
                string word = rtf[start..i];
                while (i < rtf.Length && (char.IsAsciiDigit(rtf[i]) || rtf[i] == '-')) i++;
                if (i < rtf.Length && rtf[i] == ' ') i++;
                // Some stored notes have a damaged header ("\fonttb," instead of "\fonttbl"); match by prefix.
                if (SkippedGroups.Contains(word) || word.StartsWith("fonttb", StringComparison.Ordinal)) skipping = true;
                else if (!skipping && word is "par" or "line") sb.Append('\n');
                else if (!skipping && word == "tab") sb.Append('\t');
            }
            else
            {
                FlushHex();
                if (!skipping && c != '\r' && c != '\n') sb.Append(c);
                i++;
            }
        }
        FlushHex();
        return sb.ToString().Trim();
    }
}

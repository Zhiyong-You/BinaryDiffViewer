using System.Text;
using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.Services;

public static class HexFormatter
{
    private const int BytesPerLine = 16;
    // Normal hex section width for a full 16-byte line: XX<sp> × 16 minus trailing space = 47
    private const int FullHexWidth = BytesPerLine * 3 - 1;

    public static string GeneratePlainHexView(byte[] data, long baseOffset = 0)
    {
        var sb = new StringBuilder(EstimateCapacity(data.Length));
        for (int i = 0; i < data.Length; i += BytesPerLine)
            AppendLine(sb, data, i, baseOffset, highlightSet: null);
        return sb.ToString();
    }

    public static string GenerateDiffHexView(byte[] data, IReadOnlyList<BinaryDiffItem> diffs, bool isFileA, long baseOffset = 0)
    {
        var highlightSet = BuildHighlightSet(baseOffset, data.Length, diffs, isFileA);
        var sb = new StringBuilder(EstimateCapacity(data.Length));
        for (int i = 0; i < data.Length; i += BytesPerLine)
            AppendLine(sb, data, i, baseOffset, highlightSet);
        return sb.ToString();
    }

    // Format: 0xXXXXXXXX  HH [HH] HH ...  ASCII
    private static void AppendLine(StringBuilder sb, byte[] data, int lineStart, long baseOffset, HashSet<long>? highlightSet)
    {
        int lineEnd = Math.Min(lineStart + BytesPerLine, data.Length);

        // Offset
        sb.Append($"0x{baseOffset + lineStart:X8}  ");

        // Hex section — track written width to pad for alignment
        int hexWidth = 0;
        for (int j = lineStart; j < lineEnd; j++)
        {
            var absoluteOffset = baseOffset + j;
            if (highlightSet != null && highlightSet.Contains(absoluteOffset))
            {
                sb.Append('[');
                sb.Append($"{data[j]:X2}");
                sb.Append(']');
                hexWidth += 4;
            }
            else
            {
                sb.Append($"{data[j]:X2}");
                hexWidth += 2;
            }

            if (j < lineEnd - 1)
            {
                sb.Append(' ');
                hexWidth++;
            }
        }

        // Pad hex section to full-line width so ASCII column stays aligned.
        // When brackets push hexWidth past FullHexWidth, ASCII shifts right — accepted for simplified diff view.
        if (hexWidth < FullHexWidth)
            sb.Append(' ', FullHexWidth - hexWidth);

        // Separator
        sb.Append("  ");

        // ASCII section
        for (int j = lineStart; j < lineEnd; j++)
            sb.Append(ToAsciiChar(data[j]));

        sb.AppendLine();
    }

    private static HashSet<long> BuildHighlightSet(long baseOffset, int dataLength, IReadOnlyList<BinaryDiffItem> diffs, bool isFileA)
    {
        var set = new HashSet<long>();
        var endOffset = baseOffset + dataLength;
        foreach (var diff in diffs)
        {
            if (diff.Offset < baseOffset)
                continue;

            if (diff.Offset >= endOffset)
                break; // diffs are ordered by ascending offset

            bool applies = isFileA
                ? diff.Type == DiffType.Modified || diff.Type == DiffType.Removed
                : diff.Type == DiffType.Modified || diff.Type == DiffType.Added;
            if (applies)
                set.Add(diff.Offset);
        }
        return set;
    }

    private static char ToAsciiChar(byte b) => b >= 0x20 && b < 0x7F ? (char)b : '.';

    private static int EstimateCapacity(int byteCount)
    {
        // offset(12) + hex(47) + separator(2) + ascii(16) + newline(2) per line
        int lines = (byteCount + BytesPerLine - 1) / BytesPerLine;
        return lines * 79;
    }
}

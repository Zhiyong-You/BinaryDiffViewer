using System.Text;
using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.Services;

public static class HexFormatter
{
    private const int BytesPerLine = 16;

    public static string GeneratePlainHexView(byte[] data)
    {
        var sb = new StringBuilder(EstimateCapacity(data.Length));
        for (int i = 0; i < data.Length; i += BytesPerLine)
        {
            AppendLine(sb, data, i, highlightSet: null);
        }
        return sb.ToString();
    }

    public static string GenerateDiffHexView(byte[] data, IReadOnlyList<BinaryDiffItem> diffs, bool isFileA)
    {
        var highlightSet = BuildHighlightSet(data.Length, diffs, isFileA);
        var sb = new StringBuilder(EstimateCapacity(data.Length));
        for (int i = 0; i < data.Length; i += BytesPerLine)
        {
            AppendLine(sb, data, i, highlightSet);
        }
        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, byte[] data, int lineStart, HashSet<long>? highlightSet)
    {
        sb.Append($"{lineStart:X8}  ");
        int lineEnd = Math.Min(lineStart + BytesPerLine, data.Length);
        for (int j = lineStart; j < lineEnd; j++)
        {
            if (highlightSet != null && highlightSet.Contains(j))
                sb.Append($"[{data[j]:X2}]");
            else
                sb.Append($"{data[j]:X2}");

            if (j < lineEnd - 1) sb.Append(' ');
        }
        sb.AppendLine();
    }

    private static HashSet<long> BuildHighlightSet(int dataLength, IReadOnlyList<BinaryDiffItem> diffs, bool isFileA)
    {
        var set = new HashSet<long>();
        foreach (var diff in diffs)
        {
            if (diff.Offset >= dataLength) break; // diffs are ordered by ascending offset
            bool applies = isFileA
                ? diff.Type == DiffType.Modified || diff.Type == DiffType.Removed
                : diff.Type == DiffType.Modified || diff.Type == DiffType.Added;
            if (applies)
                set.Add(diff.Offset);
        }
        return set;
    }

    private static int EstimateCapacity(int byteCount)
    {
        // offset(10) + bytes(16*3) + newline(2) per line
        int lines = (byteCount + BytesPerLine - 1) / BytesPerLine;
        return lines * 60;
    }
}

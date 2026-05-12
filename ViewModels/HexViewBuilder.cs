using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.ViewModels;

internal static class HexViewBuilder
{
    private const int BytesPerLine = 16;

    public static IReadOnlyList<HexLineViewModel> BuildPlain(byte[] data, long baseOffset = 0)
    {
        var lines = new List<HexLineViewModel>((data.Length + BytesPerLine - 1) / BytesPerLine);
        for (int lineStart = 0; lineStart < data.Length; lineStart += BytesPerLine)
            lines.Add(BuildLine(data, lineStart, baseOffset, highlightSet: null, highlightOffset: -1));
        return lines.AsReadOnly();
    }

    public static IReadOnlyList<HexLineViewModel> BuildDiff(
        byte[] data,
        IReadOnlyList<BinaryDiffItem> diffs,
        bool isFileA,
        long baseOffset = 0,
        long highlightOffset = -1)
    {
        var highlightSet = BuildHighlightSet(baseOffset, data.Length, diffs, isFileA);
        var lines = new List<HexLineViewModel>((data.Length + BytesPerLine - 1) / BytesPerLine);
        for (int lineStart = 0; lineStart < data.Length; lineStart += BytesPerLine)
            lines.Add(BuildLine(data, lineStart, baseOffset, highlightSet, highlightOffset));
        return lines.AsReadOnly();
    }

    private static HexLineViewModel BuildLine(
        byte[] data, int lineStart, long baseOffset,
        HashSet<long>? highlightSet, long highlightOffset)
    {
        var lineOffset = baseOffset + lineStart;
        var lineEnd = Math.Min(lineStart + BytesPerLine, data.Length);
        var cells = new HexCellViewModel[BytesPerLine];

        for (int j = 0; j < BytesPerLine; j++)
        {
            int idx = lineStart + j;
            if (idx < lineEnd)
            {
                var b = data[idx];
                var absOff = baseOffset + idx;
                var isDiff = highlightSet?.Contains(absOff) ?? false;
                cells[j] = new HexCellViewModel($"{b:X2}", ToAsciiChar(b).ToString(), isDiff);
            }
            else
            {
                cells[j] = HexCellViewModel.Empty;
            }
        }

        var isHighlighted = highlightOffset >= 0
            && lineOffset <= highlightOffset
            && highlightOffset < lineOffset + BytesPerLine;

        return new HexLineViewModel(lineOffset, cells, isHighlighted);
    }

    private static HashSet<long> BuildHighlightSet(
        long baseOffset, int dataLength,
        IReadOnlyList<BinaryDiffItem> diffs, bool isFileA)
    {
        var set = new HashSet<long>();
        var endOffset = baseOffset + dataLength;
        foreach (var diff in diffs)
        {
            if (diff.Offset < baseOffset) continue;
            if (diff.Offset >= endOffset) break;

            bool applies = isFileA
                ? diff.Type == DiffType.Modified || diff.Type == DiffType.Removed
                : diff.Type == DiffType.Modified || diff.Type == DiffType.Added;

            if (applies) set.Add(diff.Offset);
        }
        return set;
    }

    private static char ToAsciiChar(byte b) => b >= 0x20 && b < 0x7F ? (char)b : '.';
}

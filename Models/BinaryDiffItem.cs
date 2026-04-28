namespace BinaryDiffViewer.Models;

public record BinaryDiffItem(long Offset, DiffType Type, byte? ByteA, byte? ByteB);

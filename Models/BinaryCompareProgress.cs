namespace BinaryDiffViewer.Models;

public readonly record struct BinaryCompareProgress(long ProcessedBytes, long TotalBytes);

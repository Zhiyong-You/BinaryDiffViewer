namespace BinaryDiffViewer.Models;

public record BinaryDiffResult(
    bool SameSize,
    long TotalDiffCount,
    long? FirstDiffOffset,
    IReadOnlyList<BinaryDiffItem> Diffs
)
{
    public bool AreIdentical => TotalDiffCount == 0;
}

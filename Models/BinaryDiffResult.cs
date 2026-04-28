namespace BinaryDiffViewer.Models;

public record BinaryDiffResult(
    bool IsSameSize,
    long TotalDiffCount,
    long? FirstDiffOffset,
    long FileASize,
    long FileBSize,
    IReadOnlyList<BinaryDiffItem> DisplayDiffItems
)
{
    public bool AreIdentical => TotalDiffCount == 0;
}

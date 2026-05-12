namespace BinaryDiffViewer.ViewModels;

public sealed class HexLineViewModel
{
    public string OffsetText { get; }
    public IReadOnlyList<HexCellViewModel> Cells { get; }
    public bool IsHighlighted { get; }

    public HexLineViewModel(long offset, IReadOnlyList<HexCellViewModel> cells, bool isHighlighted = false)
    {
        OffsetText = $"0x{offset:X8}";
        Cells = cells;
        IsHighlighted = isHighlighted;
    }
}

namespace BinaryDiffViewer.ViewModels;

public sealed class HexCellViewModel
{
    public static readonly HexCellViewModel Empty = new(string.Empty, string.Empty, false, true);

    public string HexText { get; }
    public string AsciiText { get; }
    public bool IsDiff { get; }
    public bool IsEmpty { get; }

    public HexCellViewModel(string hexText, string asciiText, bool isDiff, bool isEmpty = false)
    {
        HexText = hexText;
        AsciiText = asciiText;
        IsDiff = isDiff;
        IsEmpty = isEmpty;
    }
}

using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.ViewModels;

public class DiffItemViewModel
{
    public string Offset { get; }
    public string FileAByte { get; }
    public string FileBByte { get; }
    public string DiffType { get; }

    public DiffItemViewModel(BinaryDiffItem item)
    {
        Offset = $"0x{item.Offset:X8}";
        FileAByte = item.ByteA.HasValue ? $"{item.ByteA.Value:X2}" : string.Empty;
        FileBByte = item.ByteB.HasValue ? $"{item.ByteB.Value:X2}" : string.Empty;
        DiffType = item.Type.ToString();
    }
}

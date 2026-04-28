using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BinaryDiffViewer.Commands;
using BinaryDiffViewer.Services;
using Microsoft.Win32;

namespace BinaryDiffViewer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly BinaryFileService _fileService = new();
    private readonly BinaryCompareService _compareService = new();

    private byte[] _bytesA = [];
    private byte[] _bytesB = [];

    private string _fileAPath = string.Empty;
    private string _fileASize = string.Empty;
    private string _fileAHexContent = string.Empty;

    private string _fileBPath = string.Empty;
    private string _fileBSize = string.Empty;
    private string _fileBHexContent = string.Empty;

    private string _compareStatus = "未比較";
    private string _diffCountText = "-";
    private string _firstDiffOffsetText = "-";
    private string _sameSizeText = "-";

    public string FileAPath
    {
        get => _fileAPath;
        private set { _fileAPath = value; OnPropertyChanged(); }
    }

    public string FileASize
    {
        get => _fileASize;
        private set { _fileASize = value; OnPropertyChanged(); }
    }

    public string FileAHexContent
    {
        get => _fileAHexContent;
        private set { _fileAHexContent = value; OnPropertyChanged(); }
    }

    public string FileBPath
    {
        get => _fileBPath;
        private set { _fileBPath = value; OnPropertyChanged(); }
    }

    public string FileBSize
    {
        get => _fileBSize;
        private set { _fileBSize = value; OnPropertyChanged(); }
    }

    public string FileBHexContent
    {
        get => _fileBHexContent;
        private set { _fileBHexContent = value; OnPropertyChanged(); }
    }

    public string CompareStatus
    {
        get => _compareStatus;
        private set { _compareStatus = value; OnPropertyChanged(); }
    }

    public string DiffCountText
    {
        get => _diffCountText;
        private set { _diffCountText = value; OnPropertyChanged(); }
    }

    public string FirstDiffOffsetText
    {
        get => _firstDiffOffsetText;
        private set { _firstDiffOffsetText = value; OnPropertyChanged(); }
    }

    public string SameSizeText
    {
        get => _sameSizeText;
        private set { _sameSizeText = value; OnPropertyChanged(); }
    }

    public ICommand OpenFileACommand { get; }
    public ICommand OpenFileBCommand { get; }
    public ICommand CompareCommand { get; }

    public MainViewModel()
    {
        OpenFileACommand = new RelayCommand(_ => OpenFile(isFileA: true));
        OpenFileBCommand = new RelayCommand(_ => OpenFile(isFileA: false));
        CompareCommand = new RelayCommand(
            _ => Compare(),
            _ => !string.IsNullOrEmpty(FileAPath) && !string.IsNullOrEmpty(FileBPath));
    }

    private void OpenFile(bool isFileA)
    {
        var dialog = new OpenFileDialog { Title = isFileA ? "ファイルAを開く" : "ファイルBを開く" };
        if (dialog.ShowDialog() != true) return;

        var path = dialog.FileName;
        var sizeText = BinaryFileService.FormatFileSize(_fileService.GetFileSize(path));
        var bytes = _fileService.ReadBytes(path);

        if (isFileA)
        {
            _bytesA = bytes;
            FileAPath = path;
            FileASize = sizeText;
        }
        else
        {
            _bytesB = bytes;
            FileBPath = path;
            FileBSize = sizeText;
        }

        ResetCompareResult();
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }

    private void Compare()
    {
        var result = _compareService.Compare(FileAPath, FileBPath);

        CompareStatus = result.AreIdentical ? "一致" : "差分あり";
        DiffCountText = $"{result.TotalDiffCount:N0} 件";
        FirstDiffOffsetText = result.FirstDiffOffset.HasValue
            ? $"0x{result.FirstDiffOffset.Value:X8}"
            : "なし";
        SameSizeText = result.SameSize ? "同じ" : "異なる";

        FileAHexContent = HexFormatter.GenerateDiffHexView(_bytesA, result.Diffs, isFileA: true);
        FileBHexContent = HexFormatter.GenerateDiffHexView(_bytesB, result.Diffs, isFileA: false);
    }

    private void ResetCompareResult()
    {
        CompareStatus = "未比較";
        DiffCountText = "-";
        FirstDiffOffsetText = "-";
        SameSizeText = "-";
        FileAHexContent = HexFormatter.GeneratePlainHexView(_bytesA);
        FileBHexContent = HexFormatter.GeneratePlainHexView(_bytesB);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

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
    private bool _isComparing;
    private CancellationTokenSource? _cts;

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
    private double _progress;
    private IReadOnlyList<DiffItemViewModel> _diffItems = [];

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

    public double Progress
    {
        get => _progress;
        private set { _progress = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<DiffItemViewModel> DiffItems
    {
        get => _diffItems;
        private set { _diffItems = value; OnPropertyChanged(); }
    }

    public ICommand OpenFileACommand { get; }
    public ICommand OpenFileBCommand { get; }
    public ICommand CompareCommand { get; }
    public ICommand CancelCommand { get; }

    public MainViewModel()
    {
        OpenFileACommand = new RelayCommand(_ => OpenFile(isFileA: true));
        OpenFileBCommand = new RelayCommand(_ => OpenFile(isFileA: false));
        CompareCommand = new RelayCommand(
            async _ => await CompareAsync(),
            _ => !string.IsNullOrEmpty(FileAPath) && !string.IsNullOrEmpty(FileBPath) && !_isComparing);
        CancelCommand = new RelayCommand(
            _ => _cts?.Cancel(),
            _ => _isComparing);
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
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task CompareAsync()
    {
        _isComparing = true;
        _cts = new CancellationTokenSource();
        CommandManager.InvalidateRequerySuggested();

        CompareStatus = "比較中...";
        DiffCountText = "-";
        FirstDiffOffsetText = "-";
        SameSizeText = "-";
        Progress = 0;
        DiffItems = [];

        try
        {
            var progress = new Progress<double>(p => Progress = p);
            var result = await _compareService.CompareAsync(
                FileAPath, FileBPath, progress, _cts.Token);

            CompareStatus = result.AreIdentical
                ? "2つのファイルは完全に一致しています"
                : "差分あり";
            DiffCountText = $"{result.TotalDiffCount:N0} 件";
            FirstDiffOffsetText = result.FirstDiffOffset.HasValue
                ? $"0x{result.FirstDiffOffset.Value:X8}"
                : "なし";
            SameSizeText = result.IsSameSize ? "同じ" : "異なる";

            FileAHexContent = HexFormatter.GenerateDiffHexView(_bytesA, result.DisplayDiffItems, isFileA: true);
            FileBHexContent = HexFormatter.GenerateDiffHexView(_bytesB, result.DisplayDiffItems, isFileA: false);

            DiffItems = result.DisplayDiffItems
                .Select(d => new DiffItemViewModel(d))
                .ToList();
        }
        catch (OperationCanceledException)
        {
            CompareStatus = "比較をキャンセルしました";
            DiffCountText = "-";
            FirstDiffOffsetText = "-";
            SameSizeText = "-";
            DiffItems = [];
        }
        catch (Exception ex)
        {
            CompareStatus = $"エラー: {ex.Message}";
            DiffCountText = "-";
            FirstDiffOffsetText = "-";
            SameSizeText = "-";
            DiffItems = [];
        }
        finally
        {
            Progress = 0;
            _cts?.Dispose();
            _cts = null;
            _isComparing = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private void ResetCompareResult()
    {
        CompareStatus = "未比較";
        DiffCountText = "-";
        FirstDiffOffsetText = "-";
        SameSizeText = "-";
        Progress = 0;
        FileAHexContent = HexFormatter.GeneratePlainHexView(_bytesA);
        FileBHexContent = HexFormatter.GeneratePlainHexView(_bytesB);
        DiffItems = [];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

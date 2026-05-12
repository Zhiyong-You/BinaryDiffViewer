using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BinaryDiffViewer.Commands;
using BinaryDiffViewer.Models;
using BinaryDiffViewer.Services;
using Microsoft.Win32;

namespace BinaryDiffViewer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const int BytesPerHexLine = 16;
    private const int FocusedHexViewLineCount = 256;
    private const int FocusedHexViewByteCount = FocusedHexViewLineCount * BytesPerHexLine;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    private readonly BinaryFileService _fileService = new();
    private readonly BinaryCompareService _compareService = new();
    private readonly DiffExportService _exportService = new();

    private byte[] _bytesA = [];
    private byte[] _bytesB = [];
    private CancellationTokenSource? _cts;
    private long _fileASizeBytes;
    private long _fileBSizeBytes;
    private long _lastTotalDiffCount;
    private bool _hasComparisonResult;

    private string _fileAPath = string.Empty;
    private string _fileASize = string.Empty;
    private IReadOnlyList<HexLineViewModel> _fileAHexLines = [];

    private string _fileBPath = string.Empty;
    private string _fileBSize = string.Empty;
    private IReadOnlyList<HexLineViewModel> _fileBHexLines = [];

    private string _compareStatus = "未比較";
    private string _diffCountText = "-";
    private string _firstDiffOffsetText = "-";
    private string _sameSizeText = "-";
    private string _selectedDiffOffsetText = "-";
    private string _progressText = "0.0 MB / 0.0 MB (0.0%)";
    private double _progressPercentage;
    private double _processedMegaBytes;
    private double _totalMegaBytes;
    private CompareUiState _currentUiState = CompareUiState.Idle;
    private IReadOnlyList<DiffItemViewModel> _diffItems = [];
    private IReadOnlyList<BinaryDiffItem> _lastDisplayDiffItems = [];

    public string FileAPath
    {
        get => _fileAPath;
        private set
        {
            if (_fileAPath == value) return;
            _fileAPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCompareFiles));
        }
    }

    public string FileASize
    {
        get => _fileASize;
        private set { _fileASize = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<HexLineViewModel> FileAHexLines
    {
        get => _fileAHexLines;
        private set { _fileAHexLines = value; OnPropertyChanged(); }
    }

    public string FileBPath
    {
        get => _fileBPath;
        private set
        {
            if (_fileBPath == value) return;
            _fileBPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCompareFiles));
        }
    }

    public string FileBSize
    {
        get => _fileBSize;
        private set { _fileBSize = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<HexLineViewModel> FileBHexLines
    {
        get => _fileBHexLines;
        private set { _fileBHexLines = value; OnPropertyChanged(); }
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

    public string SelectedDiffOffsetText
    {
        get => _selectedDiffOffsetText;
        private set { _selectedDiffOffsetText = value; OnPropertyChanged(); }
    }

    public string ProgressText
    {
        get => _progressText;
        private set { _progressText = value; OnPropertyChanged(); }
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        private set { _progressPercentage = value; OnPropertyChanged(); }
    }

    public double ProcessedMegaBytes
    {
        get => _processedMegaBytes;
        private set { _processedMegaBytes = value; OnPropertyChanged(); }
    }

    public double TotalMegaBytes
    {
        get => _totalMegaBytes;
        private set { _totalMegaBytes = value; OnPropertyChanged(); }
    }

    public double CompareProgress => ProgressPercentage / 100.0;
    public string ProcessedText => $"{ProcessedMegaBytes:F1} MB / {TotalMegaBytes:F1} MB";
    public string ProgressPercentageText => $"{ProgressPercentage:F0}%";

    public CompareUiState CurrentUiState
    {
        get => _currentUiState;
        private set
        {
            if (_currentUiState == value) return;

            _currentUiState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsComparing));
            OnPropertyChanged(nameof(IsStatisticsMode));
            OnPropertyChanged(nameof(IsFileSelectionEnabled));
            OnPropertyChanged(nameof(IsDiffItemsEnabled));
            OnPropertyChanged(nameof(CanCompareFiles));
            OnPropertyChanged(nameof(CanExportResults));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public IReadOnlyList<DiffItemViewModel> DiffItems
    {
        get => _diffItems;
        private set { _diffItems = value; OnPropertyChanged(); }
    }

    public bool IsComparing => CurrentUiState == CompareUiState.Comparing;

    public bool IsStatisticsMode => !IsComparing;

    public bool IsFileSelectionEnabled => !IsComparing;

    public bool IsDiffItemsEnabled => !IsComparing;

    public bool HasComparisonResult => _hasComparisonResult;

    public bool HasDifferences => _hasComparisonResult && _lastTotalDiffCount > 0;

    public bool CanCompareFiles => HasBothFiles && !IsComparing;

    public bool CanExportResults => CurrentUiState == CompareUiState.Completed && HasComparisonResult;

    public bool HasDiffLimit => _hasComparisonResult && _lastDisplayDiffItems.Count < _lastTotalDiffCount;
    public string DiffLimitText => HasDiffLimit
        ? $"表示件数上限: {_lastDisplayDiffItems.Count:N0} 件  /  全 {_lastTotalDiffCount:N0} 件"
        : string.Empty;

    public ICommand OpenFileACommand { get; }
    public ICommand OpenFileBCommand { get; }
    public ICommand CompareCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public MainViewModel()
    {
        OpenFileACommand = new RelayCommand(_ => OpenFile(isFileA: true), _ => IsFileSelectionEnabled);
        OpenFileBCommand = new RelayCommand(_ => OpenFile(isFileA: false), _ => IsFileSelectionEnabled);
        CompareCommand = new RelayCommand(async _ => await CompareAsync(), _ => CanCompareFiles);
        CancelCommand = new RelayCommand(_ => _cts?.Cancel(), _ => IsComparing);
        ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync(), _ => CanExportResults);

        UpdateProgress(0, 0);
    }

    private bool HasBothFiles => !string.IsNullOrWhiteSpace(FileAPath) && !string.IsNullOrWhiteSpace(FileBPath);

    private void OpenFile(bool isFileA)
    {
        var dialog = new OpenFileDialog { Title = isFileA ? "ファイルAを開く" : "ファイルBを開く" };
        if (dialog.ShowDialog() != true) return;

        var path = dialog.FileName;
        var sizeBytes = _fileService.GetFileSize(path);
        var sizeText = BinaryFileService.FormatFileSize(sizeBytes);
        var bytes = _fileService.ReadBytes(path);

        if (isFileA)
        {
            _bytesA = bytes;
            _fileASizeBytes = sizeBytes;
            FileAPath = path;
            FileASize = sizeText;
        }
        else
        {
            _bytesB = bytes;
            _fileBSizeBytes = sizeBytes;
            FileBPath = path;
            FileBSize = sizeText;
        }

        ResetCompareResult();
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task CompareAsync()
    {
        _cts = new CancellationTokenSource();

        PrepareForComparison();

        try
        {
            var progress = new Progress<BinaryCompareProgress>(UpdateProgress);
            var result = await _compareService.CompareAsync(
                FileAPath, FileBPath, progress, _cts.Token);

            ApplyCompareResult(result);
        }
        catch (OperationCanceledException)
        {
            ApplyCanceledState();
        }
        catch (Exception ex)
        {
            ApplyErrorState(ex.Message);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "CSV出力先を選択",
            Filter = "CSVファイル (*.csv)|*.csv|すべてのファイル (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "diff_result.csv"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            await _exportService.ExportToCsvAsync(dialog.FileName, _lastDisplayDiffItems);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"CSV出力に失敗しました: {ex.Message}",
                "BinaryDiffViewer",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void PrepareForComparison()
    {
        ClearComparisonDetails();
        ResetHexPreview();
        UpdateProgress(0, Math.Max(_fileASizeBytes, _fileBSizeBytes));
        CompareStatus = "比較中...";
        CurrentUiState = CompareUiState.Comparing;
    }

    private void ApplyCompareResult(BinaryDiffResult result)
    {
        _hasComparisonResult = true;
        _lastTotalDiffCount = result.TotalDiffCount;
        NotifyComparisonStateChanged();

        CompareStatus = result.AreIdentical ? "一致" : "差分あり";
        DiffCountText = $"{result.TotalDiffCount:N0} 件";
        FirstDiffOffsetText = result.FirstDiffOffset.HasValue
            ? $"0x{result.FirstDiffOffset.Value:X8}"
            : "なし";
        SameSizeText = result.IsSameSize ? "同じ" : "異なる";

        FileAHexLines = HexViewBuilder.BuildDiff(_bytesA, result.DisplayDiffItems, isFileA: true);
        FileBHexLines = HexViewBuilder.BuildDiff(_bytesB, result.DisplayDiffItems, isFileA: false);

        _lastDisplayDiffItems = result.DisplayDiffItems;
        DiffItems = result.DisplayDiffItems
            .Select(d => new DiffItemViewModel(d))
            .ToList();

        var totalBytes = Math.Max(result.FileASize, result.FileBSize);
        UpdateProgress(totalBytes, totalBytes);
        CurrentUiState = CompareUiState.Completed;
    }

    private void ApplyCanceledState()
    {
        ClearComparisonDetails();
        ResetHexPreview();
        CompareStatus = "キャンセルしました";
        CurrentUiState = CompareUiState.Canceled;
    }

    private void ApplyErrorState(string message)
    {
        ClearComparisonDetails();
        ResetHexPreview();
        CompareStatus = $"エラー: {message}";
        CurrentUiState = CompareUiState.Error;
    }

    private void ResetCompareResult()
    {
        ClearComparisonDetails();
        ResetHexPreview();
        UpdateProgress(0, Math.Max(_fileASizeBytes, _fileBSizeBytes));
        CurrentUiState = HasBothFiles ? CompareUiState.Ready : CompareUiState.Idle;
        CompareStatus = CurrentUiState == CompareUiState.Ready ? "比較可能" : "未比較";
    }

    private void ClearComparisonDetails()
    {
        _hasComparisonResult = false;
        _lastTotalDiffCount = 0;
        NotifyComparisonStateChanged();

        DiffCountText = "-";
        FirstDiffOffsetText = "-";
        SameSizeText = "-";
        SelectedDiffOffsetText = "-";
        DiffItems = [];
        _lastDisplayDiffItems = [];
    }

    private void ResetHexPreview()
    {
        FileAHexLines = HexViewBuilder.BuildPlain(_bytesA);
        FileBHexLines = HexViewBuilder.BuildPlain(_bytesB);
    }

    private void UpdateProgress(BinaryCompareProgress progress)
    {
        UpdateProgress(progress.ProcessedBytes, progress.TotalBytes);
    }

    private void UpdateProgress(long processedBytes, long totalBytes)
    {
        ProcessedMegaBytes = processedBytes / BytesPerMegabyte;
        TotalMegaBytes = totalBytes / BytesPerMegabyte;
        ProgressPercentage = totalBytes > 0
            ? Math.Min(100.0, processedBytes * 100.0 / totalBytes)
            : 0;
        ProgressText = $"{ProcessedMegaBytes:F1} MB / {TotalMegaBytes:F1} MB ({ProgressPercentage:F1}%)";
        OnPropertyChanged(nameof(CompareProgress));
        OnPropertyChanged(nameof(ProcessedText));
        OnPropertyChanged(nameof(ProgressPercentageText));
    }

    private void NotifyComparisonStateChanged()
    {
        OnPropertyChanged(nameof(HasComparisonResult));
        OnPropertyChanged(nameof(HasDifferences));
        OnPropertyChanged(nameof(CanExportResults));
        OnPropertyChanged(nameof(HasDiffLimit));
        OnPropertyChanged(nameof(DiffLimitText));
    }

    public void SetSelectedDiffOffset(long? offset)
    {
        SelectedDiffOffsetText = offset.HasValue
            ? $"0x{offset.Value:X8}"
            : "-";
    }

    public int FocusHexViewOnOffset(long offset)
    {
        SetSelectedDiffOffset(offset);

        if (!HasBothFiles)
            return 0;

        var selectedLineIndex = offset / BytesPerHexLine;
        var startLineIndex = Math.Max(selectedLineIndex - (FocusedHexViewLineCount / 2), 0);
        var startOffset = startLineIndex * BytesPerHexLine;

        FileAHexLines = BuildFocusedHexLines(FileAPath, startOffset, isFileA: true, offset);
        FileBHexLines = BuildFocusedHexLines(FileBPath, startOffset, isFileA: false, offset);

        return (int)(selectedLineIndex - startLineIndex);
    }

    private IReadOnlyList<HexLineViewModel> BuildFocusedHexLines(
        string filePath, long startOffset, bool isFileA, long highlightOffset)
    {
        var bytes = _fileService.ReadBytes(filePath, startOffset, FocusedHexViewByteCount);
        return HexViewBuilder.BuildDiff(bytes, _lastDisplayDiffItems, isFileA, startOffset, highlightOffset);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

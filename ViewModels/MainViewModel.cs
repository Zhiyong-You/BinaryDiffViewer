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

    private string _filePath = string.Empty;
    private string _fileSize = string.Empty;
    private string _hexContent = string.Empty;

    public string FilePath
    {
        get => _filePath;
        private set { _filePath = value; OnPropertyChanged(); }
    }

    public string FileSize
    {
        get => _fileSize;
        private set { _fileSize = value; OnPropertyChanged(); }
    }

    public string HexContent
    {
        get => _hexContent;
        private set { _hexContent = value; OnPropertyChanged(); }
    }

    public ICommand OpenFileCommand { get; }

    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(_ => OpenFile());
    }

    private void OpenFile()
    {
        var dialog = new OpenFileDialog { Title = "ファイルを開く" };
        if (dialog.ShowDialog() != true) return;

        FilePath = dialog.FileName;
        var size = _fileService.GetFileSize(dialog.FileName);
        FileSize = BinaryFileService.FormatFileSize(size);
        var bytes = _fileService.ReadBytes(dialog.FileName);
        HexContent = _fileService.GenerateHexView(bytes);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

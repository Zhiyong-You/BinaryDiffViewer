using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BinaryDiffViewer;

public partial class MainWindow : Window
{
    private ScrollViewer? _fileAHexScrollViewer;
    private ScrollViewer? _fileBHexScrollViewer;
    private bool _isSyncingHexScroll;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AttachHexScrollSync();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        DetachHexScrollSync();
    }

    private void AttachHexScrollSync()
    {
        DetachHexScrollSync();

        _fileAHexScrollViewer = FindScrollViewer(FileAHexTextBox);
        _fileBHexScrollViewer = FindScrollViewer(FileBHexTextBox);

        if (_fileAHexScrollViewer is null || _fileBHexScrollViewer is null)
            return;

        _fileAHexScrollViewer.ScrollChanged += HexScrollViewer_ScrollChanged;
        _fileBHexScrollViewer.ScrollChanged += HexScrollViewer_ScrollChanged;
    }

    private void DetachHexScrollSync()
    {
        if (_fileAHexScrollViewer is not null)
            _fileAHexScrollViewer.ScrollChanged -= HexScrollViewer_ScrollChanged;

        if (_fileBHexScrollViewer is not null)
            _fileBHexScrollViewer.ScrollChanged -= HexScrollViewer_ScrollChanged;

        _fileAHexScrollViewer = null;
        _fileBHexScrollViewer = null;
        _isSyncingHexScroll = false;
    }

    private void HexScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSyncingHexScroll || sender is not ScrollViewer sourceScrollViewer)
            return;

        if (e.VerticalChange == 0 && e.HorizontalChange == 0)
            return;

        var targetScrollViewer = ReferenceEquals(sourceScrollViewer, _fileAHexScrollViewer)
            ? _fileBHexScrollViewer
            : _fileAHexScrollViewer;

        if (targetScrollViewer is null)
            return;

        _isSyncingHexScroll = true;
        try
        {
            var targetVerticalOffset = Math.Min(sourceScrollViewer.VerticalOffset, targetScrollViewer.ScrollableHeight);
            var targetHorizontalOffset = Math.Min(sourceScrollViewer.HorizontalOffset, targetScrollViewer.ScrollableWidth);

            if (!AreClose(targetScrollViewer.VerticalOffset, targetVerticalOffset))
                targetScrollViewer.ScrollToVerticalOffset(targetVerticalOffset);

            if (!AreClose(targetScrollViewer.HorizontalOffset, targetHorizontalOffset))
                targetScrollViewer.ScrollToHorizontalOffset(targetHorizontalOffset);
        }
        finally
        {
            _isSyncingHexScroll = false;
        }
    }

    private void DiffItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel viewModel)
            return;

        if (sender is not DataGrid dataGrid || dataGrid.SelectedItem is not ViewModels.DiffItemViewModel selectedDiff)
        {
            viewModel.SetSelectedDiffOffset(null);
            return;
        }

        JumpHexViewsToOffset(viewModel, selectedDiff.OffsetValue);
    }

    private void JumpHexViewsToOffset(ViewModels.MainViewModel viewModel, long offset)
    {
        var localLineIndex = viewModel.FocusHexViewOnOffset(offset);

        Dispatcher.BeginInvoke(() =>
        {
            if (_fileAHexScrollViewer is null || _fileBHexScrollViewer is null)
                AttachHexScrollSync();

            UpdateLayout();
            FileAHexTextBox.UpdateLayout();
            FileBHexTextBox.UpdateLayout();

            _isSyncingHexScroll = true;
            try
            {
                ScrollHexTextBoxToLine(FileAHexTextBox, _fileAHexScrollViewer, localLineIndex);
                ScrollHexTextBoxToLine(FileBHexTextBox, _fileBHexScrollViewer, localLineIndex);

                _fileAHexScrollViewer?.ScrollToHorizontalOffset(0);
                _fileBHexScrollViewer?.ScrollToHorizontalOffset(0);
            }
            finally
            {
                _isSyncingHexScroll = false;
            }
        }, DispatcherPriority.Background);
    }

    private static void ScrollHexTextBoxToLine(TextBox textBox, ScrollViewer? scrollViewer, long targetLineIndex)
    {
        if (scrollViewer is null || string.IsNullOrEmpty(textBox.Text))
            return;

        var maxLineIndex = Math.Max(textBox.LineCount - 1, 0);
        var clampedLineIndex = ClampLineIndex(targetLineIndex, maxLineIndex);
        var visibleLineCount = EstimateVisibleLineCount(textBox, scrollViewer);
        var topLineIndex = Math.Max(clampedLineIndex - (visibleLineCount / 2), 0);

        textBox.ScrollToLine(topLineIndex);
    }

    private static int ClampLineIndex(long lineIndex, int maxLineIndex)
    {
        if (lineIndex <= 0)
            return 0;

        if (lineIndex >= maxLineIndex)
            return maxLineIndex;

        return (int)lineIndex;
    }

    private static int EstimateVisibleLineCount(TextBox textBox, ScrollViewer scrollViewer)
    {
        var lineHeight = textBox.FontFamily.LineSpacing > 0
            ? textBox.FontFamily.LineSpacing * textBox.FontSize
            : textBox.FontSize * 1.2;

        if (lineHeight <= 0 || scrollViewer.ViewportHeight <= 0)
            return 1;

        return Math.Max(1, (int)(scrollViewer.ViewportHeight / lineHeight));
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer scrollViewer)
            return scrollViewer;

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var descendant = FindScrollViewer(child);
            if (descendant is not null)
                return descendant;
        }

        return null;
    }

    private static bool AreClose(double left, double right)
    {
        return Math.Abs(left - right) < 0.5;
    }
}

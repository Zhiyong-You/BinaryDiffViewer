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

        _fileAHexScrollViewer = FindScrollViewer(FileAHexListBox);
        _fileBHexScrollViewer = FindScrollViewer(FileBHexListBox);

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
        if (_isSyncingHexScroll || sender is not ScrollViewer src) return;
        if (e.VerticalChange == 0 && e.HorizontalChange == 0) return;

        var target = ReferenceEquals(src, _fileAHexScrollViewer)
            ? _fileBHexScrollViewer
            : _fileAHexScrollViewer;

        if (target is null) return;

        _isSyncingHexScroll = true;
        try
        {
            var v = Math.Min(src.VerticalOffset, target.ScrollableHeight);
            var h = Math.Min(src.HorizontalOffset, target.ScrollableWidth);
            if (!AreClose(target.VerticalOffset, v)) target.ScrollToVerticalOffset(v);
            if (!AreClose(target.HorizontalOffset, h)) target.ScrollToHorizontalOffset(h);
        }
        finally
        {
            _isSyncingHexScroll = false;
        }
    }

    private void DiffItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ViewModels.MainViewModel vm) return;

        if (sender is not DataGrid grid || grid.SelectedItem is not ViewModels.DiffItemViewModel selected)
        {
            vm.SetSelectedDiffOffset(null);
            return;
        }

        JumpHexViewsToOffset(vm, selected.OffsetValue);
    }

    private void JumpHexViewsToOffset(ViewModels.MainViewModel vm, long offset)
    {
        var localLineIndex = vm.FocusHexViewOnOffset(offset);

        Dispatcher.BeginInvoke(() =>
        {
            if (_fileAHexScrollViewer is null || _fileBHexScrollViewer is null)
                AttachHexScrollSync();

            UpdateLayout();
            FileAHexListBox.UpdateLayout();
            FileBHexListBox.UpdateLayout();

            _isSyncingHexScroll = true;
            try
            {
                ScrollListBoxToLine(FileAHexListBox, _fileAHexScrollViewer, localLineIndex);
                ScrollListBoxToLine(FileBHexListBox, _fileBHexScrollViewer, localLineIndex);
                _fileAHexScrollViewer?.ScrollToHorizontalOffset(0);
                _fileBHexScrollViewer?.ScrollToHorizontalOffset(0);
            }
            finally
            {
                _isSyncingHexScroll = false;
            }
        }, DispatcherPriority.Background);
    }

    private static void ScrollListBoxToLine(ListBox listBox, ScrollViewer? scrollViewer, int targetLineIndex)
    {
        if (scrollViewer is null || listBox.Items.Count == 0) return;

        var clamped = Math.Max(0, Math.Min(targetLineIndex, listBox.Items.Count - 1));

        // 実アイテム高さを取得してセンタリングオフセットを計算
        if (listBox.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement first
            && first.ActualHeight > 0)
        {
            var itemH = first.ActualHeight;
            var viewH = scrollViewer.ViewportHeight;
            var topLine = Math.Max(0, clamped - (int)(viewH / itemH / 2));
            scrollViewer.ScrollToVerticalOffset(topLine * itemH);
        }
        else
        {
            listBox.ScrollIntoView(listBox.Items[clamped]);
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer sv) return sv;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (found is not null) return found;
        }
        return null;
    }

    private static bool AreClose(double a, double b) => Math.Abs(a - b) < 0.5;
}

using System.Windows;
using System.Windows.Input;
using Scaidome.OpcUa.Browser.ViewModels;

namespace Scaidome.OpcUa.Browser.Views;

/// <summary>Only view mechanics live here: drag and drop and double click, which forward to view model commands.</summary>
public partial class MainWindow : Window
{
    private Point _dragStart;
    private AddressSpaceNodeViewModel? _dragCandidate;

    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnTreeDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NodeUnder(e.OriginalSource) is { IsVariable: true } node)
        {
            ViewModel.MonitorNodeCommand.Execute(node);
            e.Handled = true;
        }
    }

    private void OnTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(AddressSpaceTree);
        _dragCandidate = NodeUnder(e.OriginalSource) is { IsVariable: true } node ? node : null;
    }

    private void OnTreePreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragCandidate is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var delta = e.GetPosition(AddressSpaceTree) - _dragStart;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var node = _dragCandidate;
        _dragCandidate = null;
        DragDrop.DoDragDrop(AddressSpaceTree, node, DragDropEffects.Copy);
    }

    private void OnMonitoredItemsDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(AddressSpaceNodeViewModel)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnMonitoredItemsDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(AddressSpaceNodeViewModel)) is AddressSpaceNodeViewModel node)
            ViewModel.MonitorNodeCommand.Execute(node);
    }

    private static AddressSpaceNodeViewModel? NodeUnder(object originalSource) => originalSource switch
    {
        FrameworkElement element => element.DataContext as AddressSpaceNodeViewModel,
        FrameworkContentElement element => element.DataContext as AddressSpaceNodeViewModel,
        _ => null,
    };
}

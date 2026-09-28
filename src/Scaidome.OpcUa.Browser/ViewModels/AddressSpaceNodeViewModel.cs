using System.Collections.ObjectModel;
using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Client;

namespace Scaidome.OpcUa.Browser.ViewModels;

/// <summary>
/// One node in the address space tree. Children are browsed the first time the node is expanded; until then a
/// placeholder child keeps the expander visible.
/// </summary>
public sealed class AddressSpaceNodeViewModel : ObservableObject
{
    private readonly Func<string, Task<BrowseNode[]>>? _browse;
    private bool _isExpanded;
    private bool _isSelected;
    private bool _childrenLoaded;

    public AddressSpaceNodeViewModel(BrowseNode node, Func<string, Task<BrowseNode[]>> browse)
    {
        Node = node;
        _browse = browse;

        if (node.IsFolder)
            Children.Add(Placeholder("Loading…"));
    }

    private AddressSpaceNodeViewModel(BrowseNode node)
    {
        Node = node;
        IsPlaceholder = true;
    }

    public BrowseNode Node { get; }

    public ObservableCollection<AddressSpaceNodeViewModel> Children { get; } = [];

    /// <summary>A "Loading…" or error row, not a real node.</summary>
    public bool IsPlaceholder { get; }

    public bool IsVariable => Node.NodeClass == "Variable";

    public string Icon => IsPlaceholder ? "" : Node.NodeClass switch
    {
        "Variable" => "📊",
        "Method" => "🔧",
        "Object" => "📁",
        _ => "📄",
    };

    public string ToolTip => IsPlaceholder ? Node.DisplayName : $"{Node.NodeClass}  {Node.NodeId}";

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value) && value && !_childrenLoaded)
                _ = LoadChildrenAsync();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private async Task LoadChildrenAsync()
    {
        if (_browse is null)
            return;

        _childrenLoaded = true;
        try
        {
            var children = await _browse(Node.NodeId);

            Children.Clear();
            foreach (var child in children)
                Children.Add(new AddressSpaceNodeViewModel(child, _browse));
        }
        catch (Exception ex)
        {
            Children.Clear();
            Children.Add(Placeholder($"Browse failed: {ex.Message}"));
            _childrenLoaded = false; // collapse and expand again to retry
        }
    }

    private static AddressSpaceNodeViewModel Placeholder(string text)
        => new(new BrowseNode(text, "", false, "", "", null));
}

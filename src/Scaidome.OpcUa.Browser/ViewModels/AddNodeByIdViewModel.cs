using System.Text.RegularExpressions;
using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Browser.Services;

namespace Scaidome.OpcUa.Browser.ViewModels;

/// <summary>Adds a node to the monitored items by typing its node id, for nodes that are hard to find in the tree.</summary>
public sealed partial class AddNodeByIdViewModel : OverlayViewModel
{
    private readonly OpcUaConnection _connection;
    private readonly Func<string, string, Task> _addNode;

    private string _nodeId = "";
    private string? _errorMessage;

    public AddNodeByIdViewModel(OpcUaConnection connection, Func<string, string, Task> addNode)
    {
        _connection = connection;
        _addNode = addNode;

        OpenCommand = new RelayCommand(Open);
        AddCommand = new AsyncRelayCommand(AddAsync);
    }

    public RelayCommand OpenCommand { get; }

    public AsyncRelayCommand AddCommand { get; }

    public string NodeId
    {
        get => _nodeId;
        set => SetProperty(ref _nodeId, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    private void Open()
    {
        NodeId = "";
        ErrorMessage = null;
        IsOpen = true;
    }

    private async Task AddAsync()
    {
        var nodeId = NodeId.Trim();
        if (nodeId.Length == 0)
        {
            ErrorMessage = "Enter a node id.";
            return;
        }

        if (!NodeIdFormat().IsMatch(nodeId))
        {
            ErrorMessage = "Invalid node id. Use i=, s=, g= or b= for the identifier, optionally prefixed by ns=<index>; — e.g. ns=2;s=MyVariable or i=2258.";
            return;
        }

        ErrorMessage = null;

        // Reading the value both proves the node exists and that it is a variable (objects have no Value attribute).
        var read = await _connection.ReadValueAsync(nodeId);
        switch (read.StatusCode)
        {
            case StatusCodes.BadNodeIdUnknown:
            case StatusCodes.BadNodeIdInvalid:
                ErrorMessage = "The node does not exist on the server.";
                return;
            case StatusCodes.BadAttributeIdInvalid:
                ErrorMessage = "The node is not a variable, so it has no value to monitor.";
                return;
            case StatusCodes.Bad:
                ErrorMessage = read.Error ?? "The node could not be read.";
                return;
        }

        var attributes = await _connection.ReadAttributesAsync(nodeId);
        await _addNode(nodeId, attributes.DisplayName);
        Close();
    }

    // Identifier must be i= (numeric), s= (string), g= (guid) or b= (opaque/base64); ns=<index>; is optional.
    [GeneratedRegex(@"^(ns=\d+;)?[isgb]=.+$", RegexOptions.IgnoreCase)]
    private static partial Regex NodeIdFormat();
}

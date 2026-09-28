using System.Collections.ObjectModel;
using System.Windows.Threading;
using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Browser.Services;
using Scaidome.OpcUa.Client;

namespace Scaidome.OpcUa.Browser.ViewModels;

public enum ConnectionStatus
{
    Disconnected,
    Connected,
    Reconnecting,
}

public sealed class MainViewModel : ObservableObject
{
    private const string RootFolderNodeId = "i=84";
    private const string ObjectsFolderNodeId = "i=85";

    private readonly OpcUaConnection _connection;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _statusTimer;

    private bool _isConnected;
    private ConnectionStatus _status;
    private string _statusText = "Not connected";
    private string? _notification;

    public MainViewModel(OpcUaConnection connection, RecentConnectionsStore recentConnections)
    {
        _connection = connection;
        _dispatcher = Dispatcher.CurrentDispatcher;

        Connect = new ConnectViewModel(connection, recentConnections, OnConnectedAsync);
        WriteValue = new WriteValueViewModel(connection);
        AddNodeById = new AddNodeByIdViewModel(connection, MonitorNodeAsync);

        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected);
        MonitorNodeCommand = AsyncRelayCommand.Create<AddressSpaceNodeViewModel>(node => MonitorNodeAsync(node.Node.NodeId, node.Node.DisplayName), node => node.IsVariable);
        ReadItemCommand = AsyncRelayCommand.Create<MonitoredItemViewModel>(ReadItemAsync);
        WriteItemCommand = AsyncRelayCommand.Create<MonitoredItemViewModel>(WriteValue.OpenAsync);
        RemoveItemCommand = AsyncRelayCommand.Create<MonitoredItemViewModel>(RemoveItemAsync);
        DismissNotificationCommand = new RelayCommand(() => Notification = null);
        CloseDialogsCommand = new RelayCommand(CloseDialogs);

        _connection.DataChanged += OnDataChanged;

        // The client exposes its state (connected / reconnecting) but raises no event for it, so poll.
        _statusTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => RefreshStatus(), _dispatcher);
    }

    public ConnectViewModel Connect { get; }

    public WriteValueViewModel WriteValue { get; }

    public AddNodeByIdViewModel AddNodeById { get; }

    public ObservableCollection<AddressSpaceNodeViewModel> RootNodes { get; } = [];

    public ObservableCollection<MonitoredItemViewModel> MonitoredItems { get; } = [];

    public AsyncRelayCommand DisconnectCommand { get; }

    public AsyncRelayCommand MonitorNodeCommand { get; }

    public AsyncRelayCommand ReadItemCommand { get; }

    public AsyncRelayCommand WriteItemCommand { get; }

    public AsyncRelayCommand RemoveItemCommand { get; }

    public RelayCommand DismissNotificationCommand { get; }

    public RelayCommand CloseDialogsCommand { get; }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    public ConnectionStatus Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>A dismissible message shown above the monitored items, e.g. when a node could not be added.</summary>
    public string? Notification
    {
        get => _notification;
        private set => SetProperty(ref _notification, value);
    }

    public async Task MonitorNodeAsync(string nodeId, string displayName)
    {
        if (MonitoredItems.Any(i => i.NodeId == nodeId))
            return;

        var item = new MonitoredItemViewModel(nodeId, displayName);
        MonitoredItems.Add(item);

        var monitoredNode = await _connection.MonitorAsync(nodeId, displayName, item);
        if (monitoredNode is null)
        {
            MonitoredItems.Remove(item);
            Notification = $"Could not monitor '{displayName}': invalid node id {nodeId}.";
            return;
        }

        item.MonitoredNodeId = monitoredNode.Id;
    }

    private async Task OnConnectedAsync()
    {
        IsConnected = true;
        Notification = null;
        MonitoredItems.Clear();
        RootNodes.Clear();
        RefreshStatus();
        _statusTimer.Start();

        foreach (var node in await _connection.BrowseAsync(RootFolderNodeId))
        {
            var viewModel = new AddressSpaceNodeViewModel(node, id => _connection.BrowseAsync(id));
            RootNodes.Add(viewModel);

            if (node.NodeId == ObjectsFolderNodeId)
                viewModel.IsExpanded = true;
        }
    }

    private async Task DisconnectAsync()
    {
        _statusTimer.Stop();
        await _connection.DisconnectAsync();

        MonitoredItems.Clear();
        RootNodes.Clear();
        Notification = null;
        IsConnected = false;
        RefreshStatus();
    }

    private void CloseDialogs()
    {
        if (Connect.IsOpen)
            Connect.Close();
        if (WriteValue.IsOpen)
            WriteValue.Close();
        if (AddNodeById.IsOpen)
            AddNodeById.Close();
    }

    private async Task ReadItemAsync(MonitoredItemViewModel item)
    {
        item.Apply(await _connection.ReadValueAsync(item.NodeId));
    }

    private async Task RemoveItemAsync(MonitoredItemViewModel item)
    {
        MonitoredItems.Remove(item);

        if (item.MonitoredNodeId is { } id && _connection.IsConnected)
            await _connection.StopMonitoringAsync(id);
    }

    // Called on an OPC UA stack thread.
    private void OnDataChanged(DataValue[] values)
    {
        _dispatcher.InvokeAsync(() =>
        {
            foreach (var value in values)
            {
                // The context passed to MonitorAsync is the row itself, so no lookup is needed.
                if (value.Context is MonitoredNode { Context: MonitoredItemViewModel item })
                    item.Apply(value);
            }
        });
    }

    private void RefreshStatus()
    {
        if (!_connection.IsConnected)
        {
            Status = ConnectionStatus.Disconnected;
            StatusText = "Not connected";
            return;
        }

        (Status, StatusText) = _connection.State switch
        {
            ClientState.Connected => (ConnectionStatus.Connected, $"Connected to {_connection.ServerUrl}"),
            ClientState.Reconnecting => (ConnectionStatus.Reconnecting, $"Connection to {_connection.ServerUrl} lost — reconnecting…"),
            _ => (ConnectionStatus.Disconnected, $"Disconnected from {_connection.ServerUrl}"),
        };
    }
}

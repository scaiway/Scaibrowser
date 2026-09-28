using Microsoft.Extensions.Logging;
using Scaidome.OpcUa.Client;

namespace Scaidome.OpcUa.Browser.Services;

/// <summary>
/// The browser's single connection to an OPC UA server: owns the <see cref="IClient"/> and one subscription that all
/// monitored items are added to and removed from.
/// </summary>
/// <remarks>
/// Subscription changes are serialized with a semaphore — the client's subscription isn't safe for concurrent
/// ApplyChanges, and two quick drops onto the grid would otherwise overlap. This is the part of the old API's
/// gateway (one operation at a time) that still matters when the client is used in-process.
/// </remarks>
public sealed class OpcUaConnection : IAsyncDisposable
{
    public const string DefaultServerUrl = "opc.tcp://localhost:62541/Quickstarts/ReferenceServer";

    private readonly ILoggerFactory _loggerFactory;
    private readonly ClientLogCapture _logCapture;
    private readonly SemaphoreSlim _subscriptionLock = new(1, 1);

    private IClient? _client;
    private ISubscription? _subscription;

    public OpcUaConnection(ILoggerFactory loggerFactory, ClientLogCapture logCapture)
    {
        _loggerFactory = loggerFactory;
        _logCapture = logCapture;
    }

    /// <summary>Raised on an OPC UA stack thread — marshal to the UI thread before touching bindings.</summary>
    public event Action<DataValue[]>? DataChanged;

    public bool IsConnected => _client != null;

    public ClientState State => _client?.State ?? ClientState.Disconnected;

    public string? ServerUrl => _client?.Url;

    /// <exception cref="ConnectionFailedException">The server could not be reached or refused the session.</exception>
    public async Task ConnectAsync(ConnectionSettings settings, CancellationToken ct = default)
    {
        await DisconnectAsync();

        var hasUser = !string.IsNullOrWhiteSpace(settings.UserName);
        var options = new ClientOptions
        {
            ApplicationName = "Scaidome Browser",
            Url = settings.ServerUrl,
            UseSecurity = settings.UseSecurity,
            AutoAccept = settings.AutoAcceptServerCertificate,
            UserName = hasUser ? settings.UserName : null,
            UserPassword = hasUser ? settings.Password : null,
        };

        IClient? client = null;
        try
        {
            _logCapture.ClearLastError();
            client = await ClientFactory.CreateAsync(options, _loggerFactory);

            if (!await client.ConnectAsync(ct))
            {
                ct.ThrowIfCancellationRequested();
                throw new ConnectionFailedException(_logCapture.LastError ?? "The server refused the connection.");
            }

            var subscription = await client.AddSubscriptionAsync(new SubscriptionOptions
            {
                DisplayName = "Scaidome.OpcUa.Browser",
                OnDataChanged = values => DataChanged?.Invoke(values),
            }, ct);

            _client = client;
            _subscription = subscription;
        }
        catch
        {
            if (client != null)
            {
                await client.DisconnectAsync();
                client.Dispose();
            }
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        var client = _client;
        _client = null;
        _subscription = null;

        if (client != null)
        {
            // Closes the session and deletes its subscriptions; the client logs rather than throws on failure.
            await client.DisconnectAsync();
            client.Dispose();
        }
    }

    public Task<BrowseNode[]> BrowseAsync(string nodeId, CancellationToken ct = default)
        => Client.BrowseAsync(nodeId, includeDataType: false, ct);

    public Task<ReadResult> ReadValueAsync(string nodeId, CancellationToken ct = default)
        => Client.ReadValueAsync(nodeId, ct);

    public Task<NodeAttributes> ReadAttributesAsync(string nodeId, CancellationToken ct = default)
        => Client.ReadNodeAttributesAsync(nodeId, ct);

    public Task<WriteResult> WriteValueAsync(WriteValue value, CancellationToken ct = default)
        => Client.WriteValueAsync(value, ct);

    /// <summary>
    /// Starts monitoring a node. <paramref name="context"/> comes back on every <see cref="DataValue"/> for it
    /// (as <c>((MonitoredNode)dataValue.Context).Context</c>), so updates can be routed without a lookup.
    /// </summary>
    /// <returns>The monitored node, or null if the node id couldn't be parsed.</returns>
    public async Task<MonitoredNode?> MonitorAsync(string nodeId, string displayName, object context, CancellationToken ct = default)
    {
        var subscription = Subscription;
        await _subscriptionLock.WaitAsync(ct);
        try
        {
            return await subscription.AddMonitoredItemAsync(new MonitoredNodeDescriptor(Guid.CreateVersion7(), nodeId, displayName, context), ct);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async Task StopMonitoringAsync(Guid monitoredNodeId, CancellationToken ct = default)
    {
        var subscription = Subscription;
        await _subscriptionLock.WaitAsync(ct);
        try
        {
            await subscription.RemoveMonitoredItemAsync(monitoredNodeId, ct);
        }
        finally
        {
            _subscriptionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _subscriptionLock.Dispose();
    }

    private IClient Client => _client ?? throw new InvalidOperationException("Not connected to a server.");

    private ISubscription Subscription => _subscription ?? throw new InvalidOperationException("Not connected to a server.");
}

public sealed class ConnectionFailedException(string message) : Exception(message);

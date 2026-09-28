using System.Collections.ObjectModel;
using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Browser.Services;

namespace Scaidome.OpcUa.Browser.ViewModels;

public sealed class ConnectViewModel : OverlayViewModel
{
    private readonly OpcUaConnection _connection;
    private readonly RecentConnectionsStore _recentConnections;
    private readonly Func<Task> _onConnected;

    private string _serverUrl = OpcUaConnection.DefaultServerUrl;
    private bool _useSecurity;
    private bool _autoAcceptServerCertificate = true;
    private string _userName = "";
    private bool _isBusy;
    private string? _errorMessage;
    private CancellationTokenSource? _connectCts;

    public ConnectViewModel(OpcUaConnection connection, RecentConnectionsStore recentConnections, Func<Task> onConnected)
    {
        _connection = connection;
        _recentConnections = recentConnections;
        _onConnected = onConnected;

        RecentConnections = new ObservableCollection<RecentConnection>(recentConnections.Load());

        OpenCommand = new RelayCommand(Open);
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(ServerUrl));
        SelectRecentCommand = RelayCommand.Create<RecentConnection>(SelectRecent);
    }

    public ObservableCollection<RecentConnection> RecentConnections { get; }

    public RelayCommand OpenCommand { get; }

    public AsyncRelayCommand ConnectCommand { get; }

    public RelayCommand SelectRecentCommand { get; }

    public string ServerUrl
    {
        get => _serverUrl;
        set => SetProperty(ref _serverUrl, value);
    }

    public bool UseSecurity
    {
        get => _useSecurity;
        set => SetProperty(ref _useSecurity, value);
    }

    public bool AutoAcceptServerCertificate
    {
        get => _autoAcceptServerCertificate;
        set => SetProperty(ref _autoAcceptServerCertificate, value);
    }

    /// <summary>Empty means anonymous.</summary>
    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    /// <summary>Set from the view's PasswordBox, which can't be data bound.</summary>
    public string Password { get; set; } = "";

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public override void Close()
    {
        _connectCts?.Cancel();
        base.Close();
    }

    private void Open()
    {
        ErrorMessage = null;
        IsOpen = true;
    }

    private void SelectRecent(RecentConnection entry)
    {
        ServerUrl = entry.ServerUrl;
        UseSecurity = entry.UseSecurity;
        UserName = entry.UserName ?? "";
    }

    private async Task ConnectAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        using var cts = new CancellationTokenSource();
        _connectCts = cts;
        try
        {
            var serverUrl = ServerUrl.Trim();
            var userName = string.IsNullOrWhiteSpace(UserName) ? null : UserName.Trim();

            await _connection.ConnectAsync(new ConnectionSettings(serverUrl, UseSecurity, AutoAcceptServerCertificate, userName, Password), cts.Token);

            RecentConnections.Clear();
            foreach (var entry in _recentConnections.Add(new RecentConnection(serverUrl, UseSecurity, userName, DateTime.Now)))
                RecentConnections.Add(entry);

            IsOpen = false;
            await _onConnected();
        }
        catch (OperationCanceledException)
        {
            // Cancelled by closing the dialog.
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _connectCts = null;
            IsBusy = false;
        }
    }
}

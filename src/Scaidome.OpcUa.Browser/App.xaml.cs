using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Scaidome.OpcUa.Browser.Services;
using Scaidome.OpcUa.Browser.ViewModels;
using Scaidome.OpcUa.Browser.Views;

namespace Scaidome.OpcUa.Browser;

public partial class App : Application
{
    private ILoggerFactory? _loggerFactory;
    private OpcUaConnection? _connection;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Composition root — small enough that a DI container adds nothing.
        var logCapture = new ClientLogCapture();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logCapture).SetMinimumLevel(LogLevel.Information));
        _connection = new OpcUaConnection(_loggerFactory, logCapture);

        var mainWindow = new MainWindow { DataContext = new MainViewModel(_connection, new RecentConnectionsStore()) };
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Close the session so the server can release it right away. Run off the UI thread (nothing to marshal back to
        // while exiting) and don't let a hanging server keep the process alive.
        if (_connection is { } connection)
            Task.Run(async () => await connection.DisposeAsync()).Wait(TimeSpan.FromSeconds(3));

        _loggerFactory?.Dispose();
        base.OnExit(e);
    }

    // Commands let exceptions propagate (see AsyncRelayCommand); report them instead of crashing.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(MainWindow, e.Exception.Message, "Scaidome Browser", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}

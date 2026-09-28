using Microsoft.Extensions.Logging;

namespace Scaidome.OpcUa.Browser.Services;

/// <summary>
/// Logger provider for the OPC UA client. The client logs failures instead of throwing them (e.g. ConnectAsync just returns false),
/// so this remembers the most recent error to give the user a real reason. Everything is also written to the debugger output.
/// </summary>
public sealed class ClientLogCapture : ILoggerProvider
{
    private volatile string? _lastError;

    public string? LastError => _lastError;

    public void ClearLastError() => _lastError = null;

    public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);

    public void Dispose()
    {
    }

    private sealed class CaptureLogger(ClientLogCapture owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            System.Diagnostics.Debug.WriteLine($"[{logLevel}] {category}: {message}{(exception is null ? "" : " - " + exception.Message)}");

            if (logLevel >= LogLevel.Error)
                owner._lastError = exception?.Message ?? message;
        }
    }
}

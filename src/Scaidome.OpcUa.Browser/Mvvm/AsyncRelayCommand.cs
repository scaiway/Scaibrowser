using System.Windows.Input;

namespace Scaidome.OpcUa.Browser.Mvvm;

/// <summary>
/// Asynchronous command that disables itself while running, so a double click can't start the same operation twice.
/// Exceptions are not swallowed: they surface on the dispatcher and are reported by <c>App.DispatcherUnhandledException</c>.
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    private AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public static AsyncRelayCommand Create<T>(Func<T, Task> execute, Func<T, bool>? canExecute = null)
        => new(p => p is T t ? execute(t) : Task.CompletedTask, p => p is T t && (canExecute?.Invoke(t) ?? true));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool IsRunning => _isRunning;

    public bool CanExecute(object? parameter) => !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _isRunning = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isRunning = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

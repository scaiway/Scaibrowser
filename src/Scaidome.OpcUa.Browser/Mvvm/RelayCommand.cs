using System.Windows.Input;

namespace Scaidome.OpcUa.Browser.Mvvm;

/// <summary>Synchronous command. Use <see cref="Create{T}"/> for a command that takes a typed parameter.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    private RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public static RelayCommand Create<T>(Action<T> execute, Func<T, bool>? canExecute = null)
        => new(p => { if (p is T t) execute(t); }, p => p is T t && (canExecute?.Invoke(t) ?? true));

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
            _execute(parameter);
    }
}

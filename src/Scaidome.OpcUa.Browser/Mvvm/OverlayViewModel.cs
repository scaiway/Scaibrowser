namespace Scaidome.OpcUa.Browser.Mvvm;

/// <summary>Base for the in-window overlay dialogs: they are shown and hidden by toggling <see cref="IsOpen"/>.</summary>
public abstract class OverlayViewModel : ObservableObject
{
    private bool _isOpen;

    protected OverlayViewModel()
    {
        CloseCommand = new RelayCommand(Close);
    }

    public bool IsOpen
    {
        get => _isOpen;
        protected set => SetProperty(ref _isOpen, value);
    }

    public RelayCommand CloseCommand { get; }

    public virtual void Close() => IsOpen = false;
}

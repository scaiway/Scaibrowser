using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Browser.Services;
using Scaidome.OpcUa.Client;

namespace Scaidome.OpcUa.Browser.ViewModels;

public enum StatusSeverity
{
    Good,
    Uncertain,
    Bad,
}

/// <summary>A row in the monitored items grid.</summary>
public sealed class MonitoredItemViewModel : ObservableObject
{
    private object? _value;
    private uint _statusCode;
    private string? _error;
    private DateTime? _sourceTimestamp;
    private DateTime? _serverTimestamp;

    public MonitoredItemViewModel(string nodeId, string displayName)
    {
        NodeId = nodeId;
        DisplayName = displayName;
    }

    public string NodeId { get; }

    public string DisplayName { get; }

    /// <summary>Id of the item in the client's subscription; set once monitoring has started.</summary>
    public Guid? MonitoredNodeId { get; set; }

    public object? Value
    {
        get => _value;
        private set
        {
            if (SetProperty(ref _value, value))
            {
                OnPropertyChanged(nameof(ValueText));
                OnPropertyChanged(nameof(DataTypeName));
            }
        }
    }

    public string ValueText => ValueFormatter.Format(Value);

    public string DataTypeName => ValueFormatter.TypeName(Value);

    public uint StatusCode
    {
        get => _statusCode;
        private set
        {
            if (SetProperty(ref _statusCode, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(Severity));
            }
        }
    }

    public StatusSeverity Severity => (StatusCode & 0xC0000000) switch
    {
        0 => StatusSeverity.Good,
        0x40000000 => StatusSeverity.Uncertain,
        _ => StatusSeverity.Bad,
    };

    public string StatusText => StatusCodes.GetName(StatusCode);

    /// <summary>Error text from the last read, shown as the status tooltip.</summary>
    public string? Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    public DateTime? SourceTimestamp
    {
        get => _sourceTimestamp;
        private set => SetProperty(ref _sourceTimestamp, value);
    }

    public DateTime? ServerTimestamp
    {
        get => _serverTimestamp;
        private set => SetProperty(ref _serverTimestamp, value);
    }

    public void Apply(DataValue dataValue)
    {
        Value = dataValue.Value;
        StatusCode = dataValue.StatusCode;
        SourceTimestamp = dataValue.SourceTimestamp;
        ServerTimestamp = dataValue.ServerTimestamp;
        Error = null;
    }

    public void Apply(ReadResult result)
    {
        Value = result.Value;
        StatusCode = result.StatusCode;
        SourceTimestamp = result.SourceTimestamp == DateTime.MinValue ? null : result.SourceTimestamp;
        ServerTimestamp = result.ServerTimestamp == DateTime.MinValue ? null : result.ServerTimestamp;
        Error = result.Error;
    }
}

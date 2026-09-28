using Scaidome.OpcUa.Browser.Mvvm;
using Scaidome.OpcUa.Browser.Services;
using Scaidome.OpcUa.Client;

namespace Scaidome.OpcUa.Browser.ViewModels;

public sealed class WriteValueViewModel : OverlayViewModel
{
    private const int ValueRankScalar = -1;
    private const int ValueRankScalarOrOneDimension = -3;
    private const int ValueRankAny = -2;

    private readonly OpcUaConnection _connection;

    private MonitoredItemViewModel? _item;
    private NodeAttributes? _attributes;
    private string _newValue = "";
    private string? _errorMessage;
    private string? _readOnlyReason;

    public WriteValueViewModel(OpcUaConnection connection)
    {
        _connection = connection;
        WriteCommand = new AsyncRelayCommand(WriteAsync, () => Item != null && Attributes != null && ReadOnlyReason == null);
    }

    public AsyncRelayCommand WriteCommand { get; }

    public MonitoredItemViewModel? Item
    {
        get => _item;
        private set => SetProperty(ref _item, value);
    }

    public NodeAttributes? Attributes
    {
        get => _attributes;
        private set
        {
            if (SetProperty(ref _attributes, value))
            {
                OnPropertyChanged(nameof(DataTypeName));
                OnPropertyChanged(nameof(CurrentValueText));
            }
        }
    }

    public string DataTypeName => Attributes is null ? "" : BuiltInDataTypes.GetName(Attributes.DataType);

    public string CurrentValueText => ValueFormatter.Format(Attributes?.Value);

    public string NewValue
    {
        get => _newValue;
        set => SetProperty(ref _newValue, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>Why the node can't be written from here, or null if it can.</summary>
    public string? ReadOnlyReason
    {
        get => _readOnlyReason;
        private set => SetProperty(ref _readOnlyReason, value);
    }

    public async Task OpenAsync(MonitoredItemViewModel item)
    {
        Item = item;
        Attributes = null;
        NewValue = "";
        ErrorMessage = null;
        ReadOnlyReason = null;
        IsOpen = true;

        var attributes = await _connection.ReadAttributesAsync(item.NodeId);
        if (Item != item)
            return; // closed or reopened for another item meanwhile

        Attributes = attributes;
        NewValue = ValueFormatter.Format(attributes.Value);

        if (attributes.Error != null)
            ReadOnlyReason = attributes.Error;
        else if (!attributes.CanWrite)
            ReadOnlyReason = "The node is not writable for the current user.";
        else if (attributes.ValueRank is not (ValueRankScalar or ValueRankScalarOrOneDimension or ValueRankAny) || attributes.Value is Array and not byte[])
            ReadOnlyReason = "Writing arrays is not supported.";
    }

    public override void Close()
    {
        base.Close();
        Item = null;
        Attributes = null;
    }

    private async Task WriteAsync()
    {
        if (Item is not { } item || Attributes is not { } attributes)
            return;

        if (!BuiltInDataTypes.TryParse(attributes.DataType, NewValue, out var value, out var parseError))
        {
            ErrorMessage = parseError;
            return;
        }

        ErrorMessage = null;
        var targetType = attributes.DataType.StartsWith("i=", StringComparison.Ordinal) || attributes.DataType.StartsWith("ns=", StringComparison.Ordinal)
            ? attributes.DataType
            : null;

        var result = await _connection.WriteValueAsync(new WriteValue(item.NodeId, value, targetType));

        if (StatusCodes.IsGood(result.StatusCode))
            Close();
        else
            ErrorMessage = $"Write failed: {result.Error ?? StatusCodes.GetName(result.StatusCode)}";
    }
}

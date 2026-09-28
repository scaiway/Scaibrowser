using System.Globalization;

namespace Scaidome.OpcUa.Browser.Services;

/// <summary>
/// Maps the OPC UA built-in data type node ids (as returned in <c>NodeAttributes.DataType</c>, e.g. "i=6") to a readable
/// name and a parser for text typed by the user. Parsing is culture invariant, so "1.5" means the same on every machine.
/// </summary>
public static class BuiltInDataTypes
{
    private sealed record DataType(string Name, Func<string, object>? Parse);

    private static readonly Dictionary<string, DataType> Types = new()
    {
        ["i=1"] = new("Boolean", s => bool.Parse(s)),
        ["i=2"] = new("SByte", s => sbyte.Parse(s, CultureInfo.InvariantCulture)),
        ["i=3"] = new("Byte", s => byte.Parse(s, CultureInfo.InvariantCulture)),
        ["i=4"] = new("Int16", s => short.Parse(s, CultureInfo.InvariantCulture)),
        ["i=5"] = new("UInt16", s => ushort.Parse(s, CultureInfo.InvariantCulture)),
        ["i=6"] = new("Int32", s => int.Parse(s, CultureInfo.InvariantCulture)),
        ["i=7"] = new("UInt32", s => uint.Parse(s, CultureInfo.InvariantCulture)),
        ["i=8"] = new("Int64", s => long.Parse(s, CultureInfo.InvariantCulture)),
        ["i=9"] = new("UInt64", s => ulong.Parse(s, CultureInfo.InvariantCulture)),
        ["i=10"] = new("Float", s => float.Parse(s, CultureInfo.InvariantCulture)),
        ["i=11"] = new("Double", s => double.Parse(s, CultureInfo.InvariantCulture)),
        ["i=12"] = new("String", s => s),
        ["i=13"] = new("DateTime", ParseDateTime),
        ["i=14"] = new("Guid", s => Guid.Parse(s)),
        ["i=15"] = new("ByteString", null),
        ["i=17"] = new("NodeId", null),
        ["i=21"] = new("LocalizedText", null),
        ["i=24"] = new("BaseDataType", null),
        ["i=26"] = new("Number", null),
        ["i=27"] = new("Integer", null),
        ["i=28"] = new("UInteger", null),
        ["i=29"] = new("Enumeration", null),
        ["i=290"] = new("Duration", s => double.Parse(s, CultureInfo.InvariantCulture)),
        ["i=294"] = new("UtcTime", ParseDateTime),
    };

    public static string GetName(string dataTypeNodeId)
        => Types.TryGetValue(dataTypeNodeId, out var type) ? type.Name : dataTypeNodeId;

    /// <summary>
    /// Parses <paramref name="text"/> for the given data type. Types without a parser here (enumerations, custom types, ...)
    /// return the text unchanged and leave conversion to the client, which converts against the target type node id.
    /// </summary>
    public static bool TryParse(string dataTypeNodeId, string text, out object value, out string? error)
    {
        value = text;
        error = null;

        if (!Types.TryGetValue(dataTypeNodeId, out var type) || type.Parse is null)
            return true;

        try
        {
            value = type.Parse(text.Trim());
            return true;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            error = $"'{text}' is not a valid {type.Name}. {ex.Message}";
            return false;
        }
    }

    // Typed-in times are local; OPC UA carries UTC.
    private static object ParseDateTime(string s)
        => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal);
}

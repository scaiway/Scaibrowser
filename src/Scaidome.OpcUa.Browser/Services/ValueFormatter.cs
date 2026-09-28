using System.Collections;
using System.Globalization;

namespace Scaidome.OpcUa.Browser.Services;

/// <summary>Turns OPC UA values into display text — arrays would otherwise show as "System.Int32[]".</summary>
public static class ValueFormatter
{
    private const int MaxArrayItems = 20;
    private const int MaxBytes = 64;

    public static string Format(object? value) => value switch
    {
        null => "",
        string s => s,
        DateTime dt => dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        byte[] bytes => Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, MaxBytes)) + (bytes.Length > MaxBytes ? "…" : ""),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        IEnumerable items => FormatItems(items),
        _ => value.ToString() ?? "",
    };

    public static string TypeName(object? value) => value switch
    {
        null => "",
        byte[] => "ByteString",
        Array array => $"{array.GetType().GetElementType()?.Name}[{array.Length}]",
        _ => value.GetType().Name,
    };

    private static string FormatItems(IEnumerable items)
    {
        var parts = items.Cast<object?>().Take(MaxArrayItems + 1).Select(Format).ToList();
        var truncated = parts.Count > MaxArrayItems;
        if (truncated)
            parts.RemoveAt(MaxArrayItems);

        return "[" + string.Join(", ", parts) + (truncated ? ", …" : "") + "]";
    }
}

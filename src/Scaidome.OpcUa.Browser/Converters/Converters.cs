using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Scaidome.OpcUa.Browser.ViewModels;

namespace Scaidome.OpcUa.Browser.Converters;

/// <summary>true → Visible. Pass ConverterParameter=Invert to flip.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value is true;
        if (parameter as string == "Invert")
            visible = !visible;

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Collapsed for null, empty strings, zero counts and empty collections; Visible otherwise. ConverterParameter=Invert flips it.</summary>
public sealed class HasValueToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasValue = value switch
        {
            null => false,
            string s => s.Length > 0,
            int count => count > 0,
            ICollection c => c.Count > 0,
            _ => true,
        };
        if (parameter as string == "Invert")
            hasValue = !hasValue;

        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class ConnectionStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ConnectionStatus.Connected => ThemeBrush("SuccessBrush"),
        ConnectionStatus.Reconnecting => ThemeBrush("WarningBrush"),
        _ => ThemeBrush("TextSecondaryBrush"),
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    internal static Brush ThemeBrush(string key) => (Brush)Application.Current.FindResource(key);
}

public sealed class StatusSeverityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        StatusSeverity.Good => ConnectionStatusToBrushConverter.ThemeBrush("SuccessBrush"),
        StatusSeverity.Uncertain => ConnectionStatusToBrushConverter.ThemeBrush("WarningBrush"),
        _ => ConnectionStatusToBrushConverter.ThemeBrush("ErrorBrush"),
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>OPC UA timestamps are UTC; shows them as local time of day.</summary>
public sealed class TimestampConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime dt ? dt.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

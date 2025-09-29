using System;
using System.Collections.Generic;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

// Compares two values for equality (case-insensitive for strings)
public sealed class EqualityToBoolConverter : IValueConverter, IMultiValueConverter
{
    public static readonly EqualityToBoolConverter Instance = new();

    public bool Invert { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        var areEqual = false;
        if (value is null && parameter is null) areEqual = true;

        if (value is string s1 && parameter is string s2)
            areEqual = string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        else
            areEqual = Equals(value, parameter);
        
        return Invert ? !areEqual : areEqual;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        => Avalonia.AvaloniaProperty.UnsetValue;
        
    public object Convert(IList<object?> values, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (values.Count < 2)
            return Invert ? true : false;

        var value1 = values[0];
        var value2 = values[1];

        var areEqual = false;
        if (value1 is null && value2 is null) areEqual = true;
        else if (value1 is null || value2 is null) areEqual = false;
        else if (value1 is string s1 && value2 is string s2)
            areEqual = string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        else
            areEqual = Equals(value1, value2);
        
        return Invert ? !areEqual : areEqual;
    }
}
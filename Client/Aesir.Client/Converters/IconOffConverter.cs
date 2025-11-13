using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Aesir.Client.Converters;

public class IconOffConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MaterialIconKind iconKind)
        {
            var offIconName = iconKind.ToString() + "Off";
            if (Enum.TryParse<MaterialIconKind>(offIconName, out var offIcon))
            {
                return offIcon;
            }
        }
        return MaterialIconKind.Help; // Fallback icon
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
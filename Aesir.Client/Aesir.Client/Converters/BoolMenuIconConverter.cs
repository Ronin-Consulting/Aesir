using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Aesir.Client.Converters;

public class BoolMenuIconConverter : IValueConverter
{
    public static readonly BoolMenuIconConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOpen)
        {
            return isOpen ? MaterialIconKind.MenuOpen : MaterialIconKind.MenuClose;
        }
        return MaterialIconKind.MenuClose;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
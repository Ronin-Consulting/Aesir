using System;
using System.Collections.Generic;
using System.Globalization;
using Aesir.Client.ViewModels;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

public class ToolRequestParameterConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        return new ToolRequestWithIcon()
        {
            ToolName = values[0]?.ToString() ?? string.Empty,
            McpServerName = values.Count > 1 ? values[1]?.ToString() : null
        };
    }

    public IList<object> ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
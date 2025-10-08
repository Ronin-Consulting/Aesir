using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aesir.Common.Models;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

public class ToolRequestExistsConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 2 && values?.Count != 3)
            return BindingOperations.DoNothing;

        var requests = values[0] as IEnumerable<ToolRequest>;
        var toolName = values[1] as string;

        if (requests == null || string.IsNullOrEmpty(toolName))
            return false;

        string? mcpServerName = null;
        if (values.Count == 3)
            mcpServerName = values[2] as string;

        return requests.Any(r => r.ToolName == toolName && r.McpServerName == mcpServerName);
    }
}
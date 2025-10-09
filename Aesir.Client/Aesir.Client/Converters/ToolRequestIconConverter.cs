using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

/// <summary>
/// Converts a ToolRequestWithIcon to the appropriate icon string based on selection state
/// </summary>
public class ToolRequestIconConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || 
            values[0] is not ToolRequestWithIcon toolRequestWithIcon ||
            values[1] is not ICollection<ToolRequest> selectedToolRequests)
        {
            return "Help"; // Default icon
        }

        try
        {
            // Check if this tool is currently selected/enabled
            var isSelected = selectedToolRequests.Any(t => 
                string.Equals(t.ToolName, toolRequestWithIcon.ToolName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.McpServerName, toolRequestWithIcon.McpServerName, StringComparison.OrdinalIgnoreCase));

            // Return appropriate icon based on selection state
            return isSelected ? toolRequestWithIcon.IconName : toolRequestWithIcon.IconName + "Off";
        }
        catch (Exception)
        {
            return "Help"; // Default icon on error
        }
    }

    public object?[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
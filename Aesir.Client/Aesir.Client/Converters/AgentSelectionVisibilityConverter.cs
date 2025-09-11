using System;
using System.Collections.Generic;
using System.Globalization;
using Aesir.Common.Models;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

public class AgentSelectionVisibilityConverter : IMultiValueConverter
{
    public static readonly AgentSelectionVisibilityConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 && 
            values[0] is AesirAgentBase currentAgent && 
            values[1] is AesirAgentBase selectedAgent)
        {
            return ReferenceEquals(currentAgent, selectedAgent);
        }
            
        return false;
    }
}
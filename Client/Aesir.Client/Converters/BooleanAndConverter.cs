using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters
{
    public class BooleanAndConverter : IMultiValueConverter
    {
        public static readonly BooleanAndConverter Instance = new();

        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool) && targetType != typeof(bool?))
                return false;

            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                    return false;
                if (value is not bool)
                    return false;
            }
            return true;
        }
    }
}
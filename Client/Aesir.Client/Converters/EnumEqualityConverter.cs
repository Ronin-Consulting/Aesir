using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

/// <summary>
/// A value converter that compares an enum value to a specified parameter for equality and returns a boolean result.
/// </summary>
/// <remarks>
/// This converter is intended for scenarios where a value needs to be compared to a specified parameter,
/// such as in data binding for UI elements. The comparison is performed using the <see cref="object.Equals"/> method.
/// </remarks>
/// <example>
/// The Convert method checks if the provided value and parameter are equal and returns a boolean result.
/// The ConvertBack method is not implemented and will throw a <see cref="NotImplementedException"/>.
/// </example>
/// <exception cref="NotImplementedException">
/// Thrown when the ConvertBack method is invoked, as it is not implemented in this converter.
/// </exception>
public class EnumEqualityConverter : IValueConverter
{
    /// <summary>
    /// Compares the provided value with the specified parameter to determine equality.
    /// </summary>
    /// <param name="value">The value to be compared.</param>
    /// <param name="targetType">The type of the target property. This parameter is not used in this implementation.</param>
    /// <param name="parameter">The value to compare against the provided value.</param>
    /// <param name="culture">The culture to use in the converter. This parameter is not used in this implementation.</param>
    /// <returns>Returns true if the value is equal to the parameter; otherwise, false.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;
            
        return value.Equals(parameter);
    }

    /// Converts a value back to its source type. This method is not implemented in this class and will throw a NotImplementedException if called.
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="culture">The culture to use in the converter.</param>
    /// <returns>Throws NotImplementedException. No value is returned from this method.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

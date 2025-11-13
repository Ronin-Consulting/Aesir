using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Aesir.Client.Converters;

/// <summary>
/// A value converter that converts a boolean value to a corresponding
/// <see cref="MaterialIconKind"/> representing a menu's open or close state.
/// </summary>
/// <remarks>
/// This converter is typically used to change UI icons based on the state of a menu.
/// When the boolean value is true, the converter returns <see cref="MaterialIconKind.MenuOpen"/>;
/// otherwise, it returns <see cref="MaterialIconKind.MenuClose"/>.
/// </remarks>
public class BoolMenuIconConverter : IValueConverter
{
    /// <summary>
    /// Provides a singleton instance of the <see cref="BoolMenuIconConverter"/> class.
    /// This instance can be used as a value converter to transform a boolean value into
    /// a corresponding <see cref="MaterialIconKind"/> value for menu icons.
    /// </summary>
    public static readonly BoolMenuIconConverter Instance = new();

    /// <summary>
    /// Converts a boolean value to a corresponding Material icon indicating menu state.
    /// </summary>
    /// <param name="value">The boolean value to be converted, representing the menu state.</param>
    /// <param name="targetType">The target type of the conversion. Not used in this implementation.</param>
    /// <param name="parameter">An optional parameter. Not used in this implementation.</param>
    /// <param name="culture">The culture information used in the conversion. Not used in this implementation.</param>
    /// <returns>
    /// A <see cref="MaterialIconKind"/> representing MenuOpen if the boolean value is true,
    /// otherwise MenuClose. If the value is not a boolean, defaults to MenuClose.
    /// </returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOpen)
        {
            return isOpen ? MaterialIconKind.MenuOpen : MaterialIconKind.MenuClose;
        }
        return MaterialIconKind.MenuClose;
    }

    /// <summary>
    /// Converts back a value. This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">The value that is being converted back.</param>
    /// <param name="targetType">The type to which the value is being converted back.</param>
    /// <param name="parameter">Optional parameter for the conversion operation.</param>
    /// <param name="culture">The culture used during the conversion process.</param>
    /// <returns>Throws a <see cref="NotImplementedException"/>.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
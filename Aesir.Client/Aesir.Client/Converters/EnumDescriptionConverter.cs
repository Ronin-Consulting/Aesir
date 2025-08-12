using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

/// <summary>
/// Converts Enum values to their corresponding descriptions using the
/// <see cref="DescriptionAttribute"/>, or their string representation if
public class EnumDescriptionConverter : IValueConverter
{
    /// Converts an enumeration value to its corresponding description attribute or string representation.
    /// <param name="value">The enumeration value to be converted. If it is not an enumeration, its string representation is returned.</param>
    /// <param name="t">The target type for the conversion. This parameter is not used within the method but required by the interface.</param>
    /// <param name="p">An optional parameter used by the binding system. Not used by this method.</param>
    /// <param name="c">The culture information used during conversion. Not used by this method.</param>
    /// <returns>The description attribute of the enumeration if defined; otherwise, its string representation.</returns>
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is Enum e ? e.GetDescription() : value?.ToString();

    /// Converts a value back to its original type. This method is primarily used in two-way data binding scenarios where
    /// a target value needs to be converted back to the source type.
    /// <param name="value">The value that needs to be converted back. It can be null.</param>
    /// <param name="t">The target type to which the value will be converted.</param>
    /// <param name="p">An optional parameter for the conversion logic, if needed.</param>
    /// <param name="c">The culture information to respect during the conversion process.</param>
    /// <return>Returns the original value without any conversion. The default implementation assumes no transformation during the conversion back.</return>
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) 
        => value;
}

/// <summary>
/// Provides extension methods for <see cref="System.Enum"/> types to retrieve additional metadata or functionality.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Retrieves the description of an enumeration value.
    /// If the enum value has a <see cref="DescriptionAttribute"/>, its description is returned.
    /// Otherwise, the enum value's name is returned as a string.
    /// </summary>
    /// <param name="e">The enum value for which the description is to be retrieved.</param>
    /// <returns>A string representing the description of the enumeration value if a <see cref="DescriptionAttribute"/>
    /// is present; otherwise, the name of the enum value as a string.</returns>
    public static string GetDescription(this Enum e)
    {
        var fieldInfo = e.GetType().GetField(e.ToString())!;
        
        return (fieldInfo.GetCustomAttribute<DescriptionAttribute>()?.Description) ?? e.ToString();
    }
}
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is Enum e ? e.GetDescription() : value?.ToString();

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) 
        => value;
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum e)
    {
        var fieldInfo = e.GetType().GetField(e.ToString())!;
        
        return (fieldInfo.GetCustomAttribute<DescriptionAttribute>()?.Description) ?? e.ToString();
    }
}
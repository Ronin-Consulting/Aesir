using System.ComponentModel;
using System.Reflection;

namespace Aesir.Common.Extensions;

public static class EnumExtensions
{
    // Convert enum → description string
    public static string GetDescription<T>(this T value) where T : Enum
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    // Parse description string → enum
    public static T FromDescription<T>(string description) where T : Enum
    {
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if ((attribute != null && attribute.Description == description) || field.Name == description)
            {
                return (T)field.GetValue(null)!;
            }
        }
        throw new ArgumentException($"No {typeof(T).Name} with description '{description}' found.");
    }
}
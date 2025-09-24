using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a "think" value that can either be a boolean or a string indication, such as "high", "medium", "low" (e.g., gpt-oss).
/// </summary>
[JsonConverter(typeof(ThinkValueConverter))]
public readonly struct ThinkValue : IEquatable<ThinkValue>
{
    /// <summary>
    /// Represents the internal value for the think state, which can be a boolean or a string indicating levels such as "high", "medium", or "low".
    /// </summary>
    private readonly string? _value;

    /// <summary>
    /// Represents a high level for the think value.
    /// </summary>
    public const string High = "high";

    /// <summary>
    /// Represents a medium level for the think value.
    /// </summary>
    public const string Medium = "medium";

    /// <summary>
    /// Represents a low level for the think value.
    /// </summary>
    public const string Low = "low";
	
    /// <summary>
    /// Represents a struct that encapsulates a value for "think" that can
    /// either be a boolean (`true`/`false`) or a string like (`"high"`, `"medium"`, `"low"`) used for gpt-oss models.
    /// </summary>
    /// <remarks>
    /// This type provides implicit and explicit conversion operators between
    /// `bool`, `bool?`, and `string`, enabling flexible usage for
    /// boolean logic or predefined string-based levels.
    /// </remarks>
    /// <example>
    /// Example conversion scenarios include initializing this value from
    /// a boolean, nullable boolean, or string, or interpreting its string representation.
    /// </example>
    /// <threadsafety>
    /// This struct is immutable and thread-safe due to its readonly and value type nature.
    /// </threadsafety>
    public ThinkValue(string? value)
    {
        _value = value;
    }

    /// <summary>
    /// Represents a struct that encapsulates a value for "think" which can be either
    /// a boolean or a predefined string value indicating levels such as "high", "medium", or "low".
    /// </summary>
    /// <remarks>
    /// This type provides implicit and explicit conversion operators to enable flexible
    /// usage with nullable booleans, booleans, and strings. It supports equality comparisons
    /// and ensures case-insensitive comparisons for string values.
    /// </remarks>
    /// <threadsafety>
    /// This struct is immutable and thread-safe due to its readonly and value-type nature.
    /// </threadsafety>
    public ThinkValue(bool? value)
    {
        _value = value?.ToString().ToLower();
    }

    /// <summary>
    /// Represents a struct that encapsulates a "think" value, which can be a boolean
    /// (e.g., `true` or `false`) or a string representation of levels, such as
    /// `"high"`, `"medium"`, or `"low"`, commonly used in scenarios like gpt-oss models.
    /// </summary>
    /// <remarks>
    /// This type provides flexibility in the representation of values by supporting
    /// implicit and explicit conversions between `string`, `bool`, and `bool?`. The design
    /// facilitates usage in logic requiring predefined string levels or boolean states.
    /// </remarks>
    /// <threadsafety>
    /// This struct is immutable and thread-safe due to its readonly nature and value-type
    /// implementation.
    /// </threadsafety>
    public ThinkValue(object? value)
    {
        if (value is null)
        {
            _value = null;
            return;
        }
		
        _value = 
            bool.TryParse(value!.ToString(), out var result) 
                ? result.ToString().ToLower() : value.ToString();
    }

    /// <summary>
    /// Defines an implicit conversion operator to allow a string to
    /// be implicitly converted into a ThinkValue structure.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A new instance of <see cref="ThinkValue"/> initialized with the provided string.</returns>
    public static implicit operator ThinkValue(string? value) => new(value);

    /// <summary>
    /// Explicitly converts a ThinkValue instance to a nullable string.
    /// </summary>
    /// <param name="value">The ThinkValue instance to convert.</param>
    /// <returns>The string representation of the ThinkValue instance if defined; otherwise, null.</returns>
    public static explicit operator string?(ThinkValue value) => value._value;

    /// <summary>
    /// Defines an implicit operator for converting a nullable boolean value
    /// to an instance of the ThinkValue struct. This allows a nullable boolean
    /// to be directly assigned to a ThinkValue without explicit conversion.
    /// </summary>
    /// <param name="value">The nullable boolean value to be converted.</param>
    /// <returns>A ThinkValue instance representing the provided boolean value.</returns>
    public static implicit operator ThinkValue(bool? value) => new(value);

    /// <summary>
    /// Allows implicit conversion from a boolean value to a ThinkValue.
    /// </summary>
    /// <param name="value">The boolean value to be converted.</param>
    /// <returns>A ThinkValue instance representing the given boolean value.</returns>
    public static implicit operator ThinkValue(bool value) => new(value);

    /// <summary>
    /// Defines a user-defined conversion from a ThinkValue to a nullable boolean.
    /// This operator attempts to convert the internal value of the ThinkValue instance
    /// to a nullable boolean. If the internal value is null, the result will be null.
    /// If the value can be parsed as a boolean, it will return the parsed boolean value.
    /// If it cannot be parsed, it returns false, ensuring safe handling of the conversion.
    /// </summary>
    /// <param name="value">The ThinkValue instance to convert.</param>
    /// <returns>A nullable boolean representation of the ThinkValue.</returns>
    public static implicit operator bool?(ThinkValue value)
    {
        if (value._value == null) return null;
		
        return 
            bool.TryParse(value._value, out var result) ? result : 
                !string.IsNullOrWhiteSpace(value._value)		
            ;
    }

    /// <summary>
    /// Determines whether the current ThinkValue represents a boolean value.
    /// </summary>
    /// <returns>True if the value represents a boolean (true/false), otherwise false.</returns>
    public bool IsBoolean() => 
        _value != null && bool.TryParse(_value, out _);
	
    /// <summary>
    /// Converts the current <see cref="ThinkValue"/> instance to a nullable boolean value.
    /// </summary>
    /// <returns>
    /// A nullable boolean value representing the "think" state. Returns <c>true</c>, <c>false</c>, or <c>null</c> depending on the encapsulated value.
    /// </returns>
    /// <remarks>
    /// The conversion is based on the internal representation of the value within the <see cref="ThinkValue"/> instance.
    /// If the underlying value is a string that can be interpreted as a boolean, it will be converted. Otherwise, <c>null</c> may be returned.
    /// </remarks>
    public bool? ToBoolean() =>this;
	
    /// <summary>
    /// Returns the string representation of the value.
    /// </summary>
    /// <returns>The string value.</returns>
    public override string? ToString() => _value;
	
    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="ThinkValue"/> instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the specified object is equal to the current instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj) => 
        obj is ThinkValue other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="ThinkValue"/> instance is equal to the current instance.
    /// </summary>
    /// <param name="other">The <see cref="ThinkValue"/> instance to compare with the current instance.</param>
    /// <returns>
    /// true if the specified <see cref="ThinkValue"/> is equal to the current instance; otherwise, false.
    /// </returns>
    public bool Equals(ThinkValue other) => 
        string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Serves as the default hash function.
    /// Computes a hash code for the current instance based on its value.
    /// </summary>
    /// <returns>An integer hash code that is case-insensitive and consistent with the string value of the instance.</returns>
    public override int GetHashCode() => 
        _value != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_value) : 0;

    /// <summary>
    /// Defines an equality operator for the ThinkValue structure, allowing comparison of two ThinkValue instances
    /// to determine if they are equal.
    /// </summary>
    /// <param name="left">The first ThinkValue instance to compare.</param>
    /// <param name="right">The second ThinkValue instance to compare.</param>
    /// <returns>True if both ThinkValue instances are equal; otherwise, false.</returns>
    public static bool operator ==(ThinkValue left, ThinkValue right) => 
        left.Equals(right);

    /// <summary>
    /// Implements an inequality operator for the ThinkValue struct.
    /// Returns true if the two ThinkValue instances are not equal, otherwise false.
    /// </summary>
    /// <param name="left">The first ThinkValue instance for comparison.</param>
    /// <param name="right">The second ThinkValue instance for comparison.</param>
    /// <returns>A boolean indicating whether the two ThinkValue instances are not equal.</returns>
    public static bool operator !=(ThinkValue left, ThinkValue right) => 
        !left.Equals(right);
}

/// <summary>
/// Converts a <see cref="ThinkValue"/> to or from JSON.
/// </summary>
public class ThinkValueConverter : JsonConverter<ThinkValue>
{
    /// <inheritdoc />
    public override ThinkValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new ThinkValue(reader.GetString()),
            JsonTokenType.True => new ThinkValue(true),
            JsonTokenType.False => new ThinkValue(false),
            JsonTokenType.Null => new ThinkValue(null as string),
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} for ThinkValue.")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ThinkValue value, JsonSerializerOptions options)
    {
        if (value.IsBoolean())
        {
            writer.WriteBooleanValue(value.ToBoolean() ?? false);
        }
        else
        {
            writer.WriteStringValue(value.ToString());				
        }
    }
}
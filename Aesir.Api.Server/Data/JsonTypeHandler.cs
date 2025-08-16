using System.Data;
using Dapper;
using Newtonsoft.Json;

namespace Aesir.Api.Server.Data;

/// <summary>
/// A custom Dapper TypeHandler that handles serialization and deserialization
/// of objects to and from JSON format in database columns.
/// </summary>
/// <typeparam name="T">The type of the object that this handler will serialize and deserialize.</typeparam>
public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    /// <summary>
    /// Sets the value of the parameter by serializing the provided object to a JSON string.
    /// </summary>
    /// <param name="parameter">The database parameter that will be assigned the JSON string value.</param>
    /// <param name="value">The object to serialize into a JSON string to set as the parameter value.</param>
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = value == null ? DBNull.Value : JsonConvert.SerializeObject(value);
    }

    /// <summary>
    /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The JSON string to be deserialized.</param>
    /// <returns>An object of type <typeparamref name="T"/> deserialized from the provided JSON string.</returns>
    public override T? Parse(object? value)
    {
        if (value == null || value == DBNull.Value)
            return default(T);

        if (value is string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return default(T);
            
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        // Handle cases where the value might already be the correct type
        if (value is T directValue)
            return directValue;

        // Last resort: try to convert to string first
        return JsonConvert.DeserializeObject<T>(value.ToString()!);
    }
}
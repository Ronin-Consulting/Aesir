using System.Text;
using Dapper;

namespace Aesir.Infrastructure.Data;

/// <summary>
/// Provides automatic column name mapping for Dapper to convert PascalCase C# properties to snake_case database columns.
/// This follows the AESIR convention of using snake_case for all database identifiers.
/// </summary>
public static class DapperColumnMapper
{
    private static bool _isInitialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Initializes the Dapper column name mapper to convert PascalCase to snake_case.
    /// This should be called once during application startup (typically in Program.cs).
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_isInitialized)
                return;

            // Set the default type map to use our custom mapping
            SqlMapper.SetTypeMap(
                typeof(object),
                new CustomPropertyTypeMap(
                    typeof(object),
                    (type, columnName) => type.GetProperty(ToPropertyName(columnName))));

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Converts a PascalCase property name to snake_case column name.
    /// Example: "FirstName" -> "first_name", "CreatedAt" -> "created_at"
    /// </summary>
    /// <param name="propertyName">The PascalCase property name.</param>
    /// <returns>The snake_case column name.</returns>
    public static string ToColumnName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return propertyName;

        var result = new StringBuilder();
        var isFirst = true;

        foreach (var c in propertyName)
        {
            if (char.IsUpper(c))
            {
                if (!isFirst)
                    result.Append('_');
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }

            isFirst = false;
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts a snake_case column name to PascalCase property name.
    /// Example: "first_name" -> "FirstName", "created_at" -> "CreatedAt"
    /// </summary>
    /// <param name="columnName">The snake_case column name.</param>
    /// <returns>The PascalCase property name.</returns>
    public static string ToPropertyName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return columnName;

        var result = new StringBuilder();
        var shouldCapitalize = true;

        foreach (var c in columnName)
        {
            if (c == '_')
            {
                shouldCapitalize = true;
            }
            else
            {
                result.Append(shouldCapitalize ? char.ToUpper(c) : c);
                shouldCapitalize = false;
            }
        }

        return result.ToString();
    }
}

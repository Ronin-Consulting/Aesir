using System.Text.RegularExpressions;
using Aesir.Common.FileTypes;
using StopWord;

namespace Aesir.Api.Server.Extensions;

/// <summary>
/// Provides extension methods for string manipulation and content type validation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Gets the MIME content type for a file based on its file path extension.
    /// </summary>
    /// <param name="filePath">The file path to determine the content type for.</param>
    /// <returns>The MIME content type or "application/octet-stream" if the content type cannot be determined.</returns>
    public static string GetMimeType(this string filePath) => FileTypeManager.GetMimeType(filePath);

    /// <summary>
    /// Removes a specified prefix from the beginning of a string if present.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="prefix">The prefix to remove from the beginning of the string.</param>
    /// <returns>The resulting string after the prefix is removed, or the original string if the prefix is not present.</returns>
    public static string TrimStart(this string input, string prefix)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(prefix))
            return input;

        return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? input[prefix.Length..]
            : input;
    }

    /// <summary>
    /// Extracts keywords from the input string while removing any stop words.
    /// </summary>
    /// <param name="input">The input string to extract keywords from.</param>
    /// <returns>An array of keywords without stop words and extraneous characters.</returns>
    public static string[] KeywordsOnly(this string input)
    {
        var noStopWords = input.RemoveStopWords("en");
        var keywords = Regex.Split(noStopWords, @"\W+")
            .Where(word => !string.IsNullOrWhiteSpace(word));
        
        return keywords.ToArray();
    }
}
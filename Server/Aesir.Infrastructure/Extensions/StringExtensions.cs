using System.Text.RegularExpressions;
using StopWord;

namespace Aesir.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static class StringExtensions
{
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

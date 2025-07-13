using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using StopWord;

namespace Aesir.Api.Server.Extensions;

/// <summary>
/// Provides extension methods for string manipulation and content type validation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Removes a portion of text between specified start and end delimiters.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="startAt">The start delimiter indicating the beginning of the text to remove.</param>
    /// <param name="endAt">The end delimiter indicating the end of the text to remove.</param>
    /// <returns>The input string with the specified portion removed, or unchanged if the delimiters are not found.</returns>
    public static string RemovePartOfCompletionMessage(this string input, string startAt, string endAt)
    {
        // Extract anything between <think> tags (including the tags themselves)
        var startIndex = input.IndexOf(startAt, StringComparison.Ordinal);

        if (startIndex < 0) return input;

        var endIndex = input.IndexOf(endAt, startIndex, StringComparison.Ordinal);

        if (endIndex >= 0)
        {
            // Remove everything from start of <think> to end of </think> (including tags)
            input = input.Remove(startIndex, (endIndex + endAt.Length) - startIndex);
        }
        else
        {
            // If closing tag is missing, just remove the opening tag as before
            input = input.Replace(startAt, "");
        }

        return input;
    }

    /// <summary>
    /// Gets the MIME content type for a file based on its file path extension.
    /// </summary>
    /// <param name="filePath">The file path to determine the content type for.</param>
    /// <returns>The MIME content type or "application/octet-stream" if the content type cannot be determined.</returns>
    public static string GetContentType(this string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            // If the content type cannot be determined, you can default to a generic one
            contentType = "application/octet-stream";
        }
        
        return contentType;
    }

    /// <summary>
    /// Validates whether the expected content type matches the actual content type of the specified file path.
    /// </summary>
    /// <param name="filePath">The file path whose content type needs to be validated.</param>
    /// <param name="contentType">The expected content type for validation.</param>
    /// <param name="fileContentType">The actual content type determined from the file path.</param>
    /// <returns>True if the file's actual content type matches the expected content type; otherwise, false.</returns>
    public static bool ValidFileContentType(this string filePath, string contentType, out string fileContentType)
    {
        fileContentType = GetContentType(filePath);
        return contentType == fileContentType;
    }


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
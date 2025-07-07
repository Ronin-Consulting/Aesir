using Microsoft.AspNetCore.StaticFiles;

namespace Aesir.Api.Server.Extensions;

/// <summary>
/// Provides extension methods for string manipulation and content type operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Removes a portion of text between specified start and end delimiters.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="startAt">The start delimiter.</param>
    /// <param name="endAt">The end delimiter.</param>
    /// <returns>The input string with the specified portion removed.</returns>
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
    /// <returns>The MIME content type or "application/octet-stream" if unknown.</returns>
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
    /// Validates whether the expected content type matches the file path's actual content type.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="contentType">The expected content type.</param>
    /// <param name="fileContentType">The actual content type determined from the file path.</param>
    /// <returns>True if the content types match; otherwise, false.</returns>
    public static bool ValidFileContentType(this string filePath, string contentType, out string fileContentType)
    {
        fileContentType = GetContentType(filePath);
        return contentType == fileContentType;
    }
    
    
    /// <summary>
    /// Removes a specified prefix from the beginning of a string if present.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="prefix">The prefix to remove.</param>
    /// <returns>The input string with the prefix removed, or the original string if the prefix is not found.</returns>
    public static string TrimStart(this string input, string prefix)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(prefix))
            return input;

        return input.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) 
            ? input[prefix.Length..] 
            : input;
    }
}
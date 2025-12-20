using Aesir.Client.Web.Infrastructure.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for parsing citation links from AI responses.
/// </summary>
/// <remarks>
/// Citation links follow the format: file:///conversationId/filename#page=N
/// This service extracts the conversation ID, filename, page number, and determines the file type.
/// </remarks>
public interface ICitationLinkParser
{
    /// <summary>
    /// Parses a citation link URL and extracts its components.
    /// </summary>
    /// <param name="url">The citation URL to parse.</param>
    /// <returns>A <see cref="CitationInfo"/> object if the URL is a valid citation, null otherwise.</returns>
    CitationInfo? ParseCitationLink(string url);

    /// <summary>
    /// Determines if a URL is a citation link.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if the URL is a file:// citation link, false otherwise.</returns>
    bool IsCitationLink(string? url);

    /// <summary>
    /// Gets the file type for a given file extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns>The corresponding <see cref="CitationFileType"/>.</returns>
    CitationFileType GetFileType(string extension);
}

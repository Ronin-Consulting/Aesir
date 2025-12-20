using System.Text.RegularExpressions;
using System.Web;
using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Client.Web.Infrastructure.Services;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for parsing citation links from AI responses.
/// </summary>
/// <remarks>
/// Citation links follow the format: file:///conversationId/filename#page=N
/// Examples:
/// - file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf#page=5
/// - file:///91c3a876-895d-48bc-80c1-ee917f0026ca/diagram.png
/// </remarks>
public partial class CitationLinkParser : ICitationLinkParser
{
    // Regex to match file:// URLs with optional page fragment
    // Groups: 1=conversationId (GUID), 2=filename, 3=page number (optional)
    [GeneratedRegex(
        @"^file:///([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/(.+?)(?:#page=(\d+))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CitationUrlPattern();

    // Regex to extract page number from fragment
    [GeneratedRegex(@"#page=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PageFragmentPattern();

    // File extension mappings to CitationFileType
    private static readonly Dictionary<string, CitationFileType> ExtensionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // PDF
        { "pdf", CitationFileType.Pdf },

        // Images
        { "png", CitationFileType.Image },
        { "jpg", CitationFileType.Image },
        { "jpeg", CitationFileType.Image },
        { "gif", CitationFileType.Image },
        { "webp", CitationFileType.Image },
        { "bmp", CitationFileType.Image },

        // Multi-page TIFF
        { "tiff", CitationFileType.Tiff },
        { "tif", CitationFileType.Tiff },

        // Text files
        { "txt", CitationFileType.Text },
        { "log", CitationFileType.Text },

        // Structured data
        { "json", CitationFileType.Json },
        { "xml", CitationFileType.Xml },
        { "csv", CitationFileType.Csv },

        // Markup
        { "md", CitationFileType.Markdown },
        { "markdown", CitationFileType.Markdown },
        { "html", CitationFileType.Html },
        { "htm", CitationFileType.Html }
    };

    /// <inheritdoc />
    public CitationInfo? ParseCitationLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // URL decode the URL first to handle encoded characters
        var decodedUrl = HttpUtility.UrlDecode(url);

        var match = CitationUrlPattern().Match(decodedUrl);
        if (!match.Success)
        {
            return null;
        }

        var conversationId = match.Groups[1].Value;
        var fileName = match.Groups[2].Value;

        // Extract page number if present
        int? pageNumber = null;
        if (match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var page))
        {
            pageNumber = page;
        }

        // Get file extension
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var fileType = GetFileType(extension);

        return new CitationInfo
        {
            ConversationId = conversationId,
            FileName = fileName,
            FileExtension = extension,
            PageNumber = pageNumber,
            FileType = fileType,
            OriginalUrl = url
        };
    }

    /// <inheritdoc />
    public bool IsCitationLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Quick check before expensive regex
        if (!url.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var decodedUrl = HttpUtility.UrlDecode(url);
        return CitationUrlPattern().IsMatch(decodedUrl);
    }

    /// <inheritdoc />
    public CitationFileType GetFileType(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return CitationFileType.Unknown;
        }

        // Remove leading dot if present
        var cleanExtension = extension.TrimStart('.');

        return ExtensionMappings.TryGetValue(cleanExtension, out var fileType)
            ? fileType
            : CitationFileType.Unknown;
    }
}

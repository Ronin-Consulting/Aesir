namespace Aesir.Client.Web.Infrastructure.Models;

/// <summary>
/// Represents the type of file referenced in a citation.
/// </summary>
public enum CitationFileType
{
    /// <summary>PDF document.</summary>
    Pdf,
    /// <summary>Image file (PNG, JPEG, GIF, WebP).</summary>
    Image,
    /// <summary>Multi-page TIFF image.</summary>
    Tiff,
    /// <summary>Plain text file.</summary>
    Text,
    /// <summary>JSON file.</summary>
    Json,
    /// <summary>XML file.</summary>
    Xml,
    /// <summary>CSV file.</summary>
    Csv,
    /// <summary>Markdown file.</summary>
    Markdown,
    /// <summary>HTML file.</summary>
    Html,
    /// <summary>Unknown or unsupported file type.</summary>
    Unknown
}

/// <summary>
/// Represents parsed information from a citation link.
/// </summary>
/// <remarks>
/// Citation links follow the format: file:///conversationId/filename#page=N
/// Examples:
/// - file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf#page=5
/// - file:///91c3a876-895d-48bc-80c1-ee917f0026ca/diagram.png
/// </remarks>
public sealed record CitationInfo
{
    /// <summary>
    /// Gets the conversation ID that owns this document.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    /// Gets the filename including extension.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the file extension (without the leading dot), lowercase.
    /// </summary>
    public required string FileExtension { get; init; }

    /// <summary>
    /// Gets the page number for multi-page documents (1-based), or null if not specified.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Gets the determined file type based on the extension.
    /// </summary>
    public required CitationFileType FileType { get; init; }

    /// <summary>
    /// Gets the original raw URL that was parsed.
    /// </summary>
    public required string OriginalUrl { get; init; }

    /// <summary>
    /// Gets the display text for the citation (typically the filename with page info).
    /// </summary>
    public string DisplayText => PageNumber.HasValue
        ? $"{FileName}#page={PageNumber}"
        : FileName;
}

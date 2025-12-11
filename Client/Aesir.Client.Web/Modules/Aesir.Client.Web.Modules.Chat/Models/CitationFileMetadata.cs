namespace Aesir.Client.Web.Modules.Chat.Models;

/// <summary>
/// Metadata about a citation file.
/// </summary>
public sealed record CitationFileMetadata
{
    /// <summary>
    /// Gets the filename.
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Gets the MIME type of the file.
    /// </summary>
    public required string MimeType { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public required long FileSize { get; init; }

    /// <summary>
    /// Gets the number of pages for multi-page documents (PDFs, TIFFs), or null for single-page documents.
    /// </summary>
    public int? PageCount { get; init; }

    /// <summary>
    /// Gets the date/time the file was created/uploaded.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Gets whether a thumbnail is available for this file.
    /// </summary>
    public bool HasThumbnail { get; init; }

    /// <summary>
    /// Gets a formatted file size string (e.g., "1.5 MB").
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        var order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

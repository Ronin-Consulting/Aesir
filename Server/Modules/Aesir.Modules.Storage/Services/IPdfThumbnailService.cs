namespace Aesir.Modules.Storage.Services;

/// <summary>
/// Service for generating thumbnail images from PDF documents.
/// </summary>
public interface IPdfThumbnailService
{
    /// <summary>
    /// Generates a thumbnail image from the first page of a PDF document.
    /// </summary>
    /// <param name="pdfContent">The PDF document content as a byte array.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A thumbnail result containing the image bytes and metadata, or null if generation fails.</returns>
    Task<ThumbnailResult?> GenerateThumbnailAsync(byte[] pdfContent, CancellationToken ct = default);

    /// <summary>
    /// Generates a thumbnail image from the first page of a PDF document stream.
    /// </summary>
    /// <param name="pdfStream">The PDF document stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A thumbnail result containing the image bytes and metadata, or null if generation fails.</returns>
    Task<ThumbnailResult?> GenerateThumbnailAsync(Stream pdfStream, CancellationToken ct = default);
}

/// <summary>
/// Represents the result of a thumbnail generation operation.
/// </summary>
/// <param name="Content">The thumbnail image bytes.</param>
/// <param name="MimeType">The MIME type of the thumbnail image.</param>
/// <param name="Width">The width of the thumbnail in pixels.</param>
/// <param name="Height">The height of the thumbnail in pixels.</param>
public record ThumbnailResult(byte[] Content, string MimeType, int Width, int Height);

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides vision AI functionality for processing images and extracting text.
/// </summary>
public interface IVisionService
{
    /// <summary>
    /// Extracts text from an image using vision AI models.
    /// </summary>
    /// <param name="image">The image data as a byte array.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the extracted text.</returns>
    Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default);
}
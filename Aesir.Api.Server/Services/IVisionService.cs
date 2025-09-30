namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides vision AI functionality for processing images and extracting text.
/// </summary>
public interface IVisionService
{
    /// <summary>
    /// Extracts text from an image using vision AI models.
    /// </summary>
    /// <param name="modelLocationDescriptor">Information about where to load the model from.</param>
    /// <param name="imageBytes">The image data as a byte array.</param>
    /// <param name="contentType">The MIME type of the image.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the extracted text.</returns>
    Task<string> GetImageTextAsync(ModelLocationDescriptor modelLocationDescriptor, 
        ReadOnlyMemory<byte> imageBytes, string contentType, CancellationToken cancellationToken = default);
}
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;

namespace Aesir.Modules.Storage.Services;

/// <summary>
/// Service for generating thumbnail images from PDF documents using PDFtoImage library.
/// </summary>
public class PdfThumbnailService : IPdfThumbnailService
{
    private readonly ILogger<PdfThumbnailService> _logger;

    // Thumbnail settings
    private const int MaxWidth = 200;
    private const int MaxHeight = 260;
    private const int WebPQuality = 80;
    private const string ThumbnailMimeType = "image/webp";

    public PdfThumbnailService(ILogger<PdfThumbnailService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ThumbnailResult?> GenerateThumbnailAsync(byte[] pdfContent, CancellationToken ct = default)
    {
        if (pdfContent == null || pdfContent.Length == 0)
        {
            _logger.LogWarning("Cannot generate thumbnail: PDF content is null or empty");
            return null;
        }

        using var stream = new MemoryStream(pdfContent);
        return await GenerateThumbnailAsync(stream, ct);
    }

    /// <inheritdoc />
    public Task<ThumbnailResult?> GenerateThumbnailAsync(Stream pdfStream, CancellationToken ct = default)
    {
        if (pdfStream == null || !pdfStream.CanRead)
        {
            _logger.LogWarning("Cannot generate thumbnail: PDF stream is null or not readable");
            return Task.FromResult<ThumbnailResult?>(null);
        }

        try
        {
            // Render the first page of the PDF to a bitmap
            using var bitmap = Conversion.ToImage(pdfStream, page: 0);

            if (bitmap == null)
            {
                _logger.LogWarning("Failed to render PDF first page - bitmap is null");
                return Task.FromResult<ThumbnailResult?>(null);
            }

            // Calculate scaled dimensions maintaining aspect ratio
            var (scaledWidth, scaledHeight) = CalculateScaledDimensions(bitmap.Width, bitmap.Height);

            // Resize the bitmap if needed
            SKBitmap finalBitmap;
            if (scaledWidth != bitmap.Width || scaledHeight != bitmap.Height)
            {
                finalBitmap = bitmap.Resize(new SKImageInfo(scaledWidth, scaledHeight), SKFilterQuality.High);
                if (finalBitmap == null)
                {
                    _logger.LogWarning("Failed to resize bitmap");
                    return Task.FromResult<ThumbnailResult?>(null);
                }
            }
            else
            {
                finalBitmap = bitmap;
            }

            try
            {
                // Encode as WebP
                using var image = SKImage.FromBitmap(finalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Webp, WebPQuality);

                if (data == null)
                {
                    _logger.LogWarning("Failed to encode bitmap as WebP");
                    return Task.FromResult<ThumbnailResult?>(null);
                }

                var thumbnailBytes = data.ToArray();

                _logger.LogDebug(
                    "Generated PDF thumbnail: {Width}x{Height}, {Size} bytes",
                    scaledWidth, scaledHeight, thumbnailBytes.Length);

                return Task.FromResult<ThumbnailResult?>(new ThumbnailResult(
                    thumbnailBytes,
                    ThumbnailMimeType,
                    scaledWidth,
                    scaledHeight));
            }
            finally
            {
                // Dispose resized bitmap if it's different from original
                if (finalBitmap != bitmap)
                {
                    finalBitmap.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate PDF thumbnail: {Message}", ex.Message);
            return Task.FromResult<ThumbnailResult?>(null);
        }
    }

    /// <summary>
    /// Calculates scaled dimensions maintaining aspect ratio within max bounds.
    /// </summary>
    private static (int Width, int Height) CalculateScaledDimensions(int originalWidth, int originalHeight)
    {
        if (originalWidth <= MaxWidth && originalHeight <= MaxHeight)
        {
            return (originalWidth, originalHeight);
        }

        var widthRatio = (double)MaxWidth / originalWidth;
        var heightRatio = (double)MaxHeight / originalHeight;
        var ratio = Math.Min(widthRatio, heightRatio);

        return (
            (int)Math.Round(originalWidth * ratio),
            (int)Math.Round(originalHeight * ratio)
        );
    }
}

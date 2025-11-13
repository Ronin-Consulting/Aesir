namespace Aesir.Modules.Documents.Models;

/// <summary>
/// Represents the raw content extracted from a processed source, including text, images, and associated metadata.
/// </summary>
public sealed class RawContent
{
    /// <summary>
    /// Gets the raw text content.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or initializes the raw binary content of an image, represented as a read-only memory block of bytes.
    /// </summary>
    public ReadOnlyMemory<byte>? Image { get; init; }

    /// <summary>
    /// Gets the page number associated with the content.
    /// </summary>
    public int PageNumber { get; init; }
}

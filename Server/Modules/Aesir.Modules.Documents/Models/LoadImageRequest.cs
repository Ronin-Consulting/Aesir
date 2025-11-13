using Aesir.Infrastructure.Services;

namespace Aesir.Modules.Documents.Models;

/// <summary>
/// Represents a request to load an image, including the image's local path, file name, and optional metadata.
/// </summary>
public sealed class LoadImageRequest
{
    /// <summary>
    /// Gets or sets the local file path of the image.
    /// </summary>
    /// <remarks>
    /// This property represents the location of an image stored on the local file system.
    /// It can be used to specify or retrieve the path for loading or saving an image.
    /// </remarks>
    public string? ImageLocalPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the image file associated with the load request.
    /// This property represents the file name, including its extension, but does not include the file path.
    /// </summary>
    public string? ImageFileName { get; set; }

    /// <summary>
    /// A property representing additional metadata that can be associated
    /// with a <see cref="LoadImageRequest"/>. This metadata is a collection
    /// of key-value pairs, where the key is a string and the value is an object.
    /// The metadata can be used to provide contextual information or additional
    /// attributes related to the image loading process.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the size of the batch for processing PDF content.
    /// Determines the number of content items to process in a single batch operation.
    /// </summary>
    public int BatchSize { get; set; } = Math.Max(Environment.ProcessorCount / 2 + 1, 10);

    /// <summary>
    /// Gets or sets the model location descriptor.
    /// </summary>
    /// <remarks>
    /// This property represents metadata identifying the location or descriptor of a model,
    /// including necessary details such as interface engine ID and model ID.
    /// It can be used to specify or retrieve information about the model's source or context.
    /// </remarks>
    public ModelLocationDescriptor? ModelLocation { get; set; }
}

using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Represents a service for loading image data with associated metadata and processing logic.
/// </summary>
/// <typeparam name="TKey">
/// The type of the key used to uniquely identify records.
/// Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the record containing image data and metadata.
/// Must extend <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
[Experimental("SKEXP0001")]
public interface IImageDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// Loads an image asynchronously based on the specified parameters in the request.
    /// </summary>
    /// <param name="request">The request object containing the image path, file name, and associated metadata.</param>
    /// <param name="cancellationToken">The token used to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken);
}

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
}
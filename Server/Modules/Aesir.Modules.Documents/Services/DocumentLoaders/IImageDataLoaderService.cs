using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Models;
using Aesir.Modules.Documents.Models;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

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
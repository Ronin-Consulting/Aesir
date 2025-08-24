using System.Diagnostics.CodeAnalysis;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// A service for processing and loading image data into a structured format. It integrates
/// vision services, embedding generators, and AI models to transform raw image inputs into
/// meaningful records suitable for downstream applications and storage in vectorized formats.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique key associated with each record. This must be non-null and ensure unique identification.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type representing the structured data record derived from image content. This type
/// must extend from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// Marked experimental under the identifier "SKEXP0001," this service is designed to manage
/// scalable and systematic image processing workflows. It leverages a combination of vision
/// recognition, embedding models, and configurable data pipelines to create structured outputs.
/// </remarks>
/// <seealso cref="BaseDataLoaderService{TKey, TRecord}"/>
/// <seealso cref="IImageDataLoaderService{TKey, TRecord}"/>
[Experimental("SKEXP0001")]
public class ImageDataLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadImageRequest, TRecord> recordFactory,
    IVisionService visionService,
    IModelsService modelsService,
    ILogger<ImageDataLoaderService<TKey, TRecord>> logger)
    : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator,
        modelsService, logger), IImageDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// A delegate function used to create an instance of <typeparamref name="TRecord"/>.
    /// This factory function takes a <see cref="RawContent"/> and a <see cref="LoadImageRequest"/>
    /// as parameters and produces a record of type <typeparamref name="TRecord"/>.
    /// </summary>
    /// <typeparam name="TRecord">The type of record being created.</typeparam>
    private readonly Func<RawContent, LoadImageRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Represents a private, readonly dependency on the <see cref="IVisionService"/> interface,
    /// used for operations and functionality related to vision or image processing.
    /// </summary>
    private readonly IVisionService _visionService = visionService;

    /// <summary>
    /// Asynchronously loads an image, processes its contents to extract textual information and metadata, and updates the vector store with the processed records.
    /// </summary>
    /// <param name="request">An object containing the image file's local path, file name, and other metadata required for processing.</param>
    /// <param name="cancellationToken">A token to observe during the asynchronous operation, allowing it to be canceled if requested.</param>
    /// <returns>A task representing the completion of the image processing and data storage operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the image path or file name in the request is invalid or empty.</exception>
    /// <exception cref="NotSupportedException">Thrown when the file's content type is unsupported, such as formats other than PNG.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled using the provided cancellation token.</exception>
    public async Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageLocalPath))
            throw new InvalidOperationException("ImageLocalPath is empty");

        if (string.IsNullOrEmpty(request.ImageFileName))
            throw new InvalidOperationException("ImageFileName is empty");

        // Validate PNG support
        if (!request.ImageFileName.ValidFileContentType(out var actualContentType, 
                SupportedFileContentTypes.PngContentType,
                SupportedFileContentTypes.JpegContentType,
                SupportedFileContentTypes.TiffContentType
        ))
        throw new NotSupportedException($"Only PNG images are currently supported and not: {actualContentType}");

        await InitializeCollectionAsync(cancellationToken);
        await DeleteExistingRecordsAsync(request.ImageFileName!, cancellationToken);

        // Load and process the image
        var imageBytes = await File.ReadAllBytesAsync(request.ImageLocalPath, cancellationToken).ConfigureAwait(false);

        // Convert image to text using vision service
        var extractedText = await ConvertImageToTextWithRetryAsync(
            new ReadOnlyMemory<byte>(imageBytes),
            actualContentType,
            cancellationToken).ConfigureAwait(false);

        // Create raw content for processing
        var rawContent = new RawContent
        {
            Text = extractedText,
            PageNumber = 1 // Images are treated as single page
        };

        // Chunk the text for better processing
        var textChunks = DocumentChunker.ChunkText(rawContent.Text!,
            $"Image: {request.ImageFileName}\nPage: {rawContent.PageNumber}\n");

        // Create records from chunks and process them
        var records = textChunks.Select(chunk =>
        {
            var chunkContent = new RawContent
            {
                Text = chunk,
                PageNumber = 1
            };

            var record = _recordFactory(chunkContent, request);
            record.Text ??= chunk;
            return record;
        }).ToArray();

        var processedRecords =
            await ProcessRecordsInBatchesAsync(records, request.ImageFileName!, records.Length, cancellationToken);
        await UpsertRecordsAsync(processedRecords, cancellationToken);

        await UnloadModelsAsync();
    }


    /// <summary>
    /// Attempts to extract text from the provided image bytes by processing the image data and retrying up to three times
    /// in case of a "Too Many Requests" error from the service.
    /// </summary>
    /// <param name="imageBytes">The image data in the form of a read-only memory of bytes.</param>
    /// <param name="cancellationToken">A token for observing and potentially canceling the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the extracted text from the image.</returns>
    /// <exception cref="HttpOperationException">
    /// Thrown if an HTTP error, other than "Too Many Requests", occurs or if the maximum retry limit is exceeded.
    /// </exception>
    private async Task<string> ConvertImageToTextWithRetryAsync(
        ReadOnlyMemory<byte> imageBytes,
        string contentType,
        CancellationToken cancellationToken)
    {
        return await _visionService
            .GetImageTextAsync(imageBytes, contentType, cancellationToken)
            .ConfigureAwait(false);
    }
}
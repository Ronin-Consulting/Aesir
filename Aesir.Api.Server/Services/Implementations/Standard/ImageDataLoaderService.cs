using System.Diagnostics.CodeAnalysis;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SixLabors.ImageSharp;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// A class responsible for loading and processing image data to create structured records for
/// vectorized storage and downstream applications.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique identifier associated with each generated record. Must be a non-nullable type.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type of the output record containing structured data derived from the processed image content.
/// This type must inherit from <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// An experimental class marked with identifier "SKEXP0001" designed for scalable image data ingestion.
/// It combines vision services, embedding generation, and configurable processing pipelines to extract
/// insights and generate structured outputs that are compatible with vector stores.
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
    /// A function delegate that instantiates an object of type <typeparamref name="TRecord"/>.
    /// This factory function takes an instance of <see cref="RawContent"/> and <see cref="LoadImageRequest"/>
    /// as input to generate and return a corresponding record of type <typeparamref name="TRecord"/>.
    /// </summary>
    /// <typeparam name="TRecord">The type of the record to be created.</typeparam>
    private readonly Func<RawContent, LoadImageRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// A private, readonly dependency on the <see cref="IVisionService"/> interface,
    /// utilized for vision or image processing-related operations such as analyzing images
    /// and extracting relevant information.
    /// </summary>
    private readonly IVisionService _visionService = visionService;

    /// <summary>
    /// Asynchronously loads and processes an image to extract textual content and metadata, and updates the vector store with the resulting records.
    /// </summary>
    /// <param name="request">Contains information about the image to be processed, including its local file path and file name.</param>
    /// <param name="cancellationToken">A token used to monitor for cancellation requests during the asynchronous operation.</param>
    /// <returns>A task that completes when the image has been successfully processed and the data has been stored.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the image's local path or file name is null or empty.</exception>
    /// <exception cref="NotSupportedException">Thrown if the image file type is unsupported (e.g., formats other than PNG, JPEG, or TIFF).</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the provided cancellation token.</exception>
    public async Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageLocalPath))
            throw new InvalidOperationException("ImageLocalPath is empty");

        if (string.IsNullOrEmpty(request.ImageFileName))
            throw new InvalidOperationException("ImageFileName is empty");
        
        if (!request.ImageFileName.ValidFileContentType(out var actualContentType,
                FileTypeManager.MimeTypes.Png,
                FileTypeManager.MimeTypes.Jpeg,
                FileTypeManager.MimeTypes.Tiff
            ))
            throw new NotSupportedException($"Only PNG images are currently supported and not: {actualContentType}");

        await InitializeCollectionAsync(cancellationToken);
        await DeleteExistingRecordsAsync(request.ImageFileName!, cancellationToken);

        // Load and process the image
        var imageBytes = await File.ReadAllBytesAsync(request.ImageLocalPath, cancellationToken).ConfigureAwait(false);
        
        TRecord[] records;
        // we need to process TIFF images differently because they can have more than one page
        if (actualContentType == FileTypeManager.MimeTypes.Tiff)
        {
            // load the tiff and process all of its pages
            
            using var tiffImage = Image.Load(imageBytes);
            
            var tiffRecords = new List<TRecord>();

            // Process each frame/page in the TIFF
            for (var pageIndex = 0; pageIndex < tiffImage.Frames.Count; pageIndex++)
            {
                // Extract the current frame as a separate image
                using var pageImage = tiffImage.Frames.CloneFrame(pageIndex);

                // Convert the page to bytes
                using var pageStream = new MemoryStream();
                await pageImage.SaveAsPngAsync(pageStream, cancellationToken);
                var pageBytes = pageStream.ToArray();
                
                // Convert this page to text using vision service
                var extractedText = await ConvertImageToTextWithRetryAsync(
                    new ReadOnlyMemory<byte>(pageBytes),
                    FileTypeManager.MimeTypes.Png, // Convert to PNG for processing
                    cancellationToken).ConfigureAwait(false);

                // Create raw content for this page
                var rawContent = new RawContent
                {
                    Text = extractedText,
                    PageNumber = pageIndex + 1 // 1-based page numbering
                };

                // Chunk the text for better processing
                var textChunks = DocumentChunker.ChunkText(rawContent.Text!,
                    $"Image: {request.ImageFileName}\nPage: {rawContent.PageNumber}\n");

                // Create records from chunks for this page
                var pageRecords = textChunks.Select(chunk =>
                {
                    var chunkContent = new RawContent
                    {
                        Text = chunk,
                        PageNumber = pageIndex + 1
                    };
                    
                    var record = _recordFactory(chunkContent, request);
                    record.Text ??= chunk;
                    return record;
                }).ToArray();
                
                tiffRecords.AddRange(pageRecords);
            }

            records = tiffRecords.ToArray();
        }
        else
        {
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
            records = textChunks.Select(chunk =>
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
        }
        
        var processedRecords =
            await ProcessRecordsInBatchesAsync(records, request.ImageFileName!, request.BatchSize, cancellationToken);
        
        await UpsertRecordsAsync(processedRecords, cancellationToken);

        await UnloadModelsAsync();
    }


    /// <summary>
    /// Attempts to extract text from the given image data with a retry mechanism, handling "Too Many Requests" errors
    /// by retrying the operation up to three times before failing.
    /// </summary>
    /// <param name="imageBytes">The image data as a read-only memory of bytes.</param>
    /// <param name="contentType">The content type of the image, used to validate and process the image format.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests during the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation, returning the extracted text from the image.</returns>
    /// <exception cref="HttpOperationException">
    /// Thrown if an HTTP error occurs during the operation, excluding "Too Many Requests", or if the retry limit is exceeded.
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
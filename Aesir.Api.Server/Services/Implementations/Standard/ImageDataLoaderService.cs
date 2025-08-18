using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aesir.Api.Server.Extensions;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Tiktoken;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// A service responsible for loading image data and converting it into structured records
/// utilizing embeddings, AI models, and extensible processing pipelines. It is designed to facilitate
/// scalable and systematic ingestion of image-based content.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique key associated with each record. Must be non-null and uniquely identifiable.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type representing a data record produced from the image content. This type must inherit from
/// <see cref="AesirTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// This service is marked as experimental and labeled with the identifier "SKEXP0001". It combines
/// vision services, model integration, and embedding generators to process raw image data into
/// structured entities for downstream applications.
/// </remarks>
/// <example>
/// Suitable for use cases involving large-scale image processing and transformation into
/// AI-compatible data for storage in vectorized formats.
/// </example>
/// <seealso cref="IImageDataLoaderService{TKey, TRecord}"/>
[Experimental("SKEXP0001")]
public class ImageDataLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadImageRequest, TRecord> recordFactory,
    IVisionService visionService,
    IModelsService modelsService,
    ILogger<ImageDataLoaderService<TKey, TRecord>> logger) : IImageDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Static readonly instance used for token counting in text data processing.
    /// </summary>
    /// <remarks>
    /// The <c>TokenCounter</c> variable is statically initialized using the default encoding from the <c>DocumentChunker</c>.
    /// It is crucial in the <c>ImageDataLoaderService</c> for evaluating the token count of textual content, ensuring adherence to token-based requirements
    /// in scenarios such as API interactions or other constraints dependent on text tokenization.
    /// </remarks>
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);

    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// A readonly instance of the <c>DocumentChunker</c> class, utilized for dividing documents
    /// into smaller segments based on predefined token limits.
    /// </summary>
    /// <remarks>
    /// The <c>DocumentChunker</c> variable is designed to facilitate the efficient processing
    /// of extensive text data by splitting it into manageable chunks. This is especially beneficial
    /// in cases such as embedding generation, token-bound APIs, or other operations requiring
    /// size-limited segments. The token limits are configured within the implementation, ensuring
    /// consistency and adherence to predefined constraints.
    /// </remarks>
    private static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Asynchronously loads an image, extracts textual and visual information from it, and stores the processed result in a vector store database.
    /// </summary>
    /// <param name="request">The details about the image to be loaded, including file path, file name, and other relevant metadata for processing.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the asynchronous operation to complete, allowing the operation to be canceled.</param>
    /// <returns>A task representing the asynchronous operation to load, process, and store the image.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the input request contains invalid data or when required configurations are missing.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the provided request is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the provided cancellation token.</exception>
    public async Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageLocalPath))
            throw new InvalidOperationException("ImageLocalPath is empty");

        if (string.IsNullOrEmpty(request.ImageFileName))
            throw new InvalidOperationException("ImageFileName is empty");

        // Validate PNG support
        if (!request.ImageFileName.ValidFileContentType(SupportedFileContentTypes.PngContentType,
                out var actualContentType))
            throw new NotSupportedException($"Only PNG images are currently supported and not: {actualContentType}");

        // Create the collection if it doesn't exist
        await vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);

        // Delete any existing records with the same image name
        var toDelete = await vectorStoreRecordCollection.GetAsync(
                filter: data => data.Text != null,
                int.MaxValue,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(request.ImageFileName.TrimStart("file://"))).ToList();

        if (toDelete.Count > 0)
            await vectorStoreRecordCollection.DeleteAsync(
                toDelete.Select(td => td.Key), cancellationToken);

        // Load and process the image
        var imageBytes = await File.ReadAllBytesAsync(request.ImageLocalPath, cancellationToken).ConfigureAwait(false);
        
        // Convert image to text using vision service
        var extractedText = await ConvertImageToTextWithRetryAsync(
            new ReadOnlyMemory<byte>(imageBytes), 
            cancellationToken).ConfigureAwait(false);

        // Create raw content for processing
        var rawContent = new RawContent 
        { 
            Text = extractedText, 
            PageNumber = 1 // Images are treated as single page
        };

        // Chunk the text for better processing
        var textChunks = DocumentChunker.ChunkText(rawContent.Text!, $"Image: {request.ImageFileName}\nPage: {rawContent.PageNumber}\n");
        
        // Process each chunk
        var recordTasks = textChunks.Select(async chunk =>
        {
            var chunkContent = new RawContent 
            { 
                Text = chunk, 
                PageNumber = 1 
            };
            
            var record = recordFactory(chunkContent, request);
            record.Key = uniqueKeyGenerator.GenerateKey();
            record.Text ??= chunk;
            record.ReferenceDescription ??= request.ImageFileName.TrimStart("file://");
            record.ReferenceLink ??= new Uri($"file://{request.ImageFileName.TrimStart("file://")}").AbsoluteUri;
            record.TextEmbedding ??= await GenerateEmbeddingsWithRetryAsync(chunk, cancellationToken);
            record.TokenCount ??= TokenCounter.CountTokens(record.Text!);
            
            return record;
        });

        var records = await Task.WhenAll(recordTasks).ConfigureAwait(false);

        // Upsert the records into the vector store
        await vectorStoreRecordCollection
            .UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Unload models to free resources
        await modelsService.UnloadVisionModelAsync();
        await modelsService.UnloadEmbeddingModelAsync();
    }

    /// <summary>
    /// Generates an embedding for the specified text, implementing retry mechanisms to handle transient failures such as rate limiting or client errors.
    /// </summary>
    /// <param name="text">The input text for which the embedding is to be generated.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, which returns the generated embedding for the input text.</returns>
    /// <exception cref="HttpOperationException">Thrown when the operation fails due to HTTP errors, including rate-limiting scenarios where the retry logic exceeds allowed retry attempts.</exception>
    /// <exception cref="ClientResultException">Thrown when the client cannot process the request successfully due to result-related errors.</exception>
    private async Task<Embedding<float>> GenerateEmbeddingsWithRetryAsync(string text,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                var options = new EmbeddingGenerationOptions()
                {
                    Dimensions = 1024
                };
                var vector = (await embeddingGenerator
                    .GenerateAsync(text, options, cancellationToken: cancellationToken).ConfigureAwait(false)).Vector;

                return new Embedding<float>(vector);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    logger.LogWarning("Failed to generate embedding. Error: {Exception}", ex);
                    logger.LogWarning("Retrying embedding generation...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
            catch (ClientResultException ex)
            {
                logger.LogError("Failed to generate embedding. Error: {HttpOperationException}", 
                    ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());
                
                throw;
            }
        }
    }

    /// <summary>
    /// Attempts to convert image data to text by processing the provided image bytes.
    /// Retries the operation up to three times if the service responds with a "Too Many Requests" error.
    /// </summary>
    /// <param name="imageBytes">The image data represented as a read-only memory of bytes.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete, which can be used to cancel the operation.</param>
    /// <returns>A task containing the text extracted from the image upon successful execution.</returns>
    /// <exception cref="HttpOperationException">
    /// Thrown if an HTTP error, other than "Too Many Requests", prevents successful execution
    /// or if the maximum retry attempts are exceeded.
    /// </exception>
    private async Task<string> ConvertImageToTextWithRetryAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                return await visionService
                    .GetImageTextAsync(imageBytes, SupportedFileContentTypes.PngContentType, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    logger.LogInformation("Failed to generate text from image. Error: {HttpOperationException}", ex);
                    logger.LogInformation("Retrying text to image conversion...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Tiktoken;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// A service responsible for loading image data and converting it into meaningful records
/// using embeddings and AI-enhanced processing. Designed to handle complex image data ingestion
/// pipelines in a systematic and extensible manner.
/// </summary>
/// <typeparam name="TKey">
/// The type of the unique key associated with each record. Must not be null.
/// </typeparam>
/// <typeparam name="TRecord">
/// The type representing a data record derived from image content. This must inherit from
/// <see cref="AesirConversationDocumentTextData{TKey}"/>.
/// </typeparam>
/// <remarks>
/// This service is experimental and is marked with the "SKEXP0001" identifier. It relies on
/// embeddings, AI models, and an extensible record factory to transform raw content into structured data.
/// </remarks>
/// <example>
/// Use this service when you need to process and load image data into a vector storage collection.
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
    where TRecord : AesirConversationDocumentTextData<TKey>
{
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Static readonly instance of the Tiktoken Encoder used to count tokens in text data.
    /// </summary>
    /// <remarks>
    /// The <c>TokenCounter</c> variable is initialized with the default encoding provided by the <c>DocumentChunker</c>.
    /// It is utilized in the <c>ImageDataLoaderService</c> to calculate the token count of text content during processing.
    /// This ensures token limits are respected for operations that depend on token-based APIs or specific constraints.
    /// </remarks>
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Represents a utility class for dividing documents into smaller chunks based on token limits.
    /// This class is intended to be used in scenarios where text data needs to be split into manageable
    /// or processable segments for downstream tasks such as text analysis or embedding generation.
    /// </summary>
    private static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Asynchronously loads an image, processes its content to extract text, and stores the extracted data into a vector store collection.
    /// </summary>
    /// <param name="request">The request containing information about the image to load, such as file path and file name.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation to load and process the image.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the image file path or file name is null or empty in the provided request.</exception>
    /// <exception cref="NotSupportedException">Thrown when the image type is not supported (e.g., if the image is not in PNG format).</exception>
    public async Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ImageLocalPath))
            throw new InvalidOperationException("ImageLocalPath is empty");

        if (string.IsNullOrEmpty(request.ImageFileName))
            throw new InvalidOperationException("ImageFileName is empty");

        // Validate PNG support
        if (!request.ImageFileName.ValidFileContentType(SupportedFileContentTypes.PngContentType, out var actualContentType))
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
        var textChunks = DocumentChunker.ChunkText(rawContent.Text!, $"Image: {request.ImageFileName}\nPage: {rawContent.PageNumber}");
        
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
    /// Generates an embedding for the specified text, with automatic retry logic in case of transient errors.
    /// </summary>
    /// <param name="text">The text for which the embedding is to be generated.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An embedding representation of the input text.</returns>
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
    /// Attempts to convert an image to text by processing the provided image data.
    /// Retries the operation up to three times if the service responds with a "Too Many Requests" error.
    /// </summary>
    /// <param name="imageBytes">The image data as a read-only memory of bytes.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the extracted text from the image.</returns>
    /// <exception cref="HttpOperationException">
    /// Thrown when the operation fails due to an HTTP error other than "Too Many Requests"
    /// or when the maximum retry attempts are exceeded.
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
                return await visionService.GetImageTextAsync(imageBytes, SupportedFileContentTypes.PngContentType, cancellationToken).ConfigureAwait(false);
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
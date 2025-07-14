using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using Tiktoken;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Handles the process of loading PDF files, extracting their content, and storing the resulting textual data into a vector store.
/// </summary>
/// <typeparam name="TKey">The data type used for the unique key identification of records.</typeparam>
/// <typeparam name="TRecord">The data type that represents a record of text content.</typeparam>
/// <param name="uniqueKeyGenerator">Service responsible for creating unique keys for new records.</param>
/// <param name="vectorStoreRecordCollection">The storage medium used to keep and manage vectorized data records.</param>
/// <param name="embeddingGenerator">Generator responsible for producing embeddings from text input.</param>
/// <param name="recordFactory">Function to create records from raw content and load requests.</param>
/// <param name="visionService">Service for processing visual content if relevant.</param>
/// <param name="modelsService">Service for accessing and managing AI models used for processing.</param>
/// <param name="logger">Logger instance for logging operational and debugging information.</param>
[Experimental("SKEXP0001")]
public class PdfDataLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadPdfRequest, TRecord> recordFactory,
    IVisionService visionService,
    IModelsService modelsService,
    ILogger<PdfDataLoaderService<TKey, TRecord>> logger) : IPdfDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// Represents the MIME type for PNG images.
    /// </summary>
    /// <remarks>
    /// This constant is used when processing image content, specifically for
    /// identifying PNG image data in the context of text extraction or related operations.
    /// </remarks>
    private const string PngMimeType = "image/png";
    
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Static encoder instance used for token counting tasks within the PdfDataLoaderService.
    /// </summary>
    /// <remarks>
    /// TokenCounter leverages the default encoding provided by the DocumentChunker to calculate
    /// the number of tokens in a given text. This is primarily used for determining token counts
    /// during PDF text processing and subsequent operations in the service.
    /// </remarks>
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Represents a utility for chunking text into smaller segments based on token limits.
    /// </summary>
    /// <remarks>
    /// This class is designed to split text data into manageable pieces by specifying
    /// the number of tokens per paragraph and per line. It is primarily used in text processing
    /// pipelines where handling large text inputs in smaller chunks is required.
    /// </remarks>
    /// <param name="tokensPerParagraph">The maximum number of tokens allowed per paragraph.</param>
    /// <param name="tokensPerLine">The maximum number of tokens allowed per line.</param>
    private static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Loads a PDF file, processes its contents, and stores them into a vector store collection.
    /// </summary>
    /// <param name="request">The request containing details about the PDF file, including its local path and processing parameters.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadPdfAsync(LoadPdfRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.PdfLocalPath))
            throw new InvalidOperationException("PdfPath is empty");

        // Create the collection if it doesn't exist.
        await vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
        
        // First delete any existing PDFs with same name
        var toDelete = await vectorStoreRecordCollection.GetAsync(
                filter: data => true,
                int.MaxValue, // this is dumb 
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(request.PdfFileName!.TrimStart("file://"))).ToList();

        if (toDelete.Count > 0)
            await vectorStoreRecordCollection.DeleteAsync(
                toDelete.Select(td => td.Key), cancellationToken);

        // Load the text and images from the PDF file and split them into batches.
        var pages = LoadTextAndImages(request.PdfLocalPath, cancellationToken);
        
        var batches = pages.Chunk(request.BatchSize);

        // Process each batch of content items.
        foreach (var page in batches)
        {
            // Convert any images to text.
            var extractTextTasks = page.Select(async content =>
            {
                if (content.Text != null)
                {
                    return content;
                }

                var textFromImage = await ConvertImageToTextWithRetryAsync(
                    content.Image!.Value,
                    cancellationToken).ConfigureAwait(false);
                
                return new RawContent { Text = textFromImage, PageNumber = content.PageNumber };
            });
            var rawTextContents = await Task.WhenAll(extractTextTasks).ConfigureAwait(false);
            
            // now need to break all the pages into smaller chunks with overlap to preserve mean context
            var chunkingTasks = rawTextContents.Select(rawTextContent => 
                Task.Run(() =>
                {
                    var chunkHeader = $"Page: {rawTextContent.PageNumber}\n";
                    logger.LogDebug("Created Chunk: {ChunkHeader}",chunkHeader);
                    var textChunks = DocumentChunker.ChunkText(rawTextContent.Text!, chunkHeader);
                    return textChunks.Select(textChunk => new RawContent
                    {
                        Text = textChunk,
                        PageNumber = rawTextContent.PageNumber
                    });
                }, cancellationToken)
            );
            var chunkedResults = await Task.WhenAll(chunkingTasks).ConfigureAwait(false);
            var textContents = chunkedResults.SelectMany(x => x);
            
            // Map each paragraph to a TextSnippet and generate an embedding for it.
            var recordTasks = textContents.Select(async content =>
            {
                var record = recordFactory(content, request);

                record.Key = uniqueKeyGenerator.GenerateKey();

                record.Text ??= content.Text;
                record.ReferenceDescription ??= $"{request.PdfFileName!.TrimStart("file://")}#page={content.PageNumber}";
                record.ReferenceLink ??=
                    $"{new Uri($"file://{request.PdfFileName!.TrimStart("file://")}").AbsoluteUri}#page={content.PageNumber}";
                record.TextEmbedding ??= await GenerateEmbeddingsWithRetryAsync(content.Text!, cancellationToken);

                record.TokenCount ??= TokenCounter.CountTokens(record.Text!);
                
                return record;
            });

            var records = await Task.WhenAll(recordTasks).ConfigureAwait(false);

            // Upsert the records into the vector store.
            await vectorStoreRecordCollection
                .UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.Delay(request.BetweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }

        await modelsService.UnloadVisionModelAsync();
        await modelsService.UnloadEmbeddingModelAsync();
    }

    /// <summary>
    /// Adds a retry mechanism to the process of generating embeddings.
    /// </summary>
    /// <param name="text">The input text for which the embedding is to be generated.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, returning the generated embedding as an <see cref="Embedding{T}"/>.</returns>
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
                    logger.LogWarning($"Failed to generate embedding. Error: {ex}");
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
    /// Reads the text and images from each page in the provided PDF file.
    /// </summary>
    /// <param name="pdfPath">The path to the PDF file to extract text and images from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A collection of <see cref="RawContent"/> containing the text, images, and associated page numbers from the PDF file.</returns>
    private IEnumerable<RawContent> LoadTextAndImages(string pdfPath, CancellationToken cancellationToken)
    {
        var rawContents = new List<RawContent>();

        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var images = page.GetImages().ToList();
            logger.LogDebug("Page {PageNumber}: Found {ImageCount} images", page.Number, images.Count());
            
            foreach (var image in images.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
            {
                // Log detailed image information
                logger.LogDebug("Image details - Width: {Width}, Height: {Height}, BitsPerComponent: {BitsPerComponent}, ColorSpace: {ColorSpace}", 
                    image.Bounds.Width, image.Bounds.Height, image.BitsPerComponent, image.ColorSpaceDetails?.Type);

                // Try to get image bounds and other properties
                logger.LogDebug("Image bounds: {Bounds}", image.Bounds);

                ReadOnlyMemory<byte>? imageBytes;
            
                // Try different image formats in order of preference
                if (image.TryGetPng(out var png))
                {
                    imageBytes = png;
                }
                else if (image.TryGetBytesAsMemory(out var rawBytes))
                {
                    // Convert raw bytes to PNG using System.Drawing or ImageSharp
                    imageBytes = ConvertToPng(rawBytes);
                }
                else
                {
                    imageBytes = image.RawMemory;
                }

                if (imageBytes.HasValue)
                {
                    rawContents.Add(new RawContent { Image = imageBytes.Value, PageNumber = page.Number });
                }
            }
            
            var pageSegmenter = DefaultPageSegmenter.Instance;

            var blocks = pageSegmenter.GetBlocks(page.GetWords());
            
            logger.LogDebug("Page {PageNumber}: Found {BlockCount} text blocks", page.Number, blocks.Count);

            rawContents.AddRange(blocks.TakeWhile(_ => !cancellationToken.IsCancellationRequested)
                .Select(block => new RawContent { Text = block.Text, PageNumber = page.Number }));
        }
        
        return rawContents;
    }

    /// <summary>
    /// Adds a simple retry mechanism for converting an image to text.
    /// </summary>
    /// <param name="imageBytes">The image data to be processed for text extraction.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, producing the extracted text from the image.</returns>
    private async Task<string> ConvertImageToTextWithRetryAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                return await visionService.GetImageTextAsync(imageBytes, PngMimeType, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    logger.LogWarning("Failed to generate text from image. Error: {HttpOperationException}", ex);
                    logger.LogWarning("Retrying text to image conversion...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogError("Failed to generate text from image. Error: {HttpOperationException}", ex);
                    
                    throw;
                }
            }
            catch (ClientResultException ex)
            {
                logger.LogError("Failed to generate text from image. Error: {HttpOperationException}", 
                    ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());
                
                throw;
            }
        }
    }

    /// <summary>
    /// Converts a given image in raw byte format to PNG format.
    /// </summary>
    /// <param name="imageBytes">The raw byte array of the input image.</param>
    /// <returns>A readonly memory containing the PNG-formatted image bytes.</returns>
    /// <exception cref="ApplicationException">Thrown when the conversion to PNG format fails.</exception>
    private ReadOnlyMemory<byte> ConvertToPng(ReadOnlyMemory<byte> imageBytes)
    {
        try
        {
            using var inputStream = new MemoryStream(imageBytes.ToArray());
            using var outputStream = new MemoryStream();
        
            using var image = Image.Load(inputStream);
            image.Save(outputStream, new PngEncoder());
        
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to convert image to PNG format");
            throw new ApplicationException("Failed to convert image to PNG format", ex);
        }
    }

}
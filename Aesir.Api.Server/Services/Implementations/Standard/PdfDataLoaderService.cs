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
/// Provides PDF data loading services for extracting and storing text content from PDF files.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify records.</typeparam>
/// <typeparam name="TRecord">The type of the text data record.</typeparam>
/// <param name="uniqueKeyGenerator">The key generator for creating unique identifiers.</param>
/// <param name="vectorStoreRecordCollection">The vector store collection for storing text records.</param>
// NOTE: REFACTOR SOON... inject in to document collection services
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
    private const string PngMimeType = "image/png";
    
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);
    // ReSharper disable once StaticMemberInGenericType
    private static readonly DocumentChunker DocumentChunker = new(250, 50);
    
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
    /// Add a simple retry mechanism to embedding generation.
    /// </summary>
    /// <param name="text">The text to generate the embedding for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated embedding.</returns>
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
                    Dimensions = 768
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
                    logger.LogInformation($"Failed to generate embedding. Error: {ex}");
                    logger.LogInformation("Retrying embedding generation...");
                    await Task.Delay(10_000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Read the text and images from each page in the provided PDF file.
    /// </summary>
    /// <param name="pdfPath">The pdf file to read the text and images from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The text and images from the pdf file, plus the page number that each is on.</returns>
    private IEnumerable<RawContent> LoadTextAndImages(string pdfPath, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var images = page.GetImages().ToList();
            logger.LogDebug("Page {PageNumber}: Found {ImageCount} images", page.Number, images.Count());
            
            foreach (var image in images)
            {
                // Log detailed image information
                logger.LogDebug("Image details - Width: {Width}, Height: {Height}, BitsPerComponent: {BitsPerComponent}, ColorSpace: {ColorSpace}", 
                    image.Bounds.Width, image.Bounds.Height, image.BitsPerComponent, image.ColorSpaceDetails?.Type);

                // Try to get image bounds and other properties
                logger.LogDebug("Image bounds: {Bounds}", image.Bounds);

                ReadOnlyMemory<byte>? imageBytes = null;
            
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
                    yield return new RawContent { Image = imageBytes.Value, PageNumber = page.Number };
                }
            }
            
            var pageSegmenter = DefaultPageSegmenter.Instance;

            var blocks = pageSegmenter.GetBlocks(page.GetWords());
            
            logger.LogDebug("Page {PageNumber}: Found {BlockCount} text blocks", page.Number, blocks.Count());
            
            foreach (var block in blocks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return new RawContent { Text = block.Text, PageNumber = page.Number };
            }
        }
    }

    /// <summary>
    /// Add a simple retry mechanism to image to text.
    /// </summary>
    /// <param name="imageBytes">The image to generate the text for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated text.</returns>
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

public sealed class RawContent
{
    public string? Text { get; init; }

    public ReadOnlyMemory<byte>? Image { get; init; }

    public int PageNumber { get; init; }
}

public class UniqueKeyGenerator<TKey>(Func<TKey> generator)
    where TKey : notnull
{
    /// <summary>
    /// Generate a unique key.
    /// </summary>
    /// <returns>The unique key that was generated.</returns>
    public TKey GenerateKey() => generator();
}
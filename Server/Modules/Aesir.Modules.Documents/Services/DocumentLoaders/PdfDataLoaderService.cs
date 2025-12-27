using System.ClientModel;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Extensions;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aesir.Modules.Documents.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using LoadPdfRequest = Aesir.Modules.Documents.Models.LoadPdfRequest;
using RawContent = Aesir.Modules.Documents.Models.RawContent;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

/// <summary>
/// Provides functionality specific to loading, extracting, and processing PDF documents.
/// The service utilizes a unique key generator, embedding generation, and a vision service
/// to handle both the textual and visual content from PDF files, storing the resulting data in a vector store.
/// </summary>
/// <typeparam name="TKey">The type used for identifying records uniquely.</typeparam>
/// <typeparam name="TRecord">The data type representing a single record of processed text data.</typeparam>
/// <param name="uniqueKeyGenerator">Service for generating unique keys associated with processed records.</param>
/// <param name="vectorStoreRecordCollection">The collection used to store vectorized data for search and retrieval.</param>
/// <param name="embeddingGenerator">Service for generating embeddings from provided text content.</param>
/// <param name="recordFactory">A factory function used to create a record from raw content and load requests.</param>
/// <param name="configurationService">Service for loading configuration.</param>
/// <param name="serviceProvider">Service locator.</param>
/// <param name="embeddingCache">Cache for storing generated embeddings.</param>
/// <param name="logger">Logger instance for diagnostic or operational logging purposes.</param>
[Experimental("SKEXP0001")]
public class PdfDataLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadPdfRequest, TRecord> recordFactory,
    IConfigurationService configurationService,
    IServiceProvider serviceProvider,
    IEmbeddingCache embeddingCache,
    ILogger<PdfDataLoaderService<TKey, TRecord>> logger)
    : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator,
        configurationService, serviceProvider, embeddingCache, logger), IPdfDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// Represents the MIME type for PNG images, used to specify the content type of PNG image data.
    /// </summary>
    private const string PngMimeType = "image/png";

    /// <summary>
    /// A delegate used to create an instance of the <typeparamref name="TRecord"/> type,
    /// using the provided <see cref="RawContent"/> and <see cref="LoadPdfRequest"/> data.
    /// </summary>
    private readonly Func<RawContent, LoadPdfRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Loads a PDF file, processes its contents, and stores the parsed data into a vector store collection.
    /// </summary>
    /// <param name="request">An object containing the details about the PDF to be loaded, such as its local path and processing instructions.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to observe cancellation requests during the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task LoadPdfAsync(LoadPdfRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.PdfLocalPath))
            throw new InvalidOperationException("PdfPath is empty");

        if (request.ModelLocation == null)
            throw new InvalidOperationException("ModelLocation is empty");

        await InitializeCollectionAsync(cancellationToken);
        await DeleteExistingRecordsAsync(request.PdfFileName!, cancellationToken);

        // Load the text and images from the PDF file and split them into batches.
        var pages = LoadTextAndImages(request.PdfLocalPath, cancellationToken);
        var batches = pages.Chunk(request.BatchSize).ToList();

        logger.LogDebug("Processing {BatchCount} batches with max {MaxConcurrent} concurrent operations",
            batches.Count, request.MaxConcurrentBatches);

        // Rate-limited parallel processing using SemaphoreSlim
        var rateLimiter = new SemaphoreSlim(request.MaxConcurrentBatches);
        var allRecords = new ConcurrentBag<TRecord[]>();

        var batchTasks = batches.Select(async batch =>
        {
            await rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var records = await ProcessBatchAsync(batch, request, cancellationToken).ConfigureAwait(false);
                allRecords.Add(records);
            }
            finally
            {
                rateLimiter.Release();
            }
        });

        await Task.WhenAll(batchTasks).ConfigureAwait(false);

        // Upsert all records at once
        var flattenedRecords = allRecords.SelectMany(r => r).ToArray();
        if (flattenedRecords.Length > 0)
        {
            await UpsertRecordsAsync(flattenedRecords, cancellationToken);
        }

        await UnloadModelsAsync();
    }

    /// <summary>
    /// Processes a batch of raw content items by converting images to text, chunking, and generating embeddings.
    /// </summary>
    /// <param name="batch">The batch of raw content items to process.</param>
    /// <param name="request">The load PDF request containing configuration.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An array of processed records with embeddings.</returns>
    private async Task<TRecord[]> ProcessBatchAsync(
        RawContent[] batch,
        LoadPdfRequest request,
        CancellationToken cancellationToken)
    {
        // Separate text and image content
        var textContentsFromBatch = batch.Where(c => c.Text != null).ToList();
        var imageContents = batch.Where(c => c.Image.HasValue).ToList();

        // Process images using batch vision API
        var convertedImageContents = new List<RawContent>();
        if (imageContents.Count > 0)
        {
            var visionService = ServiceProvider.GetKeyedService<IVisionService>(request.ModelLocation!.InterfaceEngineId)
                ?? throw new InvalidOperationException(
                    $"Failed to get Vision service for engine '{request.ModelLocation.InterfaceEngineId}'. " +
                    "Ensure the inference engine is registered and supports vision operations.");

            // Process images in optimal batch sizes for the provider
            for (var i = 0; i < imageContents.Count; i += visionService.MaxBatchSize)
            {
                var imageBatch = imageContents
                    .Skip(i)
                    .Take(visionService.MaxBatchSize)
                    .Select(c => (ConvertToPng(c.Image!.Value), PngMimeType))
                    .ToList();

                var extractedTexts = await visionService.GetImageTextBatchAsync(
                    request.ModelLocation!,
                    imageBatch,
                    cancellationToken).ConfigureAwait(false);

                for (var j = 0; j < extractedTexts.Length; j++)
                {
                    convertedImageContents.Add(new RawContent
                    {
                        Text = extractedTexts[j],
                        PageNumber = imageContents[i + j].PageNumber
                    });
                }
            }
        }

        var rawTextContents = textContentsFromBatch.Concat(convertedImageContents).ToArray();

        // Break content into smaller chunks with headers
        var textContents = rawTextContents.SelectMany(rawTextContent =>
        {
            var chunkHeader = $"File: {request.PdfFileName}\nPage: {rawTextContent.PageNumber}\n";
            logger.LogDebug("Created Chunk: {ChunkHeader}", chunkHeader);
            var textChunks = DocumentChunker.ChunkText(rawTextContent.Text!, chunkHeader);
            return textChunks.Select(textChunk => new RawContent
            {
                Text = textChunk,
                PageNumber = rawTextContent.PageNumber
            });
        }).ToList();

        // Create records from content
        var records = textContents.Select(content =>
        {
            var record = _recordFactory(content, request);
            record.Text ??= content.Text;
            record.ReferenceDescription ??=
                $"{request.PdfFileName!.TrimStart("file://")}#page={content.PageNumber}";
            record.ReferenceLink ??=
                $"{new Uri($"file://{request.PdfFileName!.TrimStart("file://")}").AbsoluteUri}#page={content.PageNumber}";
            return record;
        }).ToArray();

        // Process records to generate embeddings
        return await ProcessRecordsInBatchesAsync(records, request.PdfFileName!, request.BatchSize, cancellationToken);
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
                logger.LogDebug(
                    "Image details - Width: {Width}, Height: {Height}, BitsPerComponent: {BitsPerComponent}, ColorSpace: {ColorSpace}",
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
    /// <param name="modelLocationDescriptor">The location of the model to use for vision service operations.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, producing the extracted text from the image.</returns>
    private async Task<string> ConvertImageToTextAsync(
        ReadOnlyMemory<byte> imageBytes,
        ModelLocationDescriptor modelLocationDescriptor,
        CancellationToken cancellationToken)
    {
        try
        {
            // Resolve vision service using the engine ID from configuration
            var visionService = ServiceProvider.GetKeyedService<IVisionService>(modelLocationDescriptor.InterfaceEngineId)
                ?? throw new InvalidOperationException(
                    $"Failed to get Vision service for engine '{modelLocationDescriptor.InterfaceEngineId}'. " +
                    "Ensure the inference engine is registered and supports vision operations.");

            using var image = Image.Load(imageBytes.Span);
            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms, cancellationToken);
            var resizedImageBytes = new ReadOnlyMemory<byte>(ms.ToArray());

            return await visionService.GetImageTextAsync(modelLocationDescriptor, resizedImageBytes,
                    PngMimeType, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ClientResultException ex)
        {
            logger.LogError("Failed to generate text from image. Error: {HttpOperationException}",
                ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());

            throw;
        }
    }

    /// <summary>
    /// Converts a given image from its raw byte array format to PNG format.
    /// </summary>
    /// <param name="imageBytes">The raw byte array representing the input image.</param>
    /// <returns>A <see cref="ReadOnlyMemory{byte}"/> containing the PNG-formatted image bytes.</returns>
    /// <exception cref="ApplicationException">Thrown when the conversion process to PNG format fails.</exception>
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
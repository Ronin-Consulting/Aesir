using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Aesir.Api.Server.Services.Implementations.Standard;

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
/// <param name="visionService">Service capable of extracting and processing visual content within PDFs.</param>
/// <param name="modelsService">Service for managing AI models necessary for textual and visual analysis.</param>
/// <param name="logger">Logger instance for diagnostic or operational logging purposes.</param>
[Experimental("SKEXP0001")]
public class PdfDataLoaderService<TKey, TRecord>(
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    Func<RawContent, LoadPdfRequest, TRecord> recordFactory,
    IVisionService visionService,
    IModelsService modelsService,
    ILogger<PdfDataLoaderService<TKey, TRecord>> logger)
    : BaseDataLoaderService<TKey, TRecord>(uniqueKeyGenerator, vectorStoreRecordCollection, embeddingGenerator,
        modelsService, logger), IPdfDataLoaderService<TKey, TRecord>
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
    /// Represents the vision service utilized for image processing tasks such as extracting text from images
    /// within the <c>PdfDataLoaderService</c>.
    /// </summary>
    private readonly IVisionService _visionService = visionService;

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

        await InitializeCollectionAsync(cancellationToken);
        await DeleteExistingRecordsAsync(request.PdfFileName!, cancellationToken);

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

                var textFromImage = await ConvertImageToTextAsync(
                    content.Image!.Value,
                    cancellationToken).ConfigureAwait(false);

                return new RawContent { Text = textFromImage, PageNumber = content.PageNumber };
            });
            var rawTextContents = await Task.WhenAll(extractTextTasks).ConfigureAwait(false);

            // now need to break all the pages into smaller chunks with overlap to preserve mean context
            var chunkingTasks = rawTextContents.Select(rawTextContent =>
                Task.Run(() =>
                {
                    var chunkHeader = $"File: {request.PdfFileName}\nPage: {rawTextContent.PageNumber}\n";
                    logger.LogDebug("Created Chunk: {ChunkHeader}", chunkHeader);
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

            // Create records from content and process them
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

            var processedRecords =
                await ProcessRecordsInBatchesAsync(records, request.PdfFileName!, request.BatchSize, cancellationToken);
            
            await UpsertRecordsAsync(processedRecords, cancellationToken);

            await Task.Delay(request.BetweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }

        await UnloadModelsAsync();
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
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, producing the extracted text from the image.</returns>
    private async Task<string> ConvertImageToTextAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            using var image = Image.Load(imageBytes.Span);
            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms, cancellationToken);
            var resizedImageBytes = new ReadOnlyMemory<byte>(ms.ToArray());

            return await _visionService.GetImageTextAsync(resizedImageBytes, PngMimeType, cancellationToken)
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
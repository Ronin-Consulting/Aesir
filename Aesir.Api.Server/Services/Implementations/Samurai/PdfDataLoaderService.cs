using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aspose.Pdf;
using Aspose.Pdf.Drawing;
using Aspose.Pdf.Facades;
using Aspose.Pdf.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Path = System.IO.Path;

namespace Aesir.Api.Server.Services.Implementations.Samurai;

/// <summary>
/// Provides services for loading and processing data from PDF documents, including text extraction,
/// embedding generation, and saving the processed data into a vector store with metadata.
/// </summary>
/// <typeparam name="TKey">The type of the unique identifier for stored records.</typeparam>
/// <typeparam name="TRecord">The type representing the structured text data to be stored.</typeparam>
/// <param name="uniqueKeyGenerator">Service to generate unique identifiers for each record.</param>
/// <param name="vectorStoreRecordCollection">Collection handling storage and retrieval of vectorized records.</param>
/// <param name="embeddingGenerator">Generator responsible for producing embeddings from text data.</param>
/// <param name="recordFactory">Factory function for creating instances of TRecord using raw content and request parameters.</param>
/// <param name="visionService">Service for performing image processing tasks like OCR within PDFs.</param>
/// <param name="modelsService">Service providing AI models for text analysis and additional transformations.</param>
/// <param name="logger">Logger instance for recording events and debugging information during operations.</param>
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
    /// Represents the MIME type for PNG image files as a constant string.
    /// </summary>
    /// <remarks>
    /// The value of this constant is "image/png". It is primarily used to specify the MIME type
    /// when working with PNG image data, such as during image processing or converting images
    /// for specific use cases requiring this MIME type.
    /// </remarks>
    private const string PngMimeType = "image/png";

    /// <summary>
    /// A delegate responsible for generating a record instance of type <typeparamref name="TRecord"/>
    /// based on the provided <see cref="RawContent"/> and <see cref="LoadPdfRequest"/> input parameters.
    /// </summary>
    /// <remarks>
    /// This delegate enables the customization of how a record is created during the processing of PDF data.
    /// </remarks>
    private readonly Func<RawContent, LoadPdfRequest, TRecord> _recordFactory = recordFactory;

    /// <summary>
    /// Represents the vision service used for processing and analyzing image data.
    /// This service provides functionality for extracting text or other relevant information
    /// from image-based input. It is utilized in scenarios where translation of visual data
    /// into textual or structured formats is required.
    /// </summary>
    private readonly IVisionService _visionService = visionService;

    /// <summary>
    /// Loads a PDF document, processes its contents, and stores the data into a vector store collection.
    /// </summary>
    /// <param name="request">An object containing details related to the processing of the PDF file, such as its path, file name, and batch size.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation notifications.</param>
    /// <returns>A task that represents the asynchronous operation of loading and processing the PDF document.</returns>
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
                var cleanFileName = request.PdfFileName!.StartsWith("file://")
                    ? request.PdfFileName.Substring(7)
                    : request.PdfFileName;
                var fileUri = request.PdfFileName!.StartsWith("file://")
                    ? request.PdfFileName
                    : $"file://{request.PdfFileName}";
                record.ReferenceDescription ??= $"{cleanFileName}#page={content.PageNumber}";
                record.ReferenceLink ??= $"{fileUri}#page={content.PageNumber}";
                return record;
            }).ToArray();

            var processedRecords =
                await ProcessRecordsInBatchesAsync(records, request.PdfFileName!, records.Length, cancellationToken);
            await UpsertRecordsAsync(processedRecords, cancellationToken);

            await Task.Delay(request.BetweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
        }

        await UnloadModelsAsync();
    }


    /// <summary>
    /// Extracts text and image data from a PDF file, processes its pages concurrently,
    /// and returns the result as a collection of raw content.
    /// </summary>
    /// <param name="pdfPath">The file path of the PDF document to be processed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A collection of <see cref="RawContent"/> instances representing the extracted text and image data.</returns>
    private IEnumerable<RawContent> LoadTextAndImages(string pdfPath, CancellationToken cancellationToken = default)
    {
        //Aspose.PDFfor.NET.lic
        var license = new License();
        license.SetLicense("Aspose.PDFfor.NET.lic");

        // Using a ConcurrentDictionary to store the results of each page in a thread-safe way.
        // The key is the page number, which allows us to reassemble the results in order later.
        var pageResults = new ConcurrentDictionary<int, List<RawContent>>();

        var pageCount = 0;
        using (var document = new Document(pdfPath))
        {
            pageCount = document.Pages.Count;
        }

        Parallel.For(1, pageCount + 1, pageNum =>
        {
            // A local list to hold the content (text and images) for the current page.
            var currentPageContents = new List<RawContent>();
            var parentDirectory = Path.GetDirectoryName(pdfPath) ?? string.Empty;

            var pageFile =
                Path.Combine(parentDirectory,
                    $"{Path.GetFileNameWithoutExtension(pdfPath)}_{pageNum}{Path.GetExtension(pdfPath)}"
                );
            var pdfFileEditor = new PdfFileEditor();
            pdfFileEditor.Extract(pdfPath, [pageNum], pageFile);

            // ReSharper disable once ConvertToUsingDeclaration
            using (var pageDocument = new Document(pageFile))
            {
                var imageFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                var mdFilename = Path.Combine(parentDirectory, $"{Path.GetFileNameWithoutExtension(pageFile)}.md");

                var hasImages = pageDocument.Pages.First().Resources.Images.Count > 0;

                var textSearchOptions = new TextSearchOptions(true)
                {
                    IgnoreResourceFontErrors = true,
                    LogTextExtractionErrors = true,
                    UseFontEngineEncoding = true
                };
                var textAbsorber = new TextAbsorber(textSearchOptions);
                pageDocument.Pages.First().Accept(textAbsorber);

                var hasText = !string.IsNullOrWhiteSpace(textAbsorber.Text);

                if (!hasText && hasImages)
                {
                    var page = pageDocument.Pages.First();
                    if (page.Resources.Images.Count <= 0) return;

                    foreach (var image in page.Resources.Images)
                    {
                        using var ms = new MemoryStream();
                        image.Save(ms, ImageFormat.Png);
                        currentPageContents.Add(new RawContent
                        {
                            PageNumber = pageNum,
                            Image = ms.ToArray()
                        });
                    }
                }
                else
                {
                    var saveOptions = new MarkdownSaveOptions
                    {
                        UseImageHtmlTag = false,
                        ResourcesDirectoryName = imageFolder
                    };

                    pageDocument.Save(mdFilename, saveOptions);

                    currentPageContents.Add(new RawContent
                    {
                        PageNumber = pageNum,
                        Text = File.ReadAllText(mdFilename)
                    });

                    var imageFiles = Directory.EnumerateFiles(imageFolder);
                    currentPageContents.AddRange(
                        imageFiles.Select(imageFile =>
                            new RawContent { PageNumber = pageNum, Image = File.ReadAllBytes(imageFile) }
                        )
                    );

                    DeleteDirectoryQuietly(imageFolder);
                    DeleteFileQuietly(mdFilename);
                }
            }

            DeleteFileQuietly(pageFile);

            // Add the list of contents for the current page to the concurrent dictionary.
            pageResults[pageNum] = currentPageContents;
        });

        // After the parallel loop, create the final ordered list.
        // We sort the results by page number (the dictionary key) and then flatten the lists into one.
        var rawContents = pageResults
            .OrderBy(kvp => kvp.Key)
            .SelectMany(kvp => kvp.Value)
            .ToList();

        return rawContents;

        void DeleteDirectoryQuietly(string directoryPath)
        {
            try
            {
                Directory.Delete(directoryPath, true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete directory: {DirectoryPath}", directoryPath);
            }
        }

        void DeleteFileQuietly(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
            }
        }
    }

    /// <summary>
    /// Converts an image into text with a retry mechanism using OCR or vision services.
    /// </summary>
    /// <param name="imageBytes">The byte data of the image to process.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, yielding the extracted text from the image.</returns>
    private async Task<string> ConvertImageToTextAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        // Resize the image to a resolution that works well with the vision model
        using var image = Image.Load(imageBytes.Span);
        image.Mutate(x =>
            x.Resize(new ResizeOptions
            {
                Size = new Size(1024, 1024),
                Mode = ResizeMode.Max
            }));

        using var ms = new MemoryStream();
        await image.SaveAsPngAsync(ms, cancellationToken);
        var resizedImageBytes = new ReadOnlyMemory<byte>(ms.ToArray());

        return await _visionService.GetImageTextAsync(resizedImageBytes, PngMimeType, cancellationToken)
            .ConfigureAwait(false);
    }
}
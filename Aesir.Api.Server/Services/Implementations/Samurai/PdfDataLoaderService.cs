using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Aspose.Pdf;
using Aspose.Pdf.Drawing;
using Aspose.Pdf.Facades;
using Aspose.Pdf.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Tiktoken;
using Image = SixLabors.ImageSharp.Image;
using Path = System.IO.Path;

namespace Aesir.Api.Server.Services.Implementations.Samurai;

/// <summary>
/// Provides functionality for extracting text content from PDF files, generating embeddings,
/// and storing the extracted text along with associated metadata in a vector store.
/// </summary>
/// <typeparam name="TKey">The type of the unique key used to identify stored records.</typeparam>
/// <typeparam name="TRecord">The type of the text data record to be stored.</typeparam>
/// <param name="uniqueKeyGenerator">The service responsible for generating unique keys for records.</param>
/// <param name="vectorStoreRecordCollection">The collection responsible for storing text records in a vectorized format.</param>
/// <param name="embeddingGenerator">The generator used to create embeddings from the extracted text.</param>
/// <param name="recordFactory">A factory function for creating instances of TRecord using raw content and request parameters.</param>
/// <param name="visionService">The service used for handling vision-related tasks, such as OCR on PDF images.</param>
/// <param name="modelsService">The service providing access to AI models for text processing tasks.</param>
/// <param name="logger">The logger instance used for logging operations and events in the data loading process.</param>
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
    /// Represents the MIME type for PNG image files.
    /// </summary>
    /// <remarks>
    /// This constant is used to specify the MIME type "image/png" when handling PNG image data,
    /// particularly for scenarios involving image processing or passing image data to external services.
    /// </remarks>
    private const string PngMimeType = "image/png";
    
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// A static readonly instance of the <see cref="Encoder"/> class used to count tokens in a string of text.
    /// </summary>
    /// <remarks>
    /// The <c>TokenCounter</c> leverages the default encoding provided by <see cref="DocumentChunker.DefaultEncoding"/>.
    /// It is utilized for counting the number of tokens in text data within the PDF processing workflow.
    /// </remarks>
    private static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);
    // ReSharper disable once StaticMemberInGenericType
    /// <summary>
    /// Provides functionality for chunking text into smaller segments based on specified token limits.
    /// Designed for use in applications where text needs to be divided into manageable pieces,
    /// such as processing large documents or preparing data for machine learning models.
    /// </summary>
    /// <remarks>
    /// The chunking process divides text into paragraphs and lines, with a maximum number of tokens
    /// per paragraph and tokens per line. This is useful for efficiently handling large textual data,
    /// while maintaining contextual integrity of the content.
    /// </remarks>
    private static readonly DocumentChunker DocumentChunker = new(250, 50);

    /// <summary>
    /// Loads a PDF document, processes its contents, and stores the data into a vector store collection.
    /// </summary>
    /// <param name="request">An object containing details about the PDF file to process, including its local path, file name, and other parameters.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadPdfAsync(LoadPdfRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.PdfLocalPath))
            throw new InvalidOperationException("PdfPath is empty");

        // Create the collection if it doesn't exist.
        await vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
        
        // First delete any existing PDFs with same name
        var toDelete = await vectorStoreRecordCollection.GetAsync(
                filter: data => data.Text != null,// dumb because contains is not supported across vectorstore providers
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
    /// Reads the text and images from each page in the provided PDF file.
    /// </summary>
    /// <param name="pdfPath">The path to the PDF file to extract text and images from.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to monitor for cancellation requests.</param>
    /// <returns>A collection of <see cref="RawContent"/> objects containing the extracted text, images, and associated page numbers.</returns>
    private IEnumerable<RawContent> LoadTextAndImages(string pdfPath, CancellationToken cancellationToken)
    {
        //Aspose.PDFfor.NET.lic
        var license = new License();
        license.SetLicense("Aspose.PDFfor.NET.lic");

        var rawContents = new List<RawContent>();

        // ReSharper disable once ConvertToUsingDeclaration
        using (var document = new Document(pdfPath))
        {
            var pdfFileEditor = new PdfFileEditor();

            for (var pageNum = 1; pageNum <= document.Pages.Count; pageNum++)
            {
                var parentDirectory = Path.GetDirectoryName(pdfPath) ?? string.Empty;
                
                var pageFile = 
                    Path.Combine(parentDirectory, 
                        $"{Path.GetFileNameWithoutExtension(pdfPath)}_{pageNum}{Path.GetExtension(pdfPath)}"
                    );
                
                pdfFileEditor.Extract(pdfPath, [pageNum], pageFile);

                // ReSharper disable once ConvertToUsingDeclaration
                using (var pageDocument = new Document(pageFile))
                {
                    var imageFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    var mdFilename =  Path.Combine(parentDirectory, $"{Path.GetFileNameWithoutExtension(pageFile)}.md");
                    
                    var hasImages = pageDocument.Pages.First().Resources.Images.Count > 0;
                    
                    var textAbsorber = new TextAbsorber();
                    pageDocument.Pages.First().Accept(textAbsorber);
                    
                    var hasText = !string.IsNullOrWhiteSpace(textAbsorber.Text);
                    
                    if (!hasText && hasImages)
                    {
                        var page = pageDocument.Pages.First();
                        if (page.Resources.Images.Count <= 0) continue;
                        
                        foreach (var image in page.Resources.Images)
                        {
                            using var ms = new MemoryStream();
                            image.Save(ms, ImageFormat.Png);
                            rawContents.Add(new RawContent
                            {
                                PageNumber = pageNum,
                                Image = ms.ToArray()
                            });
                        }
                    }
                    else
                    {
                        // Create an instance of MarkdownSaveOptions to configure the Markdown export settings
                        var saveOptions = new MarkdownSaveOptions
                        {
                            // Set to false to prevent the use of HTML <img> tags for images in the Markdown output
                            UseImageHtmlTag = false,
                            // Specify the directory name where resources (like images) will be stored
                            ResourcesDirectoryName = imageFolder
                        };
                    
                        // Save PDF document in Markdown format to the specified output file path using the defined save options   
                        pageDocument.Save(mdFilename, saveOptions);
                    
                        // Now pull the markdown into the RawContent
                        rawContents.Add(new RawContent
                        {
                            PageNumber = pageNum,
                            Text = File.ReadAllText(mdFilename)
                        });
                    
                        // now load the images
                        var imageFiles = Directory.EnumerateFiles(imageFolder);
                        rawContents.AddRange(
                            imageFiles.Select(imageFile => 
                                new RawContent { PageNumber = pageNum, Image = File.ReadAllBytes(imageFile) }
                            )
                        );

                        DeleteDirectoryQuietly(imageFolder);
                        DeleteFileQuietly(mdFilename);  
                    }
                }

                DeleteFileQuietly(pageFile);
            }
        }

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
    /// Adds a retry mechanism to convert an image into text using OCR or other vision services.
    /// </summary>
    /// <param name="imageBytes">The byte data of the image to process.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The extracted text from the image.</returns>
    private async Task<string> ConvertImageToTextWithRetryAsync(
        ReadOnlyMemory<byte> imageBytes,
        CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
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
                
                return await visionService.GetImageTextAsync(resizedImageBytes, PngMimeType, cancellationToken).ConfigureAwait(false);
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
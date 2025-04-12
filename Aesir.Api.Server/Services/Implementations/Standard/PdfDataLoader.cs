using System.Diagnostics.CodeAnalysis;
using System.Net;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Aesir.Api.Server.Services.Implementations.Standard;

[Experimental("SKEXP0001")]
public class PdfDataLoader<TKey> (
    UniqueKeyGenerator<TKey> uniqueKeyGenerator,
    IVectorStoreRecordCollection<TKey, AesirTextData<TKey>> vectorStoreRecordCollection,
    ITextEmbeddingGenerationService textEmbeddingGenerationService,
    IChatCompletionService chatCompletionService,
    ILogger<PdfDataLoader<TKey>> logger) : IPdfDataLoader where TKey : notnull
{
    /// <inheritdoc/>
    public async Task LoadPdf(string pdfPath, int batchSize, int betweenBatchDelayInMs, CancellationToken cancellationToken)
    {
        // Create the collection if it doesn't exist.
        await vectorStoreRecordCollection.CreateCollectionIfNotExistsAsync(cancellationToken).ConfigureAwait(false);

        // Load the text and images from the PDF file and split them into batches.
        var sections = LoadTextAndImages(pdfPath, cancellationToken);
        var batches = sections.Chunk(batchSize);

        // Process each batch of content items.
        foreach (var batch in batches)
        {
            // Convert any images to text.
            var textContentTasks = batch.Select(async content =>
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
            var textContent = await Task.WhenAll(textContentTasks).ConfigureAwait(false);

            // Map each paragraph to a TextSnippet and generate an embedding for it.
            var recordTasks = textContent.Select(async content => new AesirTextData<TKey>
            {
                Key = uniqueKeyGenerator.GenerateKey(),
                Text = content.Text,
                ReferenceDescription = $"{new FileInfo(pdfPath).Name}#page={content.PageNumber}",
                ReferenceLink = $"{new Uri(new FileInfo(pdfPath).FullName).AbsoluteUri}#page={content.PageNumber}",
                TextEmbedding = await GenerateEmbeddingsWithRetryAsync(content.Text!, cancellationToken: cancellationToken).ConfigureAwait(false)
            });

            // Upsert the records into the vector store.
            var records = await Task.WhenAll(recordTasks).ConfigureAwait(false);
            var upsertedKeys = vectorStoreRecordCollection.UpsertBatchAsync(records, cancellationToken: cancellationToken);
            await foreach (var key in upsertedKeys.ConfigureAwait(false))
            {
                logger.LogInformation($"Upserted record '{key}' into VectorDB");
            }

            await Task.Delay(betweenBatchDelayInMs, cancellationToken).ConfigureAwait(false);
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

            foreach (var image in page.GetImages())
            {
                if (image.TryGetPng(out var png))
                {
                    yield return new RawContent { Image = png, PageNumber = page.Number };
                }
                else
                {
                    logger.LogInformation($"Unsupported image format on page {page.Number}");
                }
            }

            var pageSegmenter = DefaultPageSegmenter.Instance;
            
            var blocks = pageSegmenter.GetBlocks(page.GetWords());
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
    /// Add a simple retry mechanism to embedding generation.
    /// </summary>
    /// <param name="text">The text to generate the embedding for.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated embedding.</returns>
    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingsWithRetryAsync(string text, CancellationToken cancellationToken)
    {
        var tries = 0;

        while (true)
        {
            try
            {
                return await textEmbeddingGenerationService.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken).ConfigureAwait(false);
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
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage([
                    new TextContent("What’s in this image?"),
                    new ImageContent(imageBytes, "image/png"),
                ]);
                var result = await chatCompletionService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken).ConfigureAwait(false);
                return string.Join("\n", result.Select(x => x.Content));
            }
            catch (HttpOperationException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                tries++;

                if (tries < 3)
                {
                    logger.LogInformation($"Failed to generate text from image. Error: {ex}");
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

    /// <summary>
    /// Private model for returning the content items from a PDF file.
    /// </summary>
    private sealed class RawContent
    {
        public string? Text { get; init; }

        public ReadOnlyMemory<byte>? Image { get; init; }

        public int PageNumber { get; init; }
    }
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
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Tiktoken;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Abstract base class for data loader services that provides a common framework
/// for processing records, generating text embeddings, and managing operations
/// with vector store collections. This class serves as a foundation for implementing
/// data-related workflows that involve embedding generation and vector persistence.
/// </summary>
/// <typeparam name="TKey">The type of the unique key identifier for records, requiring a non-null type.</typeparam>
/// <typeparam name="TRecord">The type of the data record, which must inherit from AesirTextData<TKey>.</typeparam>
[Experimental("SKEXP0001")]
public abstract class BaseDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    /// <summary>
    /// Instance used for generating unique keys of type <c>TKey</c>.
    /// </summary>
    protected readonly UniqueKeyGenerator<TKey> UniqueKeyGenerator;

    /// <summary>
    /// Represents a collection used for managing and interacting with vector-based records
    /// within the context of a vector store. Provides operations such as ensuring collection
    /// existence, retrieving, deleting, and upserting vector records.
    /// </summary>
    protected readonly VectorStoreCollection<TKey, TRecord> VectorStoreRecordCollection;

    /// <summary>
    /// Responsible for generating vector embeddings from textual data,
    /// leveraging implementations of the IEmbeddingGenerator interface.
    /// </summary>
    protected readonly IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator;

    /// <summary>
    /// Service responsible for managing and interacting with machine learning models,
    /// including operations such as loading, unloading, and model lifecycle management.
    /// </summary>
    protected readonly IModelsService ModelsService;

    /// <summary>
    /// Logger instance used for recording messages, errors, and other logging information
    /// during the execution of data loader service operations.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// A static encoder used to perform operations for counting tokens in text.
    /// </summary>
    protected static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);

    /// <summary>
    /// Utility for splitting text documents into manageable pieces based on token limits.
    /// </summary>
    protected static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Represents a base service for loading and processing data records with generic support for keys and records.
    /// </summary>
    /// <typeparam name="TKey">The type of the unique key used for records.</typeparam>
    /// <typeparam name="TRecord">The type of the records handled by the service.</typeparam>
    /// <param name="uniqueKeyGenerator">The generator responsible for creating unique keys.</param>
    /// <param name="vectorStoreRecordCollection">The collection used for storing and managing vectorized representations of records.</param>
    /// <param name="embeddingGenerator">The generator used to compute embeddings for data.</param>
    /// <param name="modelsService">The service handling AI/ML models for processing tasks.</param>
    /// <param name="logger">The logger used for logging operations within the service.</param>
    protected BaseDataLoaderService(
        UniqueKeyGenerator<TKey> uniqueKeyGenerator,
        VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IModelsService modelsService,
        ILogger logger)
    {
        UniqueKeyGenerator = uniqueKeyGenerator;
        VectorStoreRecordCollection = vectorStoreRecordCollection;
        EmbeddingGenerator = embeddingGenerator;
        ModelsService = modelsService;
        Logger = logger;
    }

    /// <summary>
    /// Processes records in batches, enriching each record with metadata and embeddings.
    /// </summary>
    /// <param name="records">The records to process.</param>
    /// <param name="fileName">The source file name for reference metadata.</param>
    /// <param name="batchSize">The number of records to process in each batch.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An array of processed records.</returns>
    protected async Task<TRecord[]> ProcessRecordsInBatchesAsync(
        IEnumerable<TRecord> records,
        string fileName,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var processedRecords = new List<TRecord>();
        var recordsArray = records.ToArray();

        for (var i = 0; i < recordsArray.Length; i += batchSize)
        {
            var batch = recordsArray.Skip(i).Take(batchSize);
            var recordTasks = batch.Select(async record =>
            {
                await EnrichRecordAsync(record, fileName, cancellationToken).ConfigureAwait(false);
                return record;
            }).ToArray();

            processedRecords.AddRange(await Task.WhenAll(recordTasks).ConfigureAwait(false));
        }

        return processedRecords.ToArray();
    }

    /// <summary>
    /// Enriches a single record with a unique key, metadata, text embeddings, and token count.
    /// </summary>
    /// <param name="record">The record to be enriched.</param>
    /// <param name="fileName">The source file name used as metadata for the record.</param>
    /// <param name="cancellationToken">Token used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task EnrichRecordAsync(TRecord record, string fileName, CancellationToken cancellationToken)
    {
        var textChunk = record.Text ?? throw new InvalidOperationException("Text is null");

        record.Key = UniqueKeyGenerator.GenerateKey();
        record.ReferenceDescription ??= fileName.StartsWith("file://") ? fileName.Substring(7) : fileName;
        record.ReferenceLink ??= fileName.StartsWith("file://") ? fileName : $"file://{fileName}";
        record.TextEmbedding ??= await GenerateEmbeddings(textChunk, cancellationToken).ConfigureAwait(false);
        record.TokenCount ??= TokenCounter.CountTokens(textChunk);
    }

    /// <summary>
    /// Generates embeddings for the given text using defined embedding generation logic.
    /// </summary>
    /// <param name="text">The text input for which the embedding vectors are to be generated.</param>
    /// <param name="cancellationToken">A token used to monitor for cancellation requests.</param>
    /// <returns>An embedding instance containing the generated vector representation of the text.</returns>
    /// <exception cref="ClientResultException">Thrown when the embedding generation operation encounters an error.</exception>
    protected async Task<Embedding<float>> GenerateEmbeddings(string text, CancellationToken cancellationToken)
    {
        try
        {
            var options = new EmbeddingGenerationOptions()
            {
                Dimensions =
                    1024 // this should either be configuration or a requirement that the embedding model supports
            };
            var vector = (await EmbeddingGenerator
                .GenerateAsync(text, options, cancellationToken: cancellationToken).ConfigureAwait(false)).Vector;

            return new Embedding<float>(vector);
        }
        catch (ClientResultException ex)
        {
            Logger.LogError("Failed to generate embedding. Error: {HttpOperationException}",
                ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());
            throw;
        }
    }

    /// <summary>
    /// Deletes existing records from the vector store that match the specified file name.
    /// </summary>
    /// <param name="fileName">The file name to match for deletion.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous deletion operation.</returns>
    protected async Task DeleteExistingRecordsAsync(string fileName, CancellationToken cancellationToken)
    {
        var toDelete = await VectorStoreRecordCollection.GetAsync(
                filter: data => data.Text != null,
                int.MaxValue,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        var cleanFileName = fileName.StartsWith("file://") ? fileName.Substring(7) : fileName;
        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(cleanFileName)).ToList();

        if (toDelete.Count > 0)
            await VectorStoreRecordCollection.DeleteAsync(
                toDelete.Select(td => td.Key), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures the vector store collection exists and unloads AI models to free resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected async Task InitializeCollectionAsync(CancellationToken cancellationToken)
    {
        await VectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unloads AI models to free system resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of unloading models.</returns>
    protected async Task UnloadModelsAsync()
    {
        await ModelsService.UnloadVisionModelAsync().ConfigureAwait(false);
        await ModelsService.UnloadEmbeddingModelAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Upserts the processed records into the vector store.
    /// </summary>
    /// <param name="records">The records to upsert.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous upsert operation.</returns>
    protected async Task UpsertRecordsAsync(TRecord[] records, CancellationToken cancellationToken)
    {
        await VectorStoreRecordCollection
            .UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
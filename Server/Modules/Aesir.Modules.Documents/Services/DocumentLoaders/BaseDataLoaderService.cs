using System.ClientModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Documents.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Tiktoken;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

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
    /// Service responsible for managing and providing access to configuration settings.
    /// </summary>
    protected readonly IConfigurationService ConfigurationService;
    
    /// <summary>
    /// Provides access to the underlying application service container, enabling retrieval
    /// and management of dependency-injected services.
    /// </summary>
    protected readonly IServiceProvider ServiceProvider;

    /// <summary>
    /// Logger instance used for recording messages, errors, and other logging information
    /// during the execution of data loader service operations.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Cache for storing generated embeddings to avoid redundant API calls.
    /// </summary>
    protected readonly IEmbeddingCache EmbeddingCache;

    /// <summary>
    /// A static encoder used to perform operations for counting tokens in text.
    /// </summary>
    protected static readonly Encoder TokenCounter = new(DocumentChunker.DefaultEncoding);

    /// <summary>
    /// Utility for splitting text documents into manageable pieces based on token limits.
    /// </summary>
    protected static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Maximum number of tokens allowed per chunk for the embedding model.
    /// Note: This is in CL100K tokens - actual model tokenizers may count differently.
    /// </summary>
    protected const int MaxEmbeddingTokens = 120;

    /// <summary>
    /// Represents a base service for loading and processing data records with generic support for keys and records.
    /// </summary>
    /// <typeparam name="TKey">The type of the unique key used for records.</typeparam>
    /// <typeparam name="TRecord">The type of the records handled by the service.</typeparam>
    /// <param name="uniqueKeyGenerator">The generator responsible for creating unique keys.</param>
    /// <param name="vectorStoreRecordCollection">The collection used for storing and managing vectorized representations of records.</param>
    /// <param name="embeddingGenerator">The generator used to compute embeddings for data.</param>
    /// <param name="configurationService">The service for accessing configuration settings.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="embeddingCache">The cache for storing generated embeddings.</param>
    /// <param name="logger">The logger used for logging operations within the service.</param>
    protected BaseDataLoaderService(
        UniqueKeyGenerator<TKey> uniqueKeyGenerator,
        VectorStoreCollection<TKey, TRecord> vectorStoreRecordCollection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IConfigurationService configurationService,
        IServiceProvider serviceProvider,
        IEmbeddingCache embeddingCache,
        ILogger logger)
    {
        UniqueKeyGenerator = uniqueKeyGenerator;
        VectorStoreRecordCollection = vectorStoreRecordCollection;
        EmbeddingGenerator = embeddingGenerator;
        ConfigurationService = configurationService;
        ServiceProvider = serviceProvider;
        EmbeddingCache = embeddingCache;
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
        // Avoid re-allocation if already a list/array
        var recordsList = records as IList<TRecord> ?? records.ToList();
        var totalCount = recordsList.Count;

        // Pre-allocate result array with known size
        var processedRecords = new TRecord[totalCount];
        var processedIndex = 0;

        for (var i = 0; i < totalCount; i += batchSize)
        {
            var batchEnd = Math.Min(i + batchSize, totalCount);
            var currentBatchSize = batchEnd - i;

            // Single allocation for text batch per iteration
            var batchTexts = new string[currentBatchSize];

            // Enrich records and collect texts in a single pass
            for (var j = 0; j < currentBatchSize; j++)
            {
                var record = recordsList[i + j];
                await EnrichRecordAsync(record, fileName, cancellationToken).ConfigureAwait(false);
                batchTexts[j] = record.Text ?? string.Empty;
            }

            var embeddings = await GenerateEmbeddingsAsync(batchTexts, cancellationToken).ConfigureAwait(false);

            // Assign embeddings and copy to result array
            for (var j = 0; j < currentBatchSize; j++)
            {
                var record = recordsList[i + j];
                record.TextEmbedding = embeddings[j];
                processedRecords[processedIndex++] = record;
            }
        }

        return processedRecords;
    }

    /// <summary>
    /// Enriches a single record with a unique key, metadata, text embeddings, and token count.
    /// </summary>
    /// <param name="record">The record to be enriched.</param>
    /// <param name="fileName">The source file name used as metadata for the record.</param>
    /// <param name="cancellationToken">Token used to propagate notifications that the operation should be canceled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected Task EnrichRecordAsync(TRecord record, string fileName, CancellationToken cancellationToken)
    {
        var textChunk = record.Text ?? throw new InvalidOperationException("Text is null");

        record.Key = UniqueKeyGenerator.GenerateKey();
        record.ReferenceDescription ??= fileName.StartsWith("file://") ? fileName.Substring(7) : fileName;
        record.ReferenceLink ??= fileName.StartsWith("file://") ? System.Web.HttpUtility.UrlEncode(fileName) : $"file://{System.Web.HttpUtility.UrlEncode(fileName)}";
        record.TokenCount ??= TokenCounter.CountTokens(textChunk);

        if (record.TokenCount > MaxEmbeddingTokens)
        {
            Logger.LogWarning("Chunk exceeds embedding token limit: {TokenCount} > {MaxLimit} for {FileName}",
                record.TokenCount, MaxEmbeddingTokens, fileName);
        }

        return Task.CompletedTask;
    }
    
    protected async Task<Embedding<float>[]> GenerateEmbeddingsAsync(string[] textChunks, CancellationToken cancellationToken)
    {
        var results = new Embedding<float>[textChunks.Length];
        var uncachedIndices = new List<int>();
        var uncachedTexts = new List<string>();

        // Check cache for existing embeddings
        for (var i = 0; i < textChunks.Length; i++)
        {
            var cached = EmbeddingCache.Get(textChunks[i]);
            if (cached != null)
            {
                results[i] = cached;
            }
            else
            {
                uncachedIndices.Add(i);
                uncachedTexts.Add(textChunks[i]);
            }
        }

        // Generate embeddings only for uncached texts
        if (uncachedTexts.Count > 0)
        {
            var cacheHits = textChunks.Length - uncachedTexts.Count;
            Logger.LogDebug("Generating {Count} embeddings (cache hits: {Hits})", uncachedTexts.Count, cacheHits);

            try
            {
                var options = new EmbeddingGenerationOptions
                {
                    Dimensions = 1024 // this should either be configuration or a requirement that the embedding model supports
                };

                var sw = Stopwatch.StartNew();

                var generated = await EmbeddingGenerator
                    .GenerateAsync(uncachedTexts, options, cancellationToken: cancellationToken).ConfigureAwait(false);

                var generatedArray = generated.ToArray();

                sw.Stop();
                Logger.LogDebug("Generated {Count} embeddings in {Ms}ms", generatedArray.Length, sw.ElapsedMilliseconds);

                // Store in cache and assign to results
                for (var i = 0; i < uncachedIndices.Count; i++)
                {
                    var embedding = generatedArray[i];
                    var originalIndex = uncachedIndices[i];
                    results[originalIndex] = embedding;
                    EmbeddingCache.Set(uncachedTexts[i], embedding);
                }
            }
            catch (ClientResultException ex)
            {
                Logger.LogError("Failed to generate embeddings. Error: {HttpOperationException}",
                    ex.GetRawResponse()?.Content.ToString() ?? ex.ToString());
                throw;
            }
        }
        else
        {
            Logger.LogDebug("All {Count} embeddings served from cache", textChunks.Length);
        }

        return results;
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
        var ragEmbeddingModelLocationDescriptor = await GetRagEmbeddingModelLocationDescriptorAsync();
        var ragVisionModelLocationDescriptor = await GetRagVisionModelLocationDescriptorAsync();

        var ragEmbeddingModelsService = ServiceProvider.GetKeyedService<IModelsService>(ragEmbeddingModelLocationDescriptor.InterfaceEngineId) 
            ?? throw new InvalidOperationException($"Failed to get RAG embedding models service for engine {ragEmbeddingModelLocationDescriptor.InterfaceEngineId}");
        var ragVisionModelsService = ServiceProvider.GetKeyedService<IModelsService>(ragVisionModelLocationDescriptor.InterfaceEngineId)
            ?? throw new InvalidOperationException($"Failed to get RAG vision models service for engine {ragVisionModelLocationDescriptor.InterfaceEngineId}");
        
        // Fire-and-forget: start the tasks but don't await them
        // They will run to completion in the background
        _ = ragEmbeddingModelsService.UnloadModelsAsync([ragEmbeddingModelLocationDescriptor.ModelId])
            .ContinueWith(t => { /* Ignore any exceptions */ }, TaskContinuationOptions.OnlyOnFaulted);
        
        _ = ragVisionModelsService.UnloadModelsAsync([ragVisionModelLocationDescriptor.ModelId])
            .ContinueWith(t => { /* Ignore any exceptions */ }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Retrieves the location descriptor of the RAG embedding model by accessing general settings.
    /// </summary>
    /// <returns>
    /// A <see cref="ModelLocationDescriptor"/> that contains the inference engine identifier and model identifier
    /// required for locating and working with an embedding model.
    /// </returns>
    protected async Task<ModelLocationDescriptor> GetRagEmbeddingModelLocationDescriptorAsync()
    {
        var generalSettings = await ConfigurationService.GetGeneralSettingsAsync();
        
        var inferenceEngineId = generalSettings.RagEmbeddingInferenceEngineId 
            ?? throw new InvalidOperationException($"RagEmbeddingInferenceEngineId has not been defined in GeneralSettings");
        var modelId = generalSettings.RagEmbeddingModel
            ?? throw new InvalidOperationException($"RagEmbeddingModel has not been defined in GeneralSettings");

        return new ModelLocationDescriptor(inferenceEngineId, modelId);
    }

    /// <summary>
    /// Retrieves the location descriptor of the RAG vision model by accessing general settings.
    /// </summary>
    /// <returns>
    /// A <see cref="ModelLocationDescriptor"/> that contains the inference engine identifier and model identifier
    /// required for locating and working with an embedding model.
    /// </returns>
    protected async Task<ModelLocationDescriptor> GetRagVisionModelLocationDescriptorAsync()
    {
        var generalSettings = await ConfigurationService.GetGeneralSettingsAsync();
        
        var inferenceEngineId = generalSettings.RagVisionInferenceEngineId 
                                ?? throw new InvalidOperationException($"RagVisionInferenceEngineId has not been defined in GeneralSettings");
        var modelId = generalSettings.RagVisionModel
                      ?? throw new InvalidOperationException($"RagVisionModel has not been defined in GeneralSettings");

        return new ModelLocationDescriptor(inferenceEngineId, modelId);
    }

    /// <summary>
    /// Upserts the processed records into the vector store.
    /// </summary>
    /// <param name="records">The records to upsert.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous upsert operation.</returns>
    protected async Task UpsertRecordsAsync(TRecord[] records, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        await VectorStoreRecordCollection
            .UpsertAsync(records, cancellationToken: cancellationToken).ConfigureAwait(false);
        
        sw.Stop();
        Logger.LogDebug("Upserted {RecordsSize} records in {ElapsedMilliseconds}ms", records.Length, sw.ElapsedMilliseconds);
    }
}
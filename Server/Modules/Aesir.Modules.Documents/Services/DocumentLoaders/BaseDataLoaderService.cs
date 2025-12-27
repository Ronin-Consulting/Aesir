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
    /// Utility for splitting text documents into manageable pieces based on token limits.
    /// Also provides token counting via the BERT tokenizer.
    /// </summary>
    protected static readonly DocumentChunker DocumentChunker = new();

    /// <summary>
    /// Maximum number of tokens allowed per chunk for the embedding model.
    /// Uses BERT tokenizer for accurate counting with mxbai-embed-large-v1 (context limit: 512).
    /// Chunks exceeding this limit are automatically split by ProcessRecordsInBatchesAsync.
    /// </summary>
    protected const int MaxEmbeddingTokens = 450;

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
    /// Oversized records are automatically split into multiple smaller records.
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
        // First pass: split any oversized records into smaller chunks
        var expandedRecords = new List<TRecord>();
        foreach (var record in records)
        {
            var text = record.Text ?? string.Empty;
            var tokenCount = DocumentChunker.CountTokensStatic(text);

            if (tokenCount <= MaxEmbeddingTokens)
            {
                // Record is within limits, keep as-is
                expandedRecords.Add(record);
            }
            else
            {
                // Record is oversized - split into multiple records
                Logger.LogDebug("Splitting oversized record ({TokenCount} tokens) into smaller chunks for {FileName}",
                    tokenCount, fileName);

                var chunks = DocumentChunker.ChunkText(text, null);

                for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
                {
                    // Clone the record properties to a new record
                    // The first chunk keeps the original record, subsequent chunks are new
                    if (chunkIndex == 0)
                    {
                        record.Text = chunks[chunkIndex];
                        expandedRecords.Add(record);
                    }
                    else
                    {
                        // Create a new record by copying properties from original
                        var newRecord = CloneRecordWithNewText(record, chunks[chunkIndex]);
                        expandedRecords.Add(newRecord);
                    }
                }
            }
        }

        var totalCount = expandedRecords.Count;

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
                var record = expandedRecords[i + j];
                await EnrichRecordAsync(record, fileName, cancellationToken).ConfigureAwait(false);
                batchTexts[j] = record.Text ?? string.Empty;
            }

            var embeddings = await GenerateEmbeddingsAsync(batchTexts, cancellationToken).ConfigureAwait(false);

            // Assign embeddings and copy to result array
            for (var j = 0; j < currentBatchSize; j++)
            {
                var record = expandedRecords[i + j];
                record.TextEmbedding = embeddings[j];
                processedRecords[processedIndex++] = record;
            }
        }

        return processedRecords;
    }

    /// <summary>
    /// Creates a shallow copy of a record with new text content.
    /// Override this method in derived classes if TRecord requires special cloning logic.
    /// </summary>
    /// <param name="original">The original record to clone.</param>
    /// <param name="newText">The new text content for the cloned record.</param>
    /// <returns>A new record with the same properties but different text.</returns>
    protected virtual TRecord CloneRecordWithNewText(TRecord original, string newText)
    {
        // Use reflection to create a new instance and copy properties
        // This is a generic fallback - derived classes can override for better performance
        var clone = (TRecord)Activator.CreateInstance(typeof(TRecord))!;

        // Copy common AesirTextData properties
        clone.Text = newText;
        clone.ReferenceDescription = original.ReferenceDescription;
        clone.ReferenceLink = original.ReferenceLink;
        // Key and TokenCount will be set by EnrichRecordAsync
        // TextEmbedding will be set after embedding generation

        return clone;
    }

    /// <summary>
    /// Extracts the local file path from a file URI, handling both file:// and file:/// formats.
    /// </summary>
    /// <param name="fileNameOrUri">The file name or file URI to process.</param>
    /// <returns>The local file path without the file:// prefix.</returns>
    protected static string GetLocalPathFromFileUri(string fileNameOrUri)
    {
        if (string.IsNullOrEmpty(fileNameOrUri) ||
            !fileNameOrUri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return fileNameOrUri;

        try
        {
            var uri = new Uri(fileNameOrUri);
            return uri.LocalPath;
        }
        catch (UriFormatException)
        {
            // Fallback: strip file:// or file:/// prefix manually
            return fileNameOrUri.StartsWith("file:///")
                ? fileNameOrUri.Substring(8)
                : fileNameOrUri.Substring(7);
        }
    }

    /// <summary>
    /// Converts a file path to a properly formatted file URI.
    /// </summary>
    /// <param name="fileNameOrUri">The file name or existing URI.</param>
    /// <returns>A properly formatted file URI.</returns>
    protected static string GetFileUri(string fileNameOrUri)
    {
        if (string.IsNullOrEmpty(fileNameOrUri))
            return fileNameOrUri;

        if (fileNameOrUri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            // Already a URI, normalize it
            try
            {
                return new Uri(fileNameOrUri).AbsoluteUri;
            }
            catch (UriFormatException)
            {
                return fileNameOrUri;
            }
        }

        // Convert path to URI
        try
        {
            return new Uri(fileNameOrUri).AbsoluteUri;
        }
        catch (UriFormatException)
        {
            return $"file://{System.Web.HttpUtility.UrlEncode(fileNameOrUri)}";
        }
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
        record.ReferenceDescription ??= GetLocalPathFromFileUri(fileName);
        record.ReferenceLink ??= GetFileUri(fileName);
        record.TokenCount ??= DocumentChunker.CountTokensStatic(textChunk);

        // Log warning if chunk is larger than expected (should have been split upstream)
        if (record.TokenCount > MaxEmbeddingTokens)
        {
            Logger.LogWarning("Chunk exceeds embedding token limit: {TokenCount} > {MaxLimit} for {FileName}. " +
                "This should have been split by ProcessRecordsInBatchesAsync.",
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

        var cleanFileName = GetLocalPathFromFileUri(fileName);
        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(cleanFileName)).ToList();

        if (toDelete.Count > 0)
        {
            Logger.LogDebug("Deleting {Count} existing records for file {FileName}", toDelete.Count, cleanFileName);
            await VectorStoreRecordCollection.DeleteAsync(
                toDelete.Select(td => td.Key), cancellationToken).ConfigureAwait(false);
        }
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
            .ContinueWith(t =>
            {
                Logger.LogWarning(t.Exception, "Failed to unload RAG embedding model {ModelId}",
                    ragEmbeddingModelLocationDescriptor.ModelId);
            }, TaskContinuationOptions.OnlyOnFaulted);

        _ = ragVisionModelsService.UnloadModelsAsync([ragVisionModelLocationDescriptor.ModelId])
            .ContinueWith(t =>
            {
                Logger.LogWarning(t.Exception, "Failed to unload RAG vision model {ModelId}",
                    ragVisionModelLocationDescriptor.ModelId);
            }, TaskContinuationOptions.OnlyOnFaulted);
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
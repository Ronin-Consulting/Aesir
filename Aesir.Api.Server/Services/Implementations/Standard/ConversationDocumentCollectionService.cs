using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides functionality for managing document collections related to conversations, enabling operations
/// such as loading, deleting, and retrieving documents. Additionally, facilitates the generation of kernel
/// plugins designed for advanced searchability and interaction within conversational contexts.
/// </summary>
/// <remarks>
/// The service integrates with underlying search and storage components, including vector-based and hybrid
/// search mechanisms, for optimized document retrieval. Marked as experimental with code SKEXP0070.
/// </remarks>
[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    /// <summary>
    /// Represents the cap on the number of results to retrieve during a document search process.
    /// </summary>
    /// <remarks>
    /// Used to control the size of the result set returned by search operations, ensuring efficient
    /// performance and relevance by limiting outcomes to a specific count.
    /// </remarks>
    private const int TopResults = 25;

    /// <summary>
    /// Encapsulates a vector-based semantic search engine specifically designed for conversation document data management.
    /// </summary>
    /// <remarks>
    /// This field enables the service to perform advanced search functionality by leveraging vector embeddings.
    /// It is integral to locating and retrieving conversational documents that semantically match a given query,
    /// providing enhanced search precision within the context of conversation data processing.
    /// </remarks>
    private readonly VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> _conversationDocumentVectorSearch;

    /// <summary>
    /// Represents an optional hybrid search capability used for retrieving conversation documents
    /// by combining both keyword-based and vector-based search methodologies.
    /// </summary>
    /// <remarks>
    /// This private field integrates the functionality of the IKeywordHybridSearchable interface
    /// with the AesirConversationDocumentTextData model, allowing for a comprehensive search process
    /// that supports semantic understanding alongside traditional keyword search. It is utilized
    /// within the ConversationDocumentCollectionService to enhance document retrieval operations.
    /// </remarks>
    private readonly IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>?
        _conversationDocumentHybridSearch;

    /// <summary>
    /// Represents the private collection of vector store records within the service,
    /// keyed by a unique identifier of type <see cref="System.Guid"/> and containing instances
    /// of <see cref="AesirConversationDocumentTextData{TKey}"/> specific to conversation document data.
    /// </summary>
    /// <remarks>
    /// This field is intended for managing persistent storage and operations associated with
    /// conversation-related text data. It supports actions such as querying, updating, or removing
    /// entries from the vector storage. The collection is initialized and managed through dependency
    /// injection in the service's constructor.
    /// </remarks>
    private readonly VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// Handles the loading and retrieval of PDF data associated with conversation documents.
    /// </summary>
    /// <remarks>
    /// This field is utilized to facilitate operations that involve fetching and managing
    /// PDF document content related to specific conversation document identifiers.
    /// </remarks>
    private readonly IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _pdfDataLoader;

    private readonly IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _imageDataLoader;

    /// <summary>
    /// Logger instance for the <see cref="ConversationDocumentCollectionService"/> class.
    /// </summary>
    /// <remarks>
    /// Used to capture and record operational details, including informational messages, warnings,
    /// and errors, to aid in monitoring, debugging, and maintaining the service's functionalities.
    /// </remarks>
    private readonly ILogger<ConversationDocumentCollectionService> _logger;

    /// <summary>
    /// Service responsible for managing a collection of conversation documents and enabling search functionality.
    /// This includes vector-based search and optional keyword hybrid search mechanisms.
    /// </summary>
    public ConversationDocumentCollectionService(
        VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentVectorSearch,
        IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>? conversationDocumentHybridSearch,
        VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> vectorStoreRecordCollection,
        IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> pdfDataLoader,
        IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> imageDataLoader,
        ILogger<ConversationDocumentCollectionService> logger
    )
    {
        _conversationDocumentVectorSearch = conversationDocumentVectorSearch;
        _conversationDocumentHybridSearch = conversationDocumentHybridSearch;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _pdfDataLoader = pdfDataLoader;
        _imageDataLoader = imageDataLoader;
        _logger = logger;
    }

    /// <summary>
    /// Loads a document from the specified path into the conversation document collection asynchronously.
    /// </summary>
    /// <param name="documentPath">The full path of the document to be loaded.</param>
    /// <param name="fileMetaData">Optional metadata describing attributes of the document.</param>
    /// <param name="cancellationToken">A token to observe for cancellation of the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous loading operation.</returns>
    /// <exception cref="InvalidDataException">Thrown when the document format is unsupported or invalid.</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken)
    {
        // for now enforce only PDFs and PNGs
        if (!documentPath.ValidFileContentType(SupportedFileContentTypes.PdfContentType, out var actualContentType) && 
            !documentPath.ValidFileContentType(SupportedFileContentTypes.PngContentType, out actualContentType))
        {
            throw new InvalidDataException($"Invalid file content type: {actualContentType}");
        }
        
        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }

        if (actualContentType == SupportedFileContentTypes.PdfContentType)
        {
            var pdfRequest = new LoadPdfRequest()
            {
                PdfLocalPath = documentPath,
                PdfFileName = fileNameMetaData.ToString(),
                BetweenBatchDelayInMs = 10,
                Metadata = fileMetaData
            };
            await _pdfDataLoader.LoadPdfAsync(pdfRequest, cancellationToken);   
        }

        if (actualContentType == SupportedFileContentTypes.PngContentType)
        {
            var imageRequest = new LoadImageRequest()
            {
                ImageLocalPath = documentPath,
                ImageFileName = fileNameMetaData.ToString(),
                Metadata = fileMetaData
            };
            
            await _imageDataLoader.LoadImageAsync(imageRequest, cancellationToken);
        }
    }

    /// <summary>
    /// Asynchronously deletes a document from the collection based on the provided metadata and cancellation token.
    /// </summary>
    /// <param name="fileMetaData">A dictionary containing metadata about the file to be deleted, including required keys such as "FileName" and "ConversationId".</param>
    /// <param name="cancellationToken">An optional token to observe while waiting for the task to complete.</param>
    /// <returns>A boolean indicating whether the document was successfully deleted.</returns>
    public async Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken = default)
    {
        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }

        await _vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken);
        
        if (!fileMetaData.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("File metadata must contain a ConversationId property");

        var conversationId = (string)metaValue;
        
        var fileName = fileNameMetaData.ToString();
        
        var retrievalOptions = new FilteredRecordRetrievalOptions<AesirConversationDocumentTextData<Guid>>()
        {
            IncludeVectors = false
        };
        var toDelete = await _vectorStoreRecordCollection.GetAsync(
                filter: data => data.ConversationId == conversationId,
                top: int.MaxValue, // this is dumb
                options: retrievalOptions,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        toDelete = toDelete.Where(data =>
            data.ReferenceDescription!.Contains(fileName!.TrimStart("file://"))).ToList();

        if (toDelete.Count <= 0)
            return false;
        
        await _vectorStoreRecordCollection.DeleteAsync(
            toDelete.Select(td => td.Key), cancellationToken);
        
        return true;
    }

    /// <summary>
    /// Deletes documents associated with a specific conversation ID from the vector store.
    /// </summary>
    /// <param name="args">A dictionary of arguments, where "ConversationId" is the required key identifying the specific conversation whose documents need to be deleted.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public async Task DeleteDocumentsAsync(IDictionary<string, object>? args,
        CancellationToken cancellationToken = default)
    {
        if (args == null || !args.TryGetValue("ConversationId", out var argValue))
            throw new ArgumentException("Args must contain a ConversationId property");

        await _vectorStoreRecordCollection.EnsureCollectionExistsAsync(cancellationToken);
        
        var conversationId = (string)argValue;

        var retrievalOptions = new FilteredRecordRetrievalOptions<AesirConversationDocumentTextData<Guid>>()
        {
            IncludeVectors = false
        };
        var toDelete = await _vectorStoreRecordCollection.GetAsync(
                filter: data => data.ConversationId == conversationId,
                top: int.MaxValue, // this is dumb
                options: retrievalOptions,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        if (toDelete.Count <= 0) return;
        
        await _vectorStoreRecordCollection.DeleteAsync(
            toDelete.Select(td => td.Key), cancellationToken);
    }

    /// <summary>
    /// Creates and configures a Semantic Kernel plugin for searching conversation documents.
    /// </summary>
    /// <param name="kernelPluginArguments">
    /// Optional parameters for configuring the kernel plugin. Must include a non-empty "ConversationId" key.
    /// </param>
    /// <returns>
    /// An instance of <see cref="KernelPlugin"/> configured for conversation document search.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="kernelPluginArguments"/> is null or does not contain a "ConversationId" key.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the value associated with the "ConversationId" key is null or empty.
    /// </exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        const string pluginName = "ChatTools";
        
        if (kernelPluginArguments == null || !kernelPluginArguments.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("File metadata must contain a ConversationId property");
        
        var conversationId = (string)metaValue;

        var kernelFunctions = new List<KernelFunction>();
        
        // add image functions
        //AnalyzeImageContent
        var imageSearchFilter = new TextSearchFilter();
        imageSearchFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
        
        var imageTextSearchOptions = new TextSearchOptions
        {
            Top = TopResults, 
            Filter = imageSearchFilter
        };

        var imageTextSearchResultsFunctionOptions = new KernelFunctionFromMethodOptions()
        {
            FunctionName = "AnalyzeImageContent",
            Description = "Analyzes an image to classify it as 'document' or 'non-document', transcribing text if a document or providing a detailed description otherwise.",
            Parameters = [
                new KernelParameterMetadata("query") { Description = "The name of the image file to process.", ParameterType = typeof(string), IsRequired = true },
                new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
            ],
            ReturnParameter = new KernelReturnParameterMetadata { ParameterType = typeof(KernelSearchResults<TextSearchResult>), Description = "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link."  },
        };
        
        var imageSearchFunction = _conversationDocumentVectorSearch
            .CreateGetTextSearchResults(searchOptions: imageTextSearchOptions, options: imageTextSearchResultsFunctionOptions);
        
        kernelFunctions.Add(imageSearchFunction);
        
        // text searches
        if (_conversationDocumentHybridSearch != null && false)
        {
            var hybridSearch = _conversationDocumentHybridSearch;

            // ReSharper disable once MoveLocalFunctionAfterJumpStatement
            async Task<IEnumerable<TextSearchResult>> GetHybridSearchResultAsync(Kernel kernel, KernelFunction function,
                KernelArguments arguments, CancellationToken cancellationToken, int count = TopResults, int skip = 0)
            {
                arguments.TryGetValue("query", out var query);
                if (string.IsNullOrEmpty(query?.ToString()))
                {
                    return [];
                }
                
                var searchOptions = new HybridSearchOptions<AesirConversationDocumentTextData<Guid>>
                {
                    Filter = data => data.ConversationId == conversationId,
                    Skip = skip
                };
                
                var searchValue = query.ToString()!;
                var keywords = searchValue.KeywordsOnly();
                var results = await hybridSearch.HybridSearchAsync(
                    searchValue,
                    keywords,
                    count,
                    searchOptions,
                    cancellationToken
                ).ToListAsync(cancellationToken).ConfigureAwait(false);
                
                return results.Select(r =>
                    new TextSearchResult(r.Record.Text!)
                    {
                        Link = r.Record.ReferenceLink,
                        Name = r.Record.ReferenceDescription
                    }
                );
            }

            var functionOptions = new KernelFunctionFromMethodOptions()
            {
                FunctionName = "PerformHybridDocumentSearch",
                Description = "Executes a hybrid search combining exact keyword matching with semantic relevance for the given query. Returns a collection of results, each including a name (e.g., title or identifier), value (e.g., snippet or full content), and link (e.g., URI or reference) for the matched items. Use 'count' to limit results and 'skip' for pagination.",
                Parameters = [
                    new KernelParameterMetadata("query") { Description = "The search query string, supporting keywords, phrases, or natural language input for hybrid matching.", ParameterType = typeof(string), IsRequired = true },
                    new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                    new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
                ],
                ReturnParameter = new KernelReturnParameterMetadata { ParameterType = typeof(KernelSearchResults<TextSearchResult>), Description = "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link." },
            };
            
            var hybridSearchFunction = KernelFunctionFactory.CreateFromMethod(GetHybridSearchResultAsync, functionOptions);
            
            kernelFunctions.Add(hybridSearchFunction);
        }
        else
        {
            var semanticSearchFilter = new TextSearchFilter();
            semanticSearchFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
            
            var semanticTextSearchOptions = new TextSearchOptions
            {
                Top = TopResults, 
                Filter = semanticSearchFilter
            };

            var semanticSearchResultsFunctionOptions = new KernelFunctionFromMethodOptions()
            {
                FunctionName = "PerformSemanticDocumentSearch",
                Description = "Executes a semantic search for the given query. Returns a collection of results, each including a name (e.g., title or identifier), value (e.g., snippet or full content), and link (e.g., URI or reference) for the matched items. Use 'count' to limit results and 'skip' for pagination.",
                Parameters = [
                    new KernelParameterMetadata("query") { Description = "The search query string, supporting keywords, phrases, or natural language input for semantic matching.", ParameterType = typeof(string), IsRequired = true },
                    new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 25).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 25 },
                    new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
                ],
                ReturnParameter = new KernelReturnParameterMetadata { ParameterType = typeof(KernelSearchResults<TextSearchResult>), Description = "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link."  },
            };
            
            var semanticSearchFunction = _conversationDocumentVectorSearch
                .CreateGetTextSearchResults(searchOptions: semanticTextSearchOptions, options: semanticSearchResultsFunctionOptions);
            
            kernelFunctions.Add(semanticSearchFunction);
        }
        
        return KernelPluginFactory.CreateFromFunctions(
            pluginName,
            kernelFunctions.ToArray()
        ); 
    }
}
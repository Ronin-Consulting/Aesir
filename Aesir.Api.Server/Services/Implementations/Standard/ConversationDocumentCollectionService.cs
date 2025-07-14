using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service for managing document collections associated with conversations.
/// Provides operations for loading, deleting, and managing documents, as well as generating searchable plugins
/// tailored for conversation-related queries.
/// </summary>
/// <remarks>
/// This implementation is tagged as experimental with code SKEXP0070.
/// </remarks>
[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    /// <summary>
    /// Specifies the maximum number of results to return during a document search operation.
    /// </summary>
    /// <remarks>
    /// This constant is used in search-related processes to limit the number of items retrieved
    /// and optimize performance. It ensures only the top matching results are returned based on the search logic.
    /// </remarks>
    private const int TopResults = 5;

    /// <summary>
    /// Represents a vector-based text search mechanism for handling conversation document data within the service.
    /// This field is used for performing operations such as searching conversation documents using vector embeddings
    /// to find the most relevant semantic matches.
    /// </summary>
    private readonly VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> _conversationDocumentVectorSearch;

    /// <summary>
    /// Represents a hybrid search mechanism for conversation documents that combines keyword-based searching
    /// with vector-based searching. This private field is optional and used within the
    /// ConversationDocumentCollectionService to enhance document retrieval based on search criteria.
    /// </summary>
    /// <remarks>
    /// The hybrid search functionality, if provided, leverages the generic
    /// IKeywordHybridSearchable interface with AesirConversationDocumentTextData as the data type.
    /// This allows for both semantic and keyword-based filtering of conversation documents.
    /// </remarks>
    private readonly IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>? _conversationDocumentHybridSearch;

    /// <summary>
    /// Represents a private collection of vector store records, keyed by Guid and containing
    /// instances of <see cref="AesirConversationDocumentTextData{TKey}"/>.
    /// </summary>
    /// <remarks>
    /// This collection is likely used for managing operations such as retrieval, filtering,
    /// and deletion of document and vector data within the context of conversation documents.
    /// The specific type parameter ensures that the data is uniquely identified and conforms
    /// to the structure of <see cref="AesirConversationDocumentTextData{TKey}"/>.
    /// This field is initialized via dependency injection in the constructor of the containing
    /// service class.
    /// </remarks>
    private readonly VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// The <c>_pdfDataLoader</c> field is an instance of <see cref="IPdfDataLoaderService{TKey, TRecord}"/> that facilitates loading and processing
    /// of PDF documents in the context of a conversation document collection service.
    /// </summary>
    /// <remarks>
    /// This field is utilized primarily for interacting with the PDF data loader service to handle operations related to
    /// PDF document ingestion, metadata processing, and asynchronous loading tasks. It supports integration with
    /// file processing pipelines, ensuring that only supported file types (e.g., PDFs) are processed.
    /// The underlying service implementation encapsulates specific methods for loading PDF documents, as demonstrated
    /// in methods such as <c>LoadDocumentAsync</c>.
    /// </remarks>
    /// <typeparam name="TKey">
    /// The type of the identifier used to uniquely distinguish documents, constrained to non-nullable types.
    /// </typeparam>
    /// <typeparam name="TRecord">
    /// Represents the data record, which in this case is <see cref="AesirConversationDocumentTextData{TKey}"/>,
    /// supporting additional metadata and document-related content.
    /// </typeparam>
    private readonly IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Logger instance for the <see cref="ConversationDocumentCollectionService"/> class.
    /// </summary>
    /// <remarks>
    /// Utilized for logging information, warnings, errors, and other diagnostic messages
    /// related to the operations and functionality provided by the service.
    /// </remarks>
    private readonly ILogger<ConversationDocumentCollectionService> _logger;

    /// <summary>
    /// Provides services for managing and searching conversation documents using vector-based and hybrid search techniques.
    /// </summary>
    public ConversationDocumentCollectionService(
        VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentVectorSearch,
        IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>? conversationDocumentHybridSearch,
        VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> vectorStoreRecordCollection,
        IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> pdfDataLoader,
        ILogger<ConversationDocumentCollectionService> logger
    )
    {
        _conversationDocumentVectorSearch = conversationDocumentVectorSearch;
        _conversationDocumentHybridSearch = conversationDocumentHybridSearch;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _pdfDataLoader = pdfDataLoader;
        _logger = logger;
    }

    /// <summary>
    /// Loads a document from the specified path into the conversation document collection asynchronously.
    /// </summary>
    /// <param name="documentPath">The path of the document to be loaded.</param>
    /// <param name="fileMetaData">Optional metadata associated with the document.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidDataException">Thrown if the file is not a supported PDF document.</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken)
    {
        // for now enforce only PDFs
        if (!documentPath.ValidFileContentType(SupportedFileContentTypes.PdfContentType, out var actualContentType))
        {
            throw new InvalidDataException($"Invalid file content type: {actualContentType}");
        }

        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }
        
        var request = new LoadPdfRequest()
        {
            PdfLocalPath = documentPath,
            PdfFileName = fileNameMetaData.ToString(),
            BetweenBatchDelayInMs = 10,
            Metadata = fileMetaData
        };
        await _pdfDataLoader.LoadPdfAsync(request, cancellationToken);
    }

    /// <summary>
    /// Asynchronously deletes a document from the collection based on the provided metadata and cancellation token.
    /// </summary>
    /// <param name="fileMetaData">A dictionary containing metadata about the file to be deleted, including required keys such as "FileName" and "ConversationId".</param>
    /// <param name="cancellationToken">An optional token to observe while waiting for the task to complete.</param>
    /// <returns>A boolean indicating whether the document was successfully deleted.</returns>
    public async Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData, CancellationToken cancellationToken = default)
    {
        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }
        
        if(!fileMetaData.TryGetValue("ConversationId", out var metaValue))
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
    /// <param name="args">A collection of arguments, including a required "ConversationId" key identifying the conversation whose documents will be deleted.</param>
    /// <param name="cancellationToken">Token used to propagate notification that operations should be canceled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default)
    {
        if(args == null || !args.TryGetValue("ConversationId", out var argValue))
            throw new ArgumentException("Args must contain a ConversationId property");
        
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
    /// Creates a Semantic Kernel plugin for searching conversation documents.
    /// </summary>
    /// <param name="kernelPluginArguments">Optional arguments for the kernel plugin. Must contain a non-empty ConversationId.</param>
    /// <returns>An instance of <see cref="KernelPlugin"/> configured for conversation document search.</returns>
    /// <exception cref="ArgumentException">Thrown when kernelPluginArguments is null or does not include a ConversationId.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the ConversationId is null or empty.</exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if (kernelPluginArguments == null || !kernelPluginArguments.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("File metadata must contain a ConversationId property");

        var conversationId = (string)metaValue;

        if (_conversationDocumentHybridSearch != null)
        {
            var hybridSearch = _conversationDocumentHybridSearch;

            // do hybrid search
            // ReSharper disable once MoveLocalFunctionAfterJumpStatement
            async Task<IEnumerable<TextSearchResult>> GetHybridSearchResultAsync(Kernel kernel, KernelFunction function,
                KernelArguments arguments, CancellationToken cancellationToken, int? count, int skip = 0)
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
                    count ?? TopResults,
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
                FunctionName = "HybridKeywordSearch",
                Description = "Executes a hybrid search combining exact keyword matching with semantic relevance for the given query. Ideal for retrieving targeted content from local or indexed data sources on edge devices. Returns a collection of results, each including a name (e.g., title or identifier), value (e.g., snippet or full content), and link (e.g., URI or reference) for the matched items. Use 'count' to limit results and 'skip' for pagination.",
                Parameters = [
                    new KernelParameterMetadata("query") { Description = "The search query string, supporting keywords, phrases, or natural language input for hybrid matching.", ParameterType = typeof(string), IsRequired = true },
                    new KernelParameterMetadata("count") { Description = "Maximum number of results to return (default: 5).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 2 },
                    new KernelParameterMetadata("skip") { Description = "Number of initial results to skip for pagination (default: 0).", ParameterType = typeof(int), IsRequired = false, DefaultValue = 0 },
                ],
                ReturnParameter = new KernelReturnParameterMetadata { ParameterType = typeof(KernelSearchResults<TextSearchResult>), Description = "A collection of search results, where each TextSearchResult contains properties like Name, Value, and Link." },
            };
            
            var hybridSearchFunction = KernelFunctionFactory.CreateFromMethod(GetHybridSearchResultAsync, functionOptions);
            
            return KernelPluginFactory.CreateFromFunctions(
                "ChatDocSearch",
                [hybridSearchFunction]
            ); 
        }

        // standard vector search
        var conversationFilter = new TextSearchFilter();
        conversationFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
        
        var conversationDocumentTextSearchOptions = new TextSearchOptions
        {
            Top = TopResults, 
            Filter = conversationFilter
        };
            
        var conversationDocumentSearchFunction = _conversationDocumentVectorSearch
            .CreateGetTextSearchResults(searchOptions: conversationDocumentTextSearchOptions);
        
        return KernelPluginFactory.CreateFromFunctions(
            "ChatDocSearch",
            [conversationDocumentSearchFunction]
        );
    }
}
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides an experimental implementation for managing a global collection of documents within the application.
/// Enables document loading, deletion, and kernel plugin creation for enhanced document search operations.
/// </summary>
[Experimental("SKEXP0001")]
public class GlobalDocumentCollectionService : IGlobalDocumentCollectionService
{
    /// <summary>
    /// Specifies the maximum number of search results to return when querying global documents.
    /// </summary>
    private const int MaxTopResults = 50;

    /// <summary>
    /// A private instance of vector-based text search functionality for global document text data.
    /// Used for performing efficient searches in a vectorized document collection.
    /// </summary>
    private readonly VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>> _globalDocumentVectorSearch;

    /// <summary>
    /// A hybrid search service that combines keyword and vector-based search functionality for
    /// querying text data in global documents.
    /// </summary>
    private readonly IKeywordHybridSearchable<AesirGlobalDocumentTextData<Guid>>? _globalDocumentHybridSearch;

    /// <summary>
    /// Collection for storing and managing vectorized records associated with global document text data
    /// </summary>
    private readonly VectorStoreCollection<Guid, AesirGlobalDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// Service for loading and processing PDF documents into structured text data
    /// </summary>
    private readonly IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Logger used for capturing and managing log messages within the GlobalDocumentCollectionService.
    /// </summary>
    private readonly ILogger<GlobalDocumentCollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalDocumentCollectionService class
    /// </summary>
    /// <param name="globalDocumentVectorSearch">Service for performing vector-based text search in global documents</param>
    /// <param name="globalDocumentHybridSearch">Optional service for performing hybrid keyword and vector-based searches</param>
    /// <param name="vectorStoreRecordCollection">Collection used to manage records in the vector store</param>
    /// <param name="pdfDataLoader">Service responsible for loading PDF documents into the system</param>
    /// <param name="logger">Logger instance for logging operations within the GlobalDocumentCollectionService</param>
    public GlobalDocumentCollectionService(
        VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>> globalDocumentVectorSearch,
        IKeywordHybridSearchable<AesirGlobalDocumentTextData<Guid>>? globalDocumentHybridSearch,
        VectorStoreCollection<Guid, AesirGlobalDocumentTextData<Guid>> vectorStoreRecordCollection,
        IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>> pdfDataLoader,
        ILogger<GlobalDocumentCollectionService> logger
    )
    {
        _globalDocumentVectorSearch = globalDocumentVectorSearch;
        _globalDocumentHybridSearch = globalDocumentHybridSearch;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _pdfDataLoader = pdfDataLoader;
        _logger = logger;
    }

    /// <summary>
    /// Loads a document from the specified path asynchronously into the global document collection
    /// </summary>
    /// <param name="documentPath">The file path of the document to be loaded</param>
    /// <param name="fileMetaData">Optional metadata to associate with the document being loaded</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests</param>
    /// <returns>A task that represents the asynchronous load operation</returns>
    /// <exception cref="InvalidDataException">Thrown when the provided file content type is not supported</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
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
    /// Deletes a document from the collection based on the provided metadata.
    /// </summary>
    /// <param name="fileMetaData">The metadata dictionary containing information about the file to delete, including the file name.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>Returns true if the document was successfully deleted; otherwise, false.</returns>
    public async Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData, CancellationToken cancellationToken = default)
    {
        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }
        
        var fileName = fileNameMetaData.ToString();
        
        var toDelete = await _vectorStoreRecordCollection.GetAsync(
                filter: data => true,
                10000, // this is dumb 
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
    /// Deletes documents from the global document collection that match the specified criteria.
    /// </summary>
    /// <param name="args">A dictionary containing parameters for the deletion, where "CategoryId" is required to specify the category of documents to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default)
    {
        if(args == null || !args.TryGetValue("CategoryId", out var metaValue))
            throw new ArgumentException("Args must contain a CategoryId property");
        
        var categoryId = (string)args["CategoryId"];
        
        var retrievalOptions = new FilteredRecordRetrievalOptions<AesirGlobalDocumentTextData<Guid>>()
        {
            IncludeVectors = false
        };
        var toDelete = await _vectorStoreRecordCollection.GetAsync(
                filter: data => data.Category == categoryId,
                top: int.MaxValue, // this is dumb
                options: retrievalOptions,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        if (toDelete.Count <= 0) return;
        
        await _vectorStoreRecordCollection.DeleteAsync(
            toDelete.Select(td => td.Key), cancellationToken);
    }

    /// <summary>
    /// Creates a kernel plugin for searching documents in the global document collection
    /// </summary>
    /// <param name="kernelPluginArguments">A dictionary containing arguments required for the kernel plugin. Must include keys for CategoryId, PluginName, and optionally PluginDescription</param>
    /// <returns>A kernel plugin instance used for executing document searches</returns>
    /// <exception cref="ArgumentException">Thrown when one of the required arguments (CategoryId, PluginName) is missing from the kernelPluginArguments</exception>
    /// <exception cref="ArgumentNullException">Thrown when the value for CategoryId is null or empty</exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if(kernelPluginArguments == null)
            throw new ArgumentException("Kernel plugin args must contain a ConversationId");
        
        if (!kernelPluginArguments.TryGetValue("PluginName", out var pluginNameValue))
            throw new ArgumentException("Kernel plugin args must contain a PluginName");

        var kernelFunctionLibrary = new KernelFunctionLibrary<Guid,AesirGlobalDocumentTextData<Guid>>(
            _globalDocumentVectorSearch, _globalDocumentHybridSearch
        );
        
        var kernelFunctions = new List<KernelFunction>();
        
        var pluginName = (string)pluginNameValue;
        
        if (!kernelPluginArguments.TryGetValue("CategoryId", out var metaValue))
            throw new ArgumentException("Kernel plugin args must contain a CategoryId");
        var categoryId = (string)metaValue;
        
        if (string.IsNullOrEmpty(categoryId)) throw new ArgumentNullException(nameof(categoryId));

        if (_globalDocumentHybridSearch != null)
        {
            var searchOptions = new HybridSearchOptions<AesirGlobalDocumentTextData<Guid>>
            {
                Filter = data => data.Category == categoryId
            };
            
            kernelFunctions.Add(kernelFunctionLibrary.GetHybridDocumentSearchFunction(searchOptions, MaxTopResults));
        }
        else
        {
            var semanticSearchFilter = new TextSearchFilter();
            semanticSearchFilter.Equality(nameof(AesirGlobalDocumentTextData<Guid>.Category), categoryId);
            
            kernelFunctions.Add(kernelFunctionLibrary.GetSemanticDocumentSearchFunction(semanticSearchFilter, MaxTopResults));;
        }
        
        return KernelPluginFactory.CreateFromFunctions(
            pluginName,
            kernelFunctions
        );
    }
}
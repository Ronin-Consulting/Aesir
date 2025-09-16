using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Manages a global collection of documents with capabilities for document loading, deletion,
/// and integration of kernel plugins to support advanced search functionalities.
/// This service leverages vector-based and hybrid document search technologies for optimized operations.
/// </summary>
[Experimental("SKEXP0001")]
public class GlobalDocumentCollectionService : IGlobalDocumentCollectionService
{
    /// <summary>
    /// Defines the upper limit on the number of top matching results to return during document search operations.
    /// </summary>
    private const int MaxTopResults = 50;

    /// <summary>
    /// Provides a vector-based text search mechanism specifically for global document text data.
    /// Enables efficient querying and retrieval of documents based on vector similarity.
    /// </summary>
    private readonly VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>> _globalDocumentVectorSearch;

    /// <summary>
    /// A private field representing a hybrid search service used to perform combined
    /// keyword and vector-based search operations on global document text data.
    /// </summary>
    private readonly IKeywordHybridSearchable<AesirGlobalDocumentTextData<Guid>>? _globalDocumentHybridSearch;

    /// <summary>
    /// Represents a collection of vectorized records used for operations such as storing, managing,
    /// and retrieving global document text data based on vector-based queries and filtering.
    /// </summary>
    private readonly VectorStoreCollection<Guid, AesirGlobalDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// A service responsible for processing and loading PDF documents into a structured text data format
    /// for further use and analysis within the system.
    /// </summary>
    private readonly IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Represents a logger instance used for recording log messages, warnings, errors,
    /// and other diagnostic information within the GlobalDocumentCollectionService class.
    /// </summary>
    private readonly ILogger<GlobalDocumentCollectionService> _logger;

    /// <summary>
    /// Service for managing global document collections, including operations such as
    /// vector-based and hybrid text searches as well as loading PDF documents into the system.
    /// </summary>
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
    /// Asynchronously loads a document from the specified file path into the global document collection.
    /// </summary>
    /// <param name="documentPath">The file path of the document to be loaded.</param>
    /// <param name="fileMetaData">Optional dictionary containing metadata to associate with the document.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
    /// <returns>A task that represents the asynchronous operation of loading the document.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file content type is not supported.</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        // for now enforce only PDFs
        if (!documentPath.ValidFileContentType(FileTypeManager.MimeTypes.Pdf, out var actualContentType))
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
    /// <param name="fileMetaData">A dictionary containing metadata about the file to be deleted, including file name information.</param>
    /// <param name="cancellationToken">A cancellation token to monitor for task cancellation requests during execution.</param>
    /// <returns>Returns a boolean indicating whether the document was successfully deleted.</returns>
    public async Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken = default)
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
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public async Task DeleteDocumentsAsync(IDictionary<string, object>? args,
        CancellationToken cancellationToken = default)
    {
        if (args == null || !args.TryGetValue("CategoryId", out var metaValue))
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
    /// <param name="kernelPluginArguments">
    /// A dictionary containing arguments required for the kernel plugin. Must include keys for "CategoryId" and "PluginName",
    /// as well as an optional key for "PluginDescription".
    /// </param>
    /// <returns>
    /// A kernel plugin instance used for executing document searches.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when one of the required arguments ("CategoryId", "PluginName") is missing from the kernelPluginArguments.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the value for "CategoryId" is null or empty.
    /// </exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if (kernelPluginArguments == null)
            throw new ArgumentException("Kernel plugin args must contain a ConversationId");

        if (!kernelPluginArguments.TryGetValue("PluginName", out var pluginNameValue))
            throw new ArgumentException("Kernel plugin args must contain a PluginName");

        var kernelFunctionLibrary = new KernelFunctionLibrary<Guid, AesirGlobalDocumentTextData<Guid>>(
            _globalDocumentVectorSearch, _globalDocumentHybridSearch, _vectorStoreRecordCollection
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
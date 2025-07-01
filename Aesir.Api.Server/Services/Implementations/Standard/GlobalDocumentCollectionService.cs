using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service for managing global document collections that can be used across the application.
/// Provides functionality for loading documents and creating kernel plugins for document search.
/// This implementation is marked as experimental.
/// </summary>
[Experimental("SKEXP0001")]
public class GlobalDocumentCollectionService : IGlobalDocumentCollectionService
{
    /// <summary>
    /// Vector store for searching text data in global documents
    /// </summary>
    private readonly VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>> _globalDocumentTextSearch;

    /// <summary>
    /// Service for loading PDF documents and converting them to text data
    /// </summary>
    private readonly IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Logger for the GlobalDocumentCollectionService
    /// </summary>
    private readonly ILogger<GlobalDocumentCollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the GlobalDocumentCollectionService class
    /// </summary>
    /// <param name="globalDocumentTextSearch">Vector store for searching text data in global documents</param>
    /// <param name="pdfDataLoader">Service for loading PDF documents</param>
    /// <param name="logger">Logger for the GlobalDocumentCollectionService</param>
    public GlobalDocumentCollectionService(
        VectorStoreTextSearch<AesirGlobalDocumentTextData<Guid>> globalDocumentTextSearch,
        IPdfDataLoaderService<Guid, AesirGlobalDocumentTextData<Guid>> pdfDataLoader,
        ILogger<GlobalDocumentCollectionService> logger
    )
    {
        _globalDocumentTextSearch = globalDocumentTextSearch;
        _pdfDataLoader = pdfDataLoader;
        _logger = logger;
    }

    /// <summary>
    /// Loads a document from the specified path into the global document collection
    /// </summary>
    /// <param name="documentPath">The path to the document to load</param>
    /// <param name="fileMetaData">Optional metadata to associate with the document</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    /// <exception cref="InvalidDataException">Thrown when the file content type is not supported</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        // for now enforce only PDFs
        if (!documentPath.ValidFileContentType(SupportedFileContentTypes.PdfContentType, out var actualContentType))
        {
            throw new InvalidDataException($"Invalid file content type: {actualContentType}");
        }

        var request = new LoadPdfRequest()
        {
            PdfPath = documentPath,
            BatchSize = 2,
            BetweenBatchDelayInMs = 10,
            Metadata = fileMetaData
        };
        await _pdfDataLoader.LoadPdfAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a kernel plugin for searching documents in the global document collection
    /// </summary>
    /// <param name="kernelPluginArguments">Arguments for the kernel plugin, must include CategoryId, PluginName, and PluginDescription</param>
    /// <returns>A kernel plugin that can be used for searching documents</returns>
    /// <exception cref="ArgumentException">Thrown when required arguments are missing</exception>
    /// <exception cref="ArgumentNullException">Thrown when categoryId is null or empty</exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if(kernelPluginArguments == null || !kernelPluginArguments.TryGetValue("CategoryId", out var metaValue))
            throw new ArgumentException("Kernel plugin args must contain a CategoryId");
        
        var categoryId = (string)metaValue;
        if (string.IsNullOrEmpty(categoryId)) throw new ArgumentNullException(nameof(categoryId));
        
        var categoryFilter = new TextSearchFilter();
        categoryFilter.Equality(nameof(AesirGlobalDocumentTextData<Guid>.Category), categoryId);
        var globalDocumentTextSearchOptions = new TextSearchOptions
        {
            Top = 5,
            Filter = categoryFilter
        };
            
        var globalDocumentSearchPlugin = _globalDocumentTextSearch
            .CreateGetTextSearchResults(searchOptions: globalDocumentTextSearchOptions);
        
        if(!kernelPluginArguments.TryGetValue("PluginName", out var pluginNameValue))
            throw new ArgumentException("Kernel plugin args must contain a PluginName");
        
        var pluginName = (string)pluginNameValue;
        
        if(!kernelPluginArguments.TryGetValue("PluginDescription", out var pluginDescriptionValue))
            throw new ArgumentException("Kernel plugin args must contain a PluginDescription");
        
        var pluginDescription = (string)pluginDescriptionValue;
        
        return KernelPluginFactory.CreateFromFunctions(
            pluginName, 
            pluginDescription, 
            [globalDocumentSearchPlugin]
        );
    }
}
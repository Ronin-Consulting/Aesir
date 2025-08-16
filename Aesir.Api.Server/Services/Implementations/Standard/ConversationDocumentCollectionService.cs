using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Google;

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
    private const int MaxTopResults = 50;

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
        if(kernelPluginArguments == null)
            throw new ArgumentException("Kernel plugin args must contain a ConversationId");
        
        const string pluginName = "ChatTools";
        
        var kernelFunctionLibrary = new KernelFunctionLibrary<Guid,AesirConversationDocumentTextData<Guid>>(
            _conversationDocumentVectorSearch, _conversationDocumentHybridSearch
        );
        
        var kernelFunctions = new List<KernelFunction>();

        if (kernelPluginArguments.TryGetValue("EnableWebSearch", out var enableWeSearchValue))
        {
            var enableWebSearch = Convert.ToBoolean(enableWeSearchValue);

            if (enableWebSearch)
            {
                // add web search functions
                // TODO: we meed to get the searchEnginId and apiKey from somewhere else not hard coded when it makes sense
                var googleConnector = new GoogleConnector(
                    searchEngineId: "64cf6ca85e9454a44", //Environment.GetEnvironmentVariable("CSE_ID"),
                    apiKey: "AIzaSyByEQBfXtNjdxIGlpeLRz0C1isORMnsHNU"); //Environment.GetEnvironmentVariable("GOOGLE_KEY"))
        
                kernelFunctions.Add(
                    kernelFunctionLibrary.GetWebSearchFunction(googleConnector)
                );    
            }
        }

        if (kernelPluginArguments.TryGetValue("EnableDocumentSearch", out var enableDocumentSearchValue))
        {
            var enableDocumentSearch = Convert.ToBoolean(enableDocumentSearchValue);
            if (enableDocumentSearch)
            {
                if (!kernelPluginArguments.TryGetValue("ConversationId", out var metaValue))
                    throw new ArgumentException("File metadata must contain a ConversationId property");
                var conversationId = (string)metaValue;
                
                // add image analysis functions
                var imageSearchFilter = new TextSearchFilter();
                imageSearchFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
        
                kernelFunctions.Add(
                    kernelFunctionLibrary.GetImageAnalysisFunction(imageSearchFilter, MaxTopResults)
                );
        
                // text searches
                if (_conversationDocumentHybridSearch != null)
                {
                    var searchOptions = new HybridSearchOptions<AesirConversationDocumentTextData<Guid>>
                    {
                        Filter = data => data.ConversationId == conversationId
                    };
            
                    kernelFunctions.Add(kernelFunctionLibrary.GetHybridDocumentSearchFunction(searchOptions, MaxTopResults));
                }
                else
                {
                    var semanticSearchFilter = new TextSearchFilter();
                    semanticSearchFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
            
                    kernelFunctions.Add(kernelFunctionLibrary.GetSemanticDocumentSearchFunction(semanticSearchFilter, MaxTopResults));;
                }
            }
        }
        
        return KernelPluginFactory.CreateFromFunctions(
            pluginName,
            kernelFunctions.ToArray()
        ); 
    }
}
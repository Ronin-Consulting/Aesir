using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Common.FileTypes;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Google;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service responsible for handling document collections tied to conversations. Provides functionality
/// for processing, managing, and retrieving documents within specific conversational contexts.
/// </summary>
/// <remarks>
/// Utilizes advanced data management techniques such as vector-based text search, hybrid keyword search,
/// and pluggable kernel modules for efficient and intelligent document operations. Intended for experimental
/// use under feature code SKEXP0070.
/// </remarks>
[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    /// <summary>
    /// Defines the upper limit on the number of top results returned by search functions within the service.
    /// </summary>
    /// <remarks>
    /// This constant is used to ensure that the results from various search operations remain manageable
    /// and optimized for performance and usability. It acts as a cap for the number of records retrieved
    /// in scenarios involving text, image, or hybrid searches.
    /// </remarks>
    private const int MaxTopResults = 50;

    /// <summary>
    /// Encapsulates a vector-based semantic search functionality specific to handling
    /// conversation documents within the application.
    /// </summary>
    /// <remarks>
    /// This field is designed to perform similarity searches by utilizing vector embeddings
    /// that measure semantic alignment between user queries and stored conversation documents.
    /// It serves as a core component in enabling advanced search capabilities focused on
    /// conversational data, facilitating efficient and accurate retrieval.
    /// </remarks>
    private readonly VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> _conversationDocumentVectorSearch;

    /// <summary>
    /// Represents an optional hybrid search capability used for retrieving conversation documents
    /// by combining both keyword-based and vector-based search methodologies.
    /// </summary>
    /// <remarks>
    /// This field integrates with the IKeywordHybridSearchable interface and the
    /// AesirConversationDocumentTextData model to enable comprehensive search functionality. It allows for the
    /// effective retrieval of conversation documents through a combination of semantic vector-based search
    /// and traditional keyword-driven techniques. Its primary purpose is to enhance the accuracy and relevance
    /// of document search within the ConversationDocumentCollectionService.
    /// </remarks>
    private readonly IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>?
        _conversationDocumentHybridSearch;

    /// <summary>
    /// Represents a collection of vector store records used for storing and managing conversation-specific text data.
    /// </summary>
    /// <remarks>
    /// This collection is designed to handle data objects of type <see cref="AesirConversationDocumentTextData{TKey}"/>
    /// identified by unique <see cref="System.Guid"/> keys. It plays a pivotal role in supporting operations
    /// such as retrieval, updates, and deletions, ensuring effective management of conversation-related text
    /// information across the service.
    /// </remarks>
    private readonly VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// Handles the loading and processing of PDF data associated with conversation document records.
    /// </summary>
    /// <remarks>
    /// This field serves as the primary mechanism for interacting with PDF content, enabling
    /// the retrieval and management of data tied to conversation document identifiers. It is integral
    /// to operations that require handling textual data stored in PDF format.
    /// </remarks>
    private readonly IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Handles the loading and processing of image-based data for integration into the conversation document collection.
    /// </summary>
    /// <remarks>
    /// Serves as a dependency for managing workflows related to the import and organization of image-related documents.
    /// Ensures compatibility with broader system components and facilitates operations within the document collection service.
    /// </remarks>
    private readonly IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _imageDataLoader;

    /// <summary>
    /// Represents an instance of the service responsible for loading text files and converting them
    /// into structured data that conforms to the application's required document model.
    /// </summary>
    /// <remarks>
    /// This variable is used to manage the lifecycle and operations of text file loading, including
    /// reading, parsing, and structuring raw textual data into objects tailored to the application's
    /// specific needs. It abstracts the complexities of file handling to support higher-level processing
    /// within the document collection system.
    /// </remarks>
    private readonly ITextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _textFileLoader;

    /// <summary>
    /// Provides a logging utility specifically for the <see cref="ConversationDocumentCollectionService"/> class,
    /// enabling insights into the service's runtime behavior and state.
    /// </summary>
    /// <remarks>
    /// This logger is leveraged to capture operational details, debug information, warnings, and errors
    /// encountered during the execution of methods within the service. Its implementation assists in
    /// maintaining observability, system stability, and simplifies the troubleshooting process.
    /// </remarks>
    private readonly ILogger<ConversationDocumentCollectionService> _logger;

    /// <summary>
    /// Service responsible for managing conversation document collections, providing capabilities for vector-based search, optional keyword-based hybrid search,
    /// and managing data loading from diverse file formats, including PDF, image, and text files.
    /// </summary>
    public ConversationDocumentCollectionService(
        VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentVectorSearch,
        IKeywordHybridSearchable<AesirConversationDocumentTextData<Guid>>? conversationDocumentHybridSearch,
        VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> vectorStoreRecordCollection,
        IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> pdfDataLoader,
        IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> imageDataLoader,
        ITextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>> textFileLoader,
        ILogger<ConversationDocumentCollectionService> logger
    )
    {
        _conversationDocumentVectorSearch = conversationDocumentVectorSearch;
        _conversationDocumentHybridSearch = conversationDocumentHybridSearch;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _pdfDataLoader = pdfDataLoader;
        _imageDataLoader = imageDataLoader;
        _textFileLoader = textFileLoader;
        _logger = logger;
    }

    /// <summary>
    /// Asynchronously loads a document into the conversation document collection from the specified path.
    /// </summary>
    /// <param name="documentPath">The full path of the document to load.</param>
    /// <param name="fileMetaData">Optional metadata containing additional attributes of the document.</param>
    /// <param name="cancellationToken">A token to monitor for request cancellation while loading the document.</param>
    /// <returns>A task that represents the asynchronous operation of loading the document.</returns>
    /// <exception cref="InvalidDataException">Thrown when the document format is unsupported or invalid.</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken)
    {
        var allSupportedFileContentTypes = FileTypeManager.DocumentProcessingMimeTypes;

        if (!documentPath.ValidFileContentType(out var actualContentType, allSupportedFileContentTypes))
        {
            throw new InvalidDataException($"Unsupported file content type: {actualContentType}");
        }

        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }

        if (actualContentType == FileTypeManager.MimeTypes.Pdf)
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

        if (FileTypeManager.IsImage(documentPath))
        {
            var imageRequest = new LoadImageRequest()
            {
                ImageLocalPath = documentPath,
                ImageFileName = fileNameMetaData.ToString(),
                Metadata = fileMetaData
            };
            
            await _imageDataLoader.LoadImageAsync(imageRequest, cancellationToken);
        }
        
        if (FileTypeManager.IsTextFile(documentPath))
        {
            var textFileRequest = new LoadTextFileRequest()
            {
                TextFileLocalPath = documentPath,
                TextFileFileName = fileNameMetaData.ToString(),
                Metadata = fileMetaData
            };
            
            await _textFileLoader.LoadTextFileAsync(textFileRequest, cancellationToken);
        }
    }

    /// <summary>
    /// Asynchronously deletes a document from the collection based on the provided metadata and cancellation token.
    /// </summary>
    /// <param name="fileMetaData">
    /// A dictionary containing metadata about the file to be deleted, including required keys like "FileName" and "ConversationId".
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token to observe while waiting for the operation to complete.
    /// </param>
    /// <returns>
    /// A boolean indicating whether the document was successfully deleted.
    /// </returns>
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
    /// Asynchronously deletes documents associated with a specific conversation ID from the vector store.
    /// </summary>
    /// <param name="args">A dictionary containing the "ConversationId" key, which identifies the conversation whose documents will be deleted.</param>
    /// <param name="cancellationToken">A token to observe cancellation requests for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
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
    /// Optional parameters for configuring the kernel plugin. Must include a non-empty "ConversationId" key. It may also contain additional options such as enabling web search or document search.
    /// </param>
    /// <returns>
    /// A configured <see cref="KernelPlugin"/> instance for conversation document search capabilities.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="kernelPluginArguments"/> is null or does not contain a "ConversationId" key.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the value associated with the "ConversationId" key is null or empty.
    /// </exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if (kernelPluginArguments == null)
            throw new ArgumentException("Kernel plugin args must contain a ConversationId");

        const string pluginName = "ChatTools";

        var kernelFunctionLibrary = new KernelFunctionLibrary<Guid, AesirConversationDocumentTextData<Guid>>(
            _conversationDocumentVectorSearch, _conversationDocumentHybridSearch, _vectorStoreRecordCollection
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
        
                // summarize documents
                kernelFunctions.Add(
                    kernelFunctionLibrary.GetSummarizeConversationDocumentFunction(conversationId)
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
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
/// Service implementation responsible for managing collections of documents associated with conversations.
/// Supports operations like loading, deleting, and retrieving documents, providing an interface for efficient
/// and context-aware conversational workflows.
/// </summary>
/// <remarks>
/// The service leverages advanced integration mechanisms such as vector-based storage, keyword hybrid search,
/// and kernel plugins to enable sophisticated document indexing and retrieval. Designed for experimental use
/// under code SKEXP0070.
/// </remarks>
[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    /// <summary>
    /// Specifies the maximum number of top-ranked results that can be obtained during search operations.
    /// </summary>
    /// <remarks>
    /// This constant is utilized across multiple search functionalities to enforce a limit on the
    /// quantity of results retrieved, supporting better performance and precision within the application.
    /// </remarks>
    private const int MaxTopResults = 50;

    /// <summary>
    /// Encapsulates a vector-based semantic search capability tailored for processing
    /// and managing conversation-oriented document data.
    /// </summary>
    /// <remarks>
    /// This member supports high-precision retrieval operations by leveraging vector
    /// embeddings to evaluate the semantic similarity of conversation documents against
    /// user queries. It plays a crucial role in providing intelligent and relevant search
    /// functionalities within conversation data workflows.
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
    /// Represents a collection of vector store records utilized within the service, tied to unique
    /// identifiers of type <see cref="System.Guid"/> and storing instances of
    /// <see cref="AesirConversationDocumentTextData{TKey}"/> for conversation-specific text data.
    /// </summary>
    /// <remarks>
    /// This field serves as a persistent mechanism for managing conversation-related text data,
    /// facilitating operations such as data retrieval, updates, and deletions. It is initialized
    /// and maintained through the service's lifecycle using dependency injection.
    /// </remarks>
    private readonly VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> _vectorStoreRecordCollection;

    /// <summary>
    /// Provides functionality for loading and managing PDF data related to conversation documents.
    /// </summary>
    /// <remarks>
    /// This field is essential for supporting operations that involve retrieving and handling PDF content,
    /// specifically in connection with conversation document identifiers.
    /// </remarks>
    private readonly IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _pdfDataLoader;

    /// <summary>
    /// Provides a mechanism for handling image-based data loading operations within the document collection process.
    /// </summary>
    /// <remarks>
    /// Utilized to process and load image documents, enabling their integration into the conversation document collection.
    /// Acts as a dependency for managing image-specific data import workflows, ensuring compatibility with other system components.
    /// </remarks>
    private readonly IImageDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _imageDataLoader;

    /// <summary>
    /// Service responsible for handling the loading of text files and converting them into structured data
    /// for further processing and storage in the document collection system.
    /// </summary>
    /// <remarks>
    /// Utilized to read text-based documents and produce a representation aligning with
    /// the application's document data model. This service abstracts the underlying
    /// operations necessary for loading and managing text file content.
    /// </remarks>
    private readonly ITextFileLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _textFileLoader;

    /// <summary>
    /// Represents the logging mechanism for the internal operations of the <see cref="ConversationDocumentCollectionService"/> class.
    /// </summary>
    /// <remarks>
    /// Facilitates tracking and recording of service activities, including execution details, diagnostic information,
    /// and error management, ensuring effective observability and troubleshooting.
    /// </remarks>
    private readonly ILogger<ConversationDocumentCollectionService> _logger;

    /// <summary>
    /// Service responsible for managing and searching conversation document collections, utilizing vector-based search and optional hybrid search with keyword support.
    /// This service also includes functionality for loading data from PDF files, images, and text files into the document collection.
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
    /// <returns>A task representing the asynchronous operation of loading the document.</returns>
    /// <exception cref="InvalidDataException">Thrown if the document format is unsupported or invalid.</exception>
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
    /// A dictionary containing metadata about the file to be deleted, including required keys such as "FileName" and "ConversationId".
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token to observe while waiting for the task to complete.
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
    /// Deletes documents associated with a specific conversation ID from the vector store asynchronously.
    /// </summary>
    /// <param name="args">A dictionary containing the required argument "ConversationId", which specifies the conversation whose documents need to be deleted.</param>
    /// <param name="cancellationToken">A token that can be used to observe cancellation requests.</param>
    /// <returns>A task that represents the operation of deleting the specified documents.</returns>
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
        if (kernelPluginArguments == null)
            throw new ArgumentException("Kernel plugin args must contain a ConversationId");

        const string pluginName = "ChatTools";

        var kernelFunctionLibrary = new KernelFunctionLibrary<Guid, AesirConversationDocumentTextData<Guid>>(
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
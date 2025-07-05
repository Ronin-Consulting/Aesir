using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Extensions;
using Aesir.Api.Server.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service for managing and searching conversation-specific document collections.
/// Provides functionality to load documents into conversation contexts and create
/// searchable kernel plugins for querying document content.
/// </summary>
/// <remarks>
/// This service is marked as experimental with the code SKEXP0070.
/// </remarks>
[Experimental("SKEXP0070")]
public class ConversationDocumentCollectionService : IConversationDocumentCollectionService
{
    private readonly VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> _conversationDocumentTextSearch;
    private readonly VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> _vectorStoreRecordCollection;
    private readonly IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> _pdfDataLoader;
    private readonly ILogger<ConversationDocumentCollectionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationDocumentCollectionService"/> class.
    /// </summary>
    /// <param name="conversationDocumentTextSearch">Vector store for searching text in conversation documents.</param>
    /// <param name="pdfDataLoader">Service for loading and processing PDF documents.</param>
    /// <param name="logger">Logger for recording service operations.</param>
    public ConversationDocumentCollectionService(
        VectorStoreTextSearch<AesirConversationDocumentTextData<Guid>> conversationDocumentTextSearch,
        VectorStoreCollection<Guid, AesirConversationDocumentTextData<Guid>> vectorStoreRecordCollection,
        IPdfDataLoaderService<Guid, AesirConversationDocumentTextData<Guid>> pdfDataLoader,
        ILogger<ConversationDocumentCollectionService> logger
    )
    {
        _conversationDocumentTextSearch = conversationDocumentTextSearch;
        _vectorStoreRecordCollection = vectorStoreRecordCollection;
        _pdfDataLoader = pdfDataLoader;
        _logger = logger;
    }
    
    /// <summary>
    /// Loads a document from the specified path into the conversation document collection.
    /// </summary>
    /// <param name="documentPath">Path to the document file to load.</param>
    /// <param name="fileMetaData">Optional metadata associated with the document.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is not a supported PDF document.</exception>
    public async Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData, CancellationToken cancellationToken)
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

    public async Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData, CancellationToken cancellationToken = default)
    {
        if (fileMetaData == null || !fileMetaData.TryGetValue("FileName", out var fileNameMetaData))
        {
            throw new InvalidDataException($"FileName is required metadata.");
        }
        
        if(!fileMetaData.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("File metadata must contain a ConversationId property");
        
        var conversationId = (string)fileMetaData["ConversationId"];
        
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

    public async Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default)
    {
        if(args == null || !args.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("Args must contain a ConversationId property");
        
        var conversationId = (string)args["ConversationId"];
        
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
    /// <param name="kernelPluginArguments">Arguments for the kernel plugin, must contain a ConversationId.</param>
    /// <returns>A <see cref="KernelPlugin"/> configured for searching conversation documents.</returns>
    /// <exception cref="ArgumentException">Thrown when kernelPluginArguments is null or missing ConversationId.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the ConversationId is null or empty.</exception>
    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if(kernelPluginArguments == null || !kernelPluginArguments.TryGetValue("ConversationId", out var metaValue))
            throw new ArgumentException("File metadata must contain a ConversationId property");
        
        var conversationId = (string)metaValue;
        
        var conversationFilter = new TextSearchFilter();
        conversationFilter.Equality(nameof(AesirConversationDocumentTextData<Guid>.ConversationId), conversationId);
        
        var conversationDocumentTextSearchOptions = new TextSearchOptions
        {
            Top = 10, 
            Filter = conversationFilter
        };
            
        var conversationDocumentSearchPlugin = _conversationDocumentTextSearch
            .CreateGetTextSearchResults(searchOptions: conversationDocumentTextSearchOptions);
        
        return KernelPluginFactory.CreateFromFunctions(
            "ChatDocSearch",
            //"Search and extract relevant information from chat conversation documents uploaded by the user during a conversation. It is designed to query the content of these impromptu text-based documents, such as transcripts or message logs, to retrieve details based on user-specified criteria, including keywords, topics, participants, or timestamps. Use this tool when the task involves analyzing or retrieving information from user-uploaded chat conversation documents.",
            [conversationDocumentSearchPlugin]
        );
    }
}
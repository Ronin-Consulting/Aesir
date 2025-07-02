using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service that automatically routes document operations to the appropriate document collection service 
/// based on the DocumentCollectionType provided in the metadata/arguments.
/// </summary>
/// <remarks>
/// This service acts as a router between different document collection implementations,
/// primarily the conversation-specific and global document collections.
/// </remarks>
public class AutoDocumentCollectionService : IDocumentCollectionService
{
    private readonly ILogger<AutoDocumentCollectionService> _logger;
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;
    private readonly IGlobalDocumentCollectionService _globalDocumentCollectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoDocumentCollectionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for this service.</param>
    /// <param name="conversationDocumentCollectionService">The conversation-specific document collection service.</param>
    /// <param name="globalDocumentCollectionService">The global document collection service.</param>
    public AutoDocumentCollectionService(
        ILogger<AutoDocumentCollectionService> logger,
        IConversationDocumentCollectionService conversationDocumentCollectionService,
        IGlobalDocumentCollectionService globalDocumentCollectionService
    )
    {
        _logger = logger;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
        _globalDocumentCollectionService = globalDocumentCollectionService;
    }

    /// <summary>
    /// Loads a document from the specified path into the appropriate document collection based on the DocumentCollectionType metadata.
    /// </summary>
    /// <param name="documentPath">The path to the document to load.</param>
    /// <param name="fileMetaData">Metadata associated with the file, must contain a DocumentCollectionType property.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the fileMetaData is null or does not contain a valid DocumentCollectionType.</exception>
    public Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        if(fileMetaData == null || !fileMetaData.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("File metadata must contain a DocumentCollectionType property");

        var documentCollectionType = (DocumentCollectionType)metaValue;
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.LoadDocumentAsync(
                documentPath, fileMetaData, cancellationToken),
            DocumentCollectionType.Global => _globalDocumentCollectionService.LoadDocumentAsync(documentPath,
                fileMetaData, cancellationToken),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }

    public Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData, CancellationToken cancellationToken = default)
    {
        if(fileMetaData == null || !fileMetaData.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("File metadata must contain a DocumentCollectionType property");
        
        var documentCollectionType = (DocumentCollectionType)metaValue;
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.DeleteDocumentAsync(fileMetaData, cancellationToken),
            DocumentCollectionType.Global => _globalDocumentCollectionService.DeleteDocumentAsync(fileMetaData, cancellationToken),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }

    public KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null)
    {
        if(kernelPluginArguments == null || !kernelPluginArguments.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("Kernel arguments must contain a DocumentCollectionType property");
        
        var documentCollectionType = (DocumentCollectionType)metaValue;
        
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.GetKernelPlugin(kernelPluginArguments),
            DocumentCollectionType.Global => _globalDocumentCollectionService.GetKernelPlugin(kernelPluginArguments),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }
}
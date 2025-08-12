using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Service responsible for routing document operations to the corresponding document collection service
/// based on specified metadata or parameters.
/// </summary>
/// <remarks>
/// This service handles the delegation of document processing tasks between conversation-specific
/// and global document collection implementations.
/// </remarks>
public class AutoDocumentCollectionService : IDocumentCollectionService
{
    /// <summary>
    /// Logger instance used for capturing and recording logs related to the operations
    /// and activities within the <see cref="AutoDocumentCollectionService"/> class.
    /// </summary>
    /// <remarks>
    /// This logger is primarily used to provide diagnostic and operational details such
    /// as errors, warnings, or informational messages during runtime.
    /// </remarks>
    private readonly ILogger<AutoDocumentCollectionService> _logger;

    /// <summary>
    /// Represents an instance of the conversation document collection service,
    /// utilized for managing operations specific to conversation-bound documents
    /// such as loading, deleting, or manipulating conversation-scoped document data.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    /// <summary>
    /// An instance of <see cref="IGlobalDocumentCollectionService"/> used to manage
    /// document collection operations at a global scope. Provides functionality for
    /// loading, deleting, and managing documents related to global document collections.
    /// </summary>
    private readonly IGlobalDocumentCollectionService _globalDocumentCollectionService;

    /// <summary>
    /// Represents a service that automatically directs document-related operations to the appropriate
    /// implementation of a document collection service.
    /// </summary>
    /// <remarks>
    /// This service serves as a router to manage document-related operations by delegating them
    /// to either a conversation-specific document collection service or a global document collection
    /// service based on the provided metadata or arguments.
    /// </remarks>
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
    /// Loads a document from the specified path into the appropriate document collection, based on the DocumentCollectionType contained in the file metadata.
    /// </summary>
    /// <param name="documentPath">The file path of the document to load.</param>
    /// <param name="fileMetaData">The metadata associated with the file, which must include a valid DocumentCollectionType property.</param>
    /// <param name="cancellationToken">A token for propagating cancellation notifications.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the fileMetaData is null or does not contain a valid DocumentCollectionType property.</exception>
    public Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null,
        CancellationToken cancellationToken = default)
    {
        if (fileMetaData == null || !fileMetaData.TryGetValue("DocumentCollectionType", out var metaValue))
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

    /// <summary>
    /// Deletes a document based on the provided metadata and document collection type.
    /// </summary>
    /// <param name="fileMetaData">The metadata of the file, which must include a "DocumentCollectionType" property to determine the document collection.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete. Defaults to none.</param>
    /// <returns>A task representing the asynchronous operation, containing a boolean value indicating whether the document was successfully deleted.</returns>
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

    /// <summary>
    /// Deletes documents based on the provided arguments and document collection type.
    /// </summary>
    /// <param name="args">A dictionary containing the arguments for the operation, including the mandatory "DocumentCollectionType" property.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default)
    {
        if(args == null || !args.TryGetValue("DocumentCollectionType", out var metaValue))
            throw new ArgumentException("Args must contain a DocumentCollectionType property");
        
        var documentCollectionType = (DocumentCollectionType)metaValue;
        return documentCollectionType switch
        {
            DocumentCollectionType.Conversation => _conversationDocumentCollectionService.DeleteDocumentsAsync(args, cancellationToken),
            DocumentCollectionType.Global => _globalDocumentCollectionService.DeleteDocumentsAsync(args, cancellationToken),
            _ => throw new ArgumentException("Invalid DocumentCollectionType")
        };
    }

    /// <summary>
    /// Retrieves a Kernel plugin instance based on the specified document collection type.
    /// </summary>
    /// <param name="kernelPluginArguments">An optional dictionary of arguments, which must include a "DocumentCollectionType" key to determine the document collection type (e.g., Conversation or Global).</param>
    /// <returns>A Kernel plugin corresponding to the specified document collection type.</returns>
    /// <exception cref="ArgumentException">Thrown if kernelPluginArguments is null, does not contain the "DocumentCollectionType" key, or contains an invalid document collection type.</exception>
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
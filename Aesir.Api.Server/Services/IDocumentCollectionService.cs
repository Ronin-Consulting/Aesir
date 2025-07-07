using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides document collection functionality for managing document storage and retrieval with kernel plugin integration.
/// </summary>
public interface IDocumentCollectionService
{
    /// <summary>
    /// Loads a document from the specified path into the collection.
    /// </summary>
    /// <param name="documentPath">The path to the document to load.</param>
    /// <param name="fileMetaData">Optional metadata to associate with the document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadDocumentAsync(string documentPath, IDictionary<string, object>? fileMetaData = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a document from the collection based on metadata.
    /// </summary>
    /// <param name="fileMetaData">The metadata used to identify the document to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns true if the document was deleted successfully.</returns>
    Task<bool> DeleteDocumentAsync(IDictionary<string, object>? fileMetaData,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple documents from the collection based on arguments.
    /// </summary>
    /// <param name="args">The arguments used to identify the documents to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteDocumentsAsync(IDictionary<string, object>? args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a kernel plugin for document operations.
    /// </summary>
    /// <param name="kernelPluginArguments">Optional arguments for the kernel plugin.</param>
    /// <returns>A kernel plugin configured for document operations.</returns>
    KernelPlugin GetKernelPlugin(IDictionary<string, object>? kernelPluginArguments = null);
}

/// <summary>
/// Specifies the type of document collection.
/// </summary>
public enum DocumentCollectionType
{
    /// <summary>
    /// Documents associated with a specific conversation.
    /// </summary>
    Conversation,
    /// <summary>
    /// Documents available globally across all conversations.
    /// </summary>
    Global
}

/// <summary>
/// Provides arguments for global document collection operations.
/// </summary>
public class GlobalDocumentCollectionArgs : Dictionary<string, object>
{
    /// <summary>
    /// Gets the default instance of global document collection arguments.
    /// </summary>
    public static GlobalDocumentCollectionArgs Default => new();
    
    /// <summary>
    /// Gets the document collection type.
    /// </summary>
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalDocumentCollectionArgs"/> class.
    /// </summary>
    private GlobalDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Global;
    }

    /// <summary>
    /// Sets the category identifier for the document collection.
    /// </summary>
    /// <param name="categoryId">The category identifier.</param>
    public void SetCategoryId(string categoryId)
    {
        this["CategoryId"] = categoryId;   
    }
}

/// <summary>
/// Provides arguments for conversation-specific document collection operations.
/// </summary>
public class ConversationDocumentCollectionArgs : Dictionary<string, object>
{
    /// <summary>
    /// Gets the default instance of conversation document collection arguments.
    /// </summary>
    public static ConversationDocumentCollectionArgs Default => new();
    
    /// <summary>
    /// Gets the document collection type.
    /// </summary>
    public DocumentCollectionType DocumentCollectionType => (DocumentCollectionType) this["DocumentCollectionType"];
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationDocumentCollectionArgs"/> class.
    /// </summary>
    private ConversationDocumentCollectionArgs()
    {
        this["DocumentCollectionType"] = DocumentCollectionType.Conversation;
    }
    
    /// <summary>
    /// Sets the conversation identifier for the document collection.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    public void SetConversationId(string conversationId)
    {
        this["ConversationId"] = conversationId;   
    }
}

/// <summary>
/// Provides constants for supported file content types.
/// </summary>
public static class SupportedFileContentTypes
{
    /// <summary>
    /// The MIME type for PDF files.
    /// </summary>
    public static readonly string PdfContentType = "application/pdf";
}
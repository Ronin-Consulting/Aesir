namespace Aesir.Api.Server.Services.Implementations.Standard;

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
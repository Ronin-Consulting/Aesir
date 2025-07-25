namespace Aesir.Api.Server.Services.Implementations.Standard;

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
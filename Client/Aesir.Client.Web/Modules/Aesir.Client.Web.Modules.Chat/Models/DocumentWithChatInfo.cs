namespace Aesir.Client.Web.Modules.Chat.Models;

/// <summary>
/// Represents a document with its associated chat session metadata.
/// Used for the Documents view to display documents with their chat context.
/// </summary>
public class DocumentWithChatInfo
{
    /// <summary>
    /// The underlying conversation file information.
    /// </summary>
    public required ConversationFile File { get; init; }

    /// <summary>
    /// The conversation/session ID this document belongs to.
    /// Extracted from the file path.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// The chat session title. Null if session not found.
    /// </summary>
    public string? ChatTitle { get; init; }

    /// <summary>
    /// Whether the associated chat is starred.
    /// </summary>
    public bool IsChatStarred { get; init; }

    /// <summary>
    /// The last updated time of the chat session.
    /// </summary>
    public DateTimeOffset? ChatUpdatedAt { get; init; }

    /// <summary>
    /// Gets whether the associated chat exists.
    /// </summary>
    public bool HasValidChat => ChatTitle != null;
}

/// <summary>
/// Options for document deletion from the Documents view.
/// </summary>
public enum DocumentDeleteOption
{
    /// <summary>Delete only the document, keep the chat.</summary>
    DocumentOnly,

    /// <summary>Delete both the document and its associated chat.</summary>
    DocumentAndChat
}

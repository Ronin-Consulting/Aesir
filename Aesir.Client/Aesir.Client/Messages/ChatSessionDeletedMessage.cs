using System;

namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message sent when a chat session is deleted.
/// </summary>
public class ChatSessionDeletedMessage
{
    /// <summary>
    /// Gets or sets the Id of the Chat Session that was deleted.
    /// </summary>
    /// <remarks>
    /// This property holds the Id of the Chat Session that was deleted.
    /// The value is provided when a chat session is deleted.
    /// </remarks>
    public required Guid? ChatSessionId { get; set; }
}
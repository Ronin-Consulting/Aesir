using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents a single chunk in a streamed chat completion response.
/// </summary>
public class AesirChatStreamedResult
{
    /// <summary>
    /// Gets or sets the unique identifier for this stream chunk.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the title of the chat session.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Chat Session (Server)";

    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public string ConversationId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the incremental message content for this stream chunk.
    /// </summary>
    [JsonPropertyName("delta")]
    public AesirChatMessage Delta { get; set; } = null!;
}
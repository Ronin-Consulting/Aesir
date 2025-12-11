using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for streamed chat completion results.
/// </summary>
public abstract class AesirChatStreamedResultBase
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
    public string Title { get; set; } = null!;

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

    /// <summary>
    /// Gets or sets whether this chunk contains thinking/reasoning content.
    /// </summary>
    [JsonPropertyName("is_thinking")]
    public bool IsThinking { get; set; }

    /// <summary>
    /// Gets or sets the type of this stream event for proper client handling.
    /// Defaults to Text for backward compatibility.
    /// </summary>
    [JsonPropertyName("event_type")]
    public StreamEventType EventType { get; set; } = StreamEventType.Text;

    /// <summary>
    /// Gets or sets tool call information when EventType is ToolCallStart or ToolCallResult.
    /// Null for Text and Thinking events.
    /// </summary>
    [JsonPropertyName("tool_call")]
    public AesirToolCallInfo? ToolCall { get; set; }
}
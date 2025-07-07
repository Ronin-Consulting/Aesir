using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for chat sessions containing common properties.
/// </summary>
public abstract class AesirChatSessionBase
{
    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the chat session.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the chat session was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the conversation containing the message history.
    /// </summary>
    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; } = null!;

    /// <summary>
    /// Gets or sets the title of the chat session.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;
}
using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents a conversation containing a collection of chat messages.
/// </summary>
public class AesirConversation
{
    /// <summary>
    /// Gets or sets the unique identifier for the conversation.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of messages in the conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public IList<AesirChatMessage> Messages { get; set; } = new List<AesirChatMessage>();
}
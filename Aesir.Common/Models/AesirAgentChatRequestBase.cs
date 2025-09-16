using System.Globalization;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for agent chat completion requests containing common properties.
/// </summary>
public class AesirAgentChatRequestBase
{
    /// <summary>
    /// Gets or sets the unique identifer of the agent to use for the chat completion.
    /// </summary>
    [JsonPropertyName("agent_id")]
    public Guid? AgentId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the title of the chat session.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = "title-not-set";

    /// <summary>
    /// Gets or sets the timestamp when the chat session was last updated.
    /// </summary>
    [JsonPropertyName("chat_session_updated_at")]
    public DateTimeOffset ChatSessionUpdatedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// Gets or sets the conversation containing the message history.
    /// </summary>
    [JsonPropertyName("conversation")]
    public AesirConversation Conversation { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the user identifier making the request.
    /// </summary>
    [JsonPropertyName("user")]
    public string User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the client's current date and time as a formatted string.
    /// </summary>
    [JsonPropertyName("client_datetime")]
    public string ClientDateTime { get; set; } = DateTime.Now.ToString("F", new CultureInfo("en-US"));
}
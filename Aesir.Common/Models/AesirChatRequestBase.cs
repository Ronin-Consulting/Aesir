using System.Globalization;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for chat completion requests containing common properties.
/// </summary>
public class AesirChatRequestBase
{
    /// <summary>
    /// Gets or sets the model to use for the chat completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "model-not-set";

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
    /// Gets or sets the temperature parameter for controlling randomness in responses.
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets the top-p parameter for nucleus sampling.
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate in the response.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

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

    /// <summary>
    /// Gets or sets a value indicating whether "thinking" behavior should be enabled for this chat request.
    /// </summary>
    [JsonPropertyName("enable_thinking")]
    public bool? EnableThinking { get; set; }
}
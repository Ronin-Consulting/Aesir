using System.Text.Json.Serialization;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents the result of a chat completion request including conversation data and token usage statistics.
/// </summary>
public class AesirChatResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the chat session.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the conversation containing the complete message history.
    /// </summary>
    [JsonPropertyName("conversation")]
    public AesirConversation AesirConversation { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of tokens used in the prompt.
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens used in the request and response.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of tokens generated in the completion.
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
}
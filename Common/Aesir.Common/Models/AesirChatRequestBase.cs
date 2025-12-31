using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Common.Models;

/// <summary>
/// Base class for chat completion requests containing common properties.
/// </summary>
public class AesirChatRequestBase : ChatRequestBase
{
    /// <summary>
    /// Gets or sets the model to use for the chat completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "model-not-set";
    
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
    /// Gets or sets the prompt persona to use for system prompt selection.
    /// </summary>
    [JsonPropertyName("chat_prompt_persona")]
    public PromptPersona? ChatPromptPersona { get; set; }

    /// <summary>
    /// Gets or sets custom prompt content when using the Custom persona.
    /// </summary>
    [JsonPropertyName("chat_custom_prompt_content")]
    public string? ChatCustomPromptContent { get; set; }
}
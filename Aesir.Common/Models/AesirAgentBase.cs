using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Common.Models;

public class AesirAgentBase
{
    /// <summary>
    /// Gets or sets the id of the agent
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the agent
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the agent
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the id of the chat (and embedding) model's inference engine
    /// </summary>
    [JsonPropertyName("chat_inference_engine_id")]
    public Guid? ChatInferenceEngineId { get; set; }

    /// <summary>
    /// Gets or sets the name of the chat model
    /// </summary>
    [JsonPropertyName("chat_model")]
    public string? ChatModel { get; set; }

    /// <summary>
    /// Gets or sets the temperature chat model parameter for controlling randomness.
    /// </summary>
    [JsonPropertyName("chat_temperature")]
    public double? ChatTemperature { get; set; }

    /// <summary>
    /// Gets or sets the top-p chat model parameter for nucleus sampling.
    /// </summary>
    [JsonPropertyName("chat_top_p")]
    public double? ChatTopP { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("chat_max_tokens")]
    public int? ChatMaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the prompt persona used by the agent
    /// </summary>
    [JsonPropertyName("chat_prompt_persona")]
    public PromptPersona? ChatPromptPersona { get; set; }

    /// <summary>
    /// Gets or sets the custom prompt content used by the agent when the PromptPerson is Custom
    /// </summary>
    [JsonPropertyName("chat_custom_prompt_content")]
    public string? ChatCustomPromptContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the agent is allowed to perform active thinking or reasoning.
    /// </summary>
    [JsonPropertyName("allow_thinking")]
    public bool? AllowThinking { get; set; }

    /// <summary>
    /// Gets or sets the "think" value, which can represent a boolean or a string indication such as "high", "medium", or "low".
    /// </summary>
    [JsonPropertyName("think_value")]
    public ThinkValue? ThinkValue { get; set; }
}
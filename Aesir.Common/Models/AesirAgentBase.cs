using System.ComponentModel;
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
    /// Gets or sets the id of the vision model's inference engine
    /// </summary>
    [JsonPropertyName("vision_inference_engine_id")]
    public Guid? VisionInferenceEngineId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the vision model
    /// </summary>
    [JsonPropertyName("vision_model")]
    public string? VisionModel { get; set; }
    
    /// <summary>
    /// Gets or sets the prompt persona used by the agent
    /// </summary>
    [JsonPropertyName("prompt_persona")]
    public PromptPersona? PromptPersona { get; set; }
    
    /// <summary>
    /// Gets or sets the custom prompt content used by the agent when the PromptPerson is Custom
    /// </summary>
    [JsonPropertyName("custom_prompt_content")]
    public string? CustomPromptContent { get; set; }
}
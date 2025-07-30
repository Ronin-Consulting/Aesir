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
    /// Gets or sets the name of the chat model
    /// </summary>
    [JsonPropertyName("chat_model")]
    public string? ChatModel { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the embedding model
    /// </summary>
    [JsonPropertyName("embedding_model")]
    public string? EmbeddingModel { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the vision model
    /// </summary>
    [JsonPropertyName("vision_model")]
    public string? VisionModel { get; set; }
    
    /// <summary>
    /// Gets or sets the souece of the model
    /// </summary>
    [JsonPropertyName("source")]
    public ModelSource? Source { get; set; }
    
    /// <summary>
    /// Gets or sets the prompt used by the agent
    /// </summary>
    [JsonPropertyName("prompt")]
    public PromptContext? Prompt { get; set; }
}

public enum ModelSource
{
    [Description("OpenAI")]
    OpenAI,
    [Description("Ollama")]
    Ollama
}
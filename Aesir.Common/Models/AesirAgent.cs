using System.ComponentModel;
using System.Text.Json.Serialization;
using Aesir.Common.Prompts;

namespace Aesir.Common.Models;

public class AesirAgent
{
    /// <summary>
    /// Gets or sets the name of the agent
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the chat model
    /// </summary>
    [JsonPropertyName("chat-model")]
    public string? ChatModel { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the embedding model
    /// </summary>
    [JsonPropertyName("embedding-model")]
    public string? EmbeddingModel { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the vision model
    /// </summary>
    [JsonPropertyName("vision-model")]
    public string? VisionModel { get; set; }
    
    /// <summary>
    /// Gets or sets the souece of the model
    /// </summary>
    [JsonPropertyName("source")]
    public ModelSource? Source { get; set; }

    /// <summary>
    /// Gets or sets the tools used by the agent
    /// </summary>
    [JsonPropertyName("tools")]
    public IEnumerable<string> Tools { get; set; } = new List<string>();
    
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
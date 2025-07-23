using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirAgent
{
    /// <summary>
    /// Gets or sets the name of the agent
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the chat model
    /// </summary>
    [JsonPropertyName("chat-model")]
    public string ChatModel { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the embedding model
    /// </summary>
    [JsonPropertyName("embedding-model")]
    public string EmbeddingModel { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the vision model
    /// </summary>
    [JsonPropertyName("vision-model")]
    public string VisionModel { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the model
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the model
    /// </summary>
    [JsonPropertyName("tools")]
    public string Tools { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the name of the model
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = null!;
}
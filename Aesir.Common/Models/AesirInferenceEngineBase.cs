using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

public class AesirInferenceEngineBase
{   
    /// <summary>
    /// Gets or sets the id of the inference engine
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the inference engine
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// Gets or sets the description of the inference engine
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the inference engine
    /// </summary>
    [JsonPropertyName("type")]
    public InferenceEngineType? Type { get; set; }

    /// <summary>
    /// Gets or sets the configuration of the inference engine
    /// </summary>
    [JsonPropertyName("configuration")]
    public IDictionary<string, string?> Configuration { get; set; }
}

public enum InferenceEngineType
{
    [Description("Ollama")]
    Ollama,
    [Description("Open AI Compatible")]
    OpenAICompatible
}
using System.Text.Json.Serialization;
using OllamaSharp.Models;

namespace Aesir.Api.Server.Models;

/// <summary>
/// Represents information about an AI model including its capabilities and metadata.
/// </summary>
public class AesirModelInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the organization or entity that owns the model.
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the date and time when the model was created.
    /// </summary>
    [JsonPropertyName("created")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports chat completions.
    /// </summary>
    [JsonPropertyName("is_chat_model")]
    public bool IsChatModel { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports text embeddings.
    /// </summary>
    [JsonPropertyName("is_embedding_model")]
    public bool IsEmbeddingModel { get; set; }

    /// <summary>
    /// Gets or sets additional details about the AI model, such as its parent model,
    /// format, family, and other descriptive metadata.
    /// </summary>
    [JsonPropertyName("details")]
    public AesirModelDetails? Details { get; set; }
}

public class AesirModelDetails
{
    /// <summary>
    /// Gets or sets the name of the parent model on which the model is based.
    /// </summary>
    [JsonPropertyName("parent_model")]
    public string? ParentModel { get; set; }

    /// <summary>Gets or sets the format of the model file.</summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>Gets or sets the family of the model.</summary>
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    /// <summary>Gets or sets the families of the model.</summary>
    [JsonPropertyName("families")]
    public string[]? Families { get; set; }

    /// <summary>Gets or sets the number of parameters in the model.</summary>
    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    /// <summary>Gets or sets the quantization level of the model.</summary>
    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
    
    internal static AesirModelDetails NewFrom(Details ollamaDetails) =>
        new()
        {
            ParentModel = ollamaDetails.ParentModel,
            Format = ollamaDetails.Format,
            Family = ollamaDetails.Family,
            Families = ollamaDetails.Families,
            ParameterSize = ollamaDetails.ParameterSize,
            QuantizationLevel = ollamaDetails.QuantizationLevel
        };
}
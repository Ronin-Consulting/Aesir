using System.Text.Json.Serialization;

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
}
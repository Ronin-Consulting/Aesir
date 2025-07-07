using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines a service for retrieving model information asynchronously.
/// </summary>
public interface IModelService
{
    /// Asynchronously retrieves a collection of available Aesir models.
    /// This method makes an HTTP request to fetch model information and returns
    /// a collection of AesirModelInfo objects representing the models. If an error
    /// occurs during the request, it logs the exception and rethrows it.
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes,
    /// it contains a collection of AesirModelInfo objects representing the available models.
    /// </returns>
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync();
}

/// Represents metadata about an Aesir model. This class contains
/// properties that describe various characteristics of a model,
/// including its identifier, ownership, creation date, and supported
/// features such as chat or embedding capabilities.
public class AesirModelInfo
{
    /// Gets or sets the unique identifier of the model. This property represents
    /// the unique ID assigned to the model, commonly used for referencing and
    /// identification purposes within the service.
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the entity that owns the model.
    /// </summary>
    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = null!;

    /// Represents the date and time when the associated entity was created.
    /// This property is populated with the creation timestamp of the object,
    /// allowing for tracking and auditing purposes within the application.
    [JsonPropertyName("created")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model is designed for chat-based operations.
    /// </summary>
    [JsonPropertyName("is_chat_model")]
    public bool IsChatModel { get; set; }

    /// Indicates whether the model is an embedding model.
    /// This property is represented as a boolean value and is used to identify
    /// models that are specifically designed for embedding tasks in natural
    /// language processing or other related fields.
    [JsonPropertyName("is_embedding_model")]
    public bool IsEmbeddingModel { get; set; }
}
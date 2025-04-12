using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IModelService
{
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync();
}

public class AesirModelInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("owned_by")]
    public string OwnedBy { get; set; } = null!;
    
    [JsonPropertyName("created")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("is_chat_model")]
    public bool IsChatModel { get; set; }
    
    [JsonPropertyName("is_embedding_model")]
    public bool IsEmbeddingModel { get; set; }
}
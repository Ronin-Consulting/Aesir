using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OllamaSharp;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ChatService> logger,
    OllamaApiClient api,
    IConfiguration configuration)
    : IModelsService
{
    private readonly ILogger<ChatService> _logger = logger;

    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        // only ever one embedding model
        var embeddingModel = configuration.GetValue<string>("Inference:EmbeddingModel") ?? string.Empty;
        
        // can be many chat models
        var models = (configuration.GetValue<IEnumerable<string>>("Inference:ChatModels") ?? Array.Empty<string>()).ToList();
        models.Add(embeddingModel);
        
        return (await api.ListLocalModelsAsync()).Select(m => new AesirModelInfo
        {
            Id = m.Name,
            OwnedBy = "Aesir",
            CreatedAt = m.ModifiedAt,
            IsChatModel = embeddingModel != m.Name,
            IsEmbeddingModel = embeddingModel == m.Name
        });
    }
}
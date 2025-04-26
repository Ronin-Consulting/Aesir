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
        var embeddingModelName = configuration.GetValue<string>("Inference:Ollama:EmbeddingModel") 
                             ?? throw new InvalidOperationException("No embedding model configured");
        
        var allowedModelNames = (configuration.GetValue<IEnumerable<string>>("Inference:Ollama:ChatModels") 
                      ?? throw new InvalidOperationException("No chat models configured")).ToList();

        // get ollama models loaded
        var ollamaModels = (await api.ListLocalModelsAsync()).ToList();
        
        if(ollamaModels.Count == 0)
            throw new InvalidOperationException("No models found");
        
        if(!ollamaModels.Any(m => m.Name.Equals(embeddingModelName, StringComparison.InvariantCultureIgnoreCase)))
            throw new InvalidOperationException("Embedding model not found");
        
        var models = new List<AesirModelInfo>();
        
        // add embedding model
        var embeddingModel = ollamaModels.First(m => m.Name.Equals(embeddingModelName, StringComparison.InvariantCultureIgnoreCase));
        models.Add(new AesirModelInfo
        {
            Id = embeddingModel.Name,
            OwnedBy = "Aesir",
            CreatedAt = embeddingModel.ModifiedAt,
            IsChatModel = false,
            IsEmbeddingModel = true
        });
        
        var allowedModels = 
            ollamaModels.Where(m => allowedModelNames.Contains(m.Name)).ToList();
        
        if(allowedModels.Count == 0)
            throw new InvalidOperationException("No chat models not founds");
        
        models.AddRange(allowedModels.Select(m => new AesirModelInfo
        {
            Id = m.Name,
            OwnedBy = "Aesir",
            CreatedAt = m.ModifiedAt,
            IsChatModel = true,
            IsEmbeddingModel = false
        }));
        
        return models;
    }
}
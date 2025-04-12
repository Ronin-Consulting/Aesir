using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ChatService> logger,
    OpenAIClient api,
    IConfiguration configuration)
    : IModelsService
{
    private readonly ILogger<ChatService> _logger = logger;

    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        var models = await api.ModelsEndpoint.GetModelsAsync();
        
        var allowedChatModels = configuration.GetValue<IEnumerable<string>>("OpenAI:AllowedChatModels") 
            ?? new List<string> { "gpt-4", "gpt-3.5-turbo" };
        
        var embeddingModel = configuration.GetValue<string>("OpenAI:EmbeddingModel") 
            ?? "text-embedding-ada-002";
        
        return models.Select(model => new AesirModelInfo
        {
            Id = model.Id,
            OwnedBy = model.OwnedBy,
            CreatedAt = model.CreatedAt.DateTime,
            IsChatModel = allowedChatModels.Contains(model.Id),
            IsEmbeddingModel = model.Id == embeddingModel
        }).Where(m => m.IsChatModel || m.IsEmbeddingModel);
    }
}

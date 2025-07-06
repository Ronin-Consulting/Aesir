using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IModelsService
{
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync();
    
    Task UnloadChatModelAsync();
    
    Task UnloadEmbeddingModelAsync();
    
    Task UnloadVisionModelAsync();
}

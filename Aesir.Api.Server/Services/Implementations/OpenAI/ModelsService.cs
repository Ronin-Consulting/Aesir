using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OpenAI_API;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ModelsService : IModelsService
{
    private readonly ILogger<ModelsService> _logger;
    private readonly OpenAIAPI _api;
    private readonly IConfiguration _configuration;

    public ModelsService(
        ILogger<ModelsService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        var apiKey = configuration.GetValue<string>("Inference:OpenAI:ApiKey") ?? 
                    throw new InvalidOperationException("OpenAI API key is not configured");
        _api = new OpenAIAPI(apiKey);
    }

    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        var allowedModels = _configuration.GetValue<IEnumerable<string>>("Inference:OpenAI:AllowedChatModels") ?? 
                           Array.Empty<string>();

        var models = new List<AesirModelInfo>();
        
        foreach (var model in allowedModels)
        {
            models.Add(new AesirModelInfo
            {
                Id = model,
                OwnedBy = "OpenAI",
                CreatedAt = DateTime.UtcNow,
                IsChatModel = true,
                IsEmbeddingModel = false
            });
        }

        return models;
    }
}

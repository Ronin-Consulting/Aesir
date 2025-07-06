using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ModelsService> logger,
    OpenAIClient client,
    IConfiguration configuration)
    : IModelsService
{
    private readonly ILogger<ModelsService> _logger = logger;

    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        var result = new List<AesirModelInfo>();

        // Getting embedding model from configuration
        var embeddingModel = configuration.GetValue<string>("Inference:OpenAI:EmbeddingModel") ?? "text-embedding-3-small";

        // Getting chat models from configuration
        var configuredChatModels = configuration.GetSection("Inference:OpenAI:ChatModels").Get<string[]>() ??
            ["gpt-4o", "gpt-4-turbo"];

        // Get available models from API
        try
        {
            var response = await client.GetOpenAIModelClient().GetModelsAsync();

            foreach (var model in response.Value)
            {
                // Check if this is one of our configured models
                var isChatModel = configuredChatModels.Contains(model.Id);
                var isEmbeddingModel = embeddingModel == model.Id;

                // Only include models that are configured for use
                if (isChatModel || isEmbeddingModel)
                {
                    result.Add(new AesirModelInfo
                    {
                        Id = model.Id,
                        OwnedBy = model.OwnedBy,
                        CreatedAt = model.CreatedAt.DateTime,
                        IsChatModel = isChatModel,
                        IsEmbeddingModel = isEmbeddingModel
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models from OpenAI API");

            throw;
        }

        return result;
    }

    public Task UnloadChatModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    public Task UnloadEmbeddingModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    public Task UnloadVisionModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }
}
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OllamaSharp;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Provides model management services using the Ollama backend.
/// </summary>
/// <param name="logger">The logger instance for recording operations.</param>
/// <param name="api">The Ollama API client for model operations.</param>
/// <param name="configuration">The application configuration for model settings.</param>
[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ChatService> logger,
    OllamaApiClient api,
    IConfiguration configuration)
    : IModelsService
{
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        // only ever one embedding model
        var embeddingModelName = configuration.GetValue<string>("Inference:Ollama:EmbeddingModel")
                             ?? throw new InvalidOperationException("No embedding model configured");

        var allowedModelNames = (configuration.GetSection("Inference:Ollama:ChatModels").Get<string[]>()
                      ?? throw new InvalidOperationException("No chat models configured")).ToList();

        // get ollama models loaded
        var ollamaModels = (await api.ListLocalModelsAsync()).ToList();

        if (ollamaModels.Count == 0)
            throw new InvalidOperationException("No models found");

        if (!ollamaModels.Any(m => m.Name.Equals(embeddingModelName, StringComparison.InvariantCultureIgnoreCase)))
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

        if (allowedModels.Count == 0)
            throw new InvalidOperationException("No chat models founds");

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

    public async Task UnloadChatModelAsync()
    {
        var allowedModelNames = (configuration.GetSection("Inference:Ollama:ChatModels").Get<string[]>()
                                 ?? throw new InvalidOperationException("No chat models configured")).ToList();

        await Parallel.ForEachAsync(allowedModelNames, async (modelName, token) =>
        {
            try
            {
                await api.RequestModelUnloadAsync(modelName, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unload of chat model had error: {Error}", ex);
            }
        });
    }

    public async Task UnloadEmbeddingModelAsync()
    {
        var modelName = configuration.GetValue<string>("Inference:Ollama:EmbeddingModel")
                                 ?? throw new InvalidOperationException("No embedding model configured");
        
        try
        {
            await api.RequestModelUnloadAsync(modelName);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unload of embedding model had error: {Error}", ex);
        }
    }

    public async Task UnloadVisionModelAsync()
    {
        var modelName = configuration.GetValue<string>("Inference:Ollama:VisionModel")
                        ?? throw new InvalidOperationException("No vision model configured");

        try
        {
            await api.RequestModelUnloadAsync(modelName);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Unload of vision model had error: {Error}", ex);
        }
    }
}
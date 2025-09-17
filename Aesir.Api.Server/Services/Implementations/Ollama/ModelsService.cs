using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using OllamaSharp;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Handles the management and lifecycle operations of models through the Ollama API backend.
/// </summary>
/// <param name="logger">The logging service for operation tracking and reporting.</param>
/// <param name="api">The client for interacting with the Ollama API endpoints.</param>
/// <param name="configuration">The configuration interface containing application settings.</param>
[Experimental("SKEXP0070")]
public class ModelsService(
    string serviceId,
    ILogger<ModelsService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider)
    : IModelsService
{
    /// <summary>
    /// Retrieves a collection of AI models based on the specified category.
    /// </summary>
    /// <param name="category">
    /// An optional category of models to filter the results. If null, all models are returned.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, with a result of an enumerable collection of AI model information.
    /// </returns>
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync(ModelCategory? category)
    {
        var models = new List<AesirModelInfo>();

        try
        {
            var api = serviceProvider.GetKeyedService<OllamaApiClient>(serviceId) ??
                      throw new InvalidOperationException($"Not OllamaApiClient registered for {serviceId}");
            
            // get ollama models loaded
            var ollamaModels = (await api.ListLocalModelsAsync()).ToList();

            // populate embedding models
            if (category is null or ModelCategory.Embedding)
            {
                var allowedModelNames =
                    configuration.GetSection("Configuration:RestrictEmbeddingModelsTo").Get<string[]>() ?? [];

                // restrict the models if the configuration requested it
                var allowedModels = ollamaModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = ollamaModels.Where(m => allowedModelNames.Contains(m.Name)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Name,
                        OwnedBy = "Aesir",
                        CreatedAt = m.ModifiedAt,
                        IsChatModel = false,
                        IsEmbeddingModel = true
                    });
                });
            }

            // populate chat models
            if (category is null or ModelCategory.Chat)
            {
                var allowedModelNames =
                    configuration.GetSection("Configuration:RestrictChatModelsTo").Get<string[]>() ?? [];

                // restrict the models if the configuration requested it
                var allowedModels = ollamaModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = ollamaModels.Where(m => allowedModelNames.Contains(m.Name)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Name,
                        OwnedBy = "Aesir",
                        CreatedAt = m.ModifiedAt,
                        IsChatModel = true,
                        IsEmbeddingModel = false
                    });
                });
            }

            // populate vision models
            if (category is null or ModelCategory.Vision)
            {
                var allowedModelNames =
                    configuration.GetSection("Configuration:RestrictVisionModelsTo").Get<string[]>() ?? [];

                // restrict the models if the configuration requested it
                var allowedModels = ollamaModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = ollamaModels.Where(m => allowedModelNames.Contains(m.Name)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Name,
                        OwnedBy = "Aesir",
                        CreatedAt = m.ModifiedAt,
                        IsChatModel = false,
                        IsEmbeddingModel = false
                    });
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting models from Ollama");

            throw;
        }

        return models;
    }

    /// Asynchronously unloads all specified models from the system using the Ollama API.
    /// Any errors encountered during the unloading process will be logged.
    /// <returns>
    /// A task that represents the asynchronous operation of unloading the selected models.
    /// </returns>
    public async Task UnloadModelsAsync(string[] modelIds)
    {
        await Parallel.ForEachAsync(modelIds, async (modelId, token) =>
        {
            try
            {
                var api = serviceProvider.GetKeyedService<OllamaApiClient>(serviceId) ??
                          throw new InvalidOperationException($"Not OllamaApiClient registered for {serviceId}");
                
                await api.RequestModelUnloadAsync(modelId, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unload of model had error: {Error}", ex);
            }
        });
    }
}
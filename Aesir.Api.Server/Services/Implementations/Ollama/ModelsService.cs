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
    ILogger<ChatService> logger,
    OllamaApiClient api,
    IConfiguration configuration)
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

    /// Asynchronously unloads all configured chat models from the system using the Ollama API.
    /// This method retrieves a list of allowed chat models from the application configuration,
    /// then attempts to unload each model by making asynchronous requests to the underlying API.
    /// Any errors encountered during the unloading process will be logged.
    /// <returns>
    /// A task that represents the asynchronous operation of unloading the chat models.
    /// </returns>
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

    /// Asynchronously unloads the configured embedding model from the system.
    /// If the embedding model is not configured, an InvalidOperationException is thrown.
    /// Logs a warning if an error occurs during the unload process.
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// Asynchronously unloads the configured vision model from the system.
    /// This method retrieves the name of the configured vision model from the application's configuration
    /// and attempts to unload the model using the associated API client. If the configuration setting
    /// for the vision model is unavailable, an InvalidOperationException is thrown. Additionally, the
    /// completion or any exception during the unload process is logged.
    /// <returns>A Task that represents the asynchronous operation.</returns>
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
    
    /// <summary>
    /// Asynchronously unloads all models from the system.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UnloadAllModelsAsync()
    {
        await UnloadChatModelAsync();
        await UnloadEmbeddingModelAsync();
        await UnloadVisionModelAsync();
    }

}
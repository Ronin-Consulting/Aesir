using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Provides functionality to manage and control various AI models using OpenAI's backend.
/// </summary>
/// <param name="logger">Logger instance for recording diagnostic and operational messages related to the service.</param>
/// <param name="client">OpenAIClient instance for handling communication with the OpenAI API.</param>
/// <param name="configuration">Configuration settings for customizing model operations and preferences.</param>
[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ModelsService> logger,
    OpenAIClient client,
    IConfiguration configuration)
    : IModelsService
{
    /// <summary>
    /// An instance of <see cref="ILogger{ModelsService}"/> used for recording logs
    /// and tracking execution flow within the <see cref="ModelsService"/> class.
    /// </summary>
    private readonly ILogger<ModelsService> _logger = logger;

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
            // Get available models from API
            var openAiModels = (await client.GetOpenAIModelClient().GetModelsAsync()).Value;
            
            // populate embedding models
            if (category is null or ModelCategory.Embedding)
            {
                var allowedModelNames =
                    configuration.GetSection("Configuration:RestrictEmbeddingModelsTo").Get<string[]>() ?? [];

                // restrict the models if the configuration requested it
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m => allowedModelNames.Contains(m.Id)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Id,
                        OwnedBy = m.OwnedBy,
                        CreatedAt = m.CreatedAt.DateTime,
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
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m => allowedModelNames.Contains(m.Id)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Id,
                        OwnedBy = m.OwnedBy,
                        CreatedAt = m.CreatedAt.DateTime,
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
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m => allowedModelNames.Contains(m.Id)).ToList();

                allowedModels.ForEach(m =>
                {
                    models.Add(new AesirModelInfo
                    {
                        Id = m.Id,
                        OwnedBy = m.OwnedBy,
                        CreatedAt = m.CreatedAt.DateTime,
                        IsChatModel = false,
                        IsEmbeddingModel = false
                    });
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting models from OpenAI API");

            throw;
        }

        return models;
    }

    /// <summary>
    /// Asynchronously unloads the chat model from the system or clears any associated resources.
    /// This operation is currently a no-op and does not perform any functional task.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous unload operation.
    /// </returns>
    public Task UnloadChatModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously unloads the embedding model from the service.
    /// This method performs no operation and acts as a placeholder for potential future implementation.
    /// </summary>
    /// <returns>
    /// A completed task representing the asynchronous operation.
    /// </returns>
    public Task UnloadEmbeddingModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    /// <summary>
    /// Unloads the vision model asynchronously. This operation is a no-op in its current implementation.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public Task UnloadVisionModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    /// <summary>
    /// Unloads all loaded models, freeing up resources and resetting the state
    /// of available model configurations in the server.
    /// </summary>
    /// <returns>
    /// A completed task representing the operation of unloading all loaded models without
    /// performing any additional actions.
    /// </returns>
    public Task UnloadAllModelsAsync()
    {
        // no op
        return Task.CompletedTask;
    }
}
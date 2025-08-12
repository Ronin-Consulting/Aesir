using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
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
    /// Retrieves a collection of available models from the OpenAI API, filtered to include only
    /// the models explicitly configured for use, such as chat and embedding models.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an enumerable
    /// collection of <see cref="AesirModelInfo"/> objects representing the configured models.
    /// </returns>
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        var result = new List<AesirModelInfo>();

        // Getting embedding model from configuration
        var embeddingModel = configuration.GetValue<string>("Inference:OpenAI:EmbeddingModel") ??
                             "text-embedding-3-small";

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
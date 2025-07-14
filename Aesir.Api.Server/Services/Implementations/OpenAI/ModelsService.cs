using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Handles the implementation of the model management services using OpenAI's backend.
/// </summary>
/// <param name="logger">Logger used for monitoring and logging actions within the service.</param>
/// <param name="client">Instance of OpenAIClient for communicating with the OpenAI API.</param>
/// <param name="configuration">Configuration settings for model behavior and preferences.</param>
[Experimental("SKEXP0070")]
public class ModelsService(
    ILogger<ModelsService> logger,
    OpenAIClient client,
    IConfiguration configuration)
    : IModelsService
{
    /// <summary>
    /// The logger instance used for recording logs and operational details
    /// within the <see cref="ModelsService"/> class.
    /// </summary>
    private readonly ILogger<ModelsService> _logger = logger;

    /// <summary>
    /// Retrieves a collection of available models from the OpenAI API, filtered to include only
    /// those models explicitly configured for use, such as chat and embedding models.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is a collection
    /// of <see cref="AesirModelInfo"/> objects representing the models configured for usage.
    /// </returns>
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

    /// <summary>
    /// Asynchronously unloads the chat model from the system or clears any associated resources.
    /// This operation is currently a no-op and does not perform any functional task.
    /// </summary>
    /// <returns>A task representing the asynchronous unload operation.</returns>
    public Task UnloadChatModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    /// Asynchronously unloads the embedding model from the service.
    /// This method performs no operation and is a placeholder for potential future implementation.
    /// <return>
    /// A completed task representing the asynchronous operation.
    /// </return>
    public Task UnloadEmbeddingModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }

    /// Unloads the vision model asynchronously. This operation is a no-op in its current implementation.
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public Task UnloadVisionModelAsync()
    {
        // no op
        return Task.CompletedTask;
    }
}
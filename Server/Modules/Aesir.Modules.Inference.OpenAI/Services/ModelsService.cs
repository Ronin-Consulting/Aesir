using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Aesir.Modules.Inference.OpenAI.Services;

/// <summary>
/// Provides functionality to manage and control various AI models using OpenAI's backend.
/// </summary>
/// <param name="logger">Logger instance for recording diagnostic and operational messages related to the service.</param>
/// <param name="configuration">Configuration settings for customizing model operations and preferences.</param>
[Experimental("SKEXP0070")]
public class ModelsService(
    string serviceId,
    ILogger<ModelsService> logger,
    IConfiguration configuration,
    IServiceProvider serviceProvider)
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
            var client = serviceProvider.GetKeyedService<OpenAIClient>(serviceId) ??
                         throw new InvalidOperationException($"Not OpenAIClient registered for {serviceId}");

            _logger.LogDebug("[ModelsService] Fetching models for service: {ServiceId}, category: {Category}", serviceId, category);

            // Get available models from API
            var response = await client.GetOpenAIModelClient().GetModelsAsync().ConfigureAwait(false);
            var openAiModels = response.Value;

            _logger.LogDebug("[ModelsService] Received {Count} models from API", openAiModels.Count);
            foreach (var model in openAiModels)
            {
                _logger.LogDebug("[ModelsService] Model ID: {ModelId}, OwnedBy: {OwnedBy}", model.Id, model.OwnedBy);
            }

            // populate embedding models
            if (category is null or ModelCategory.Embedding)
            {
                var allowedModelNames =
                    configuration.GetSection("Configuration:RestrictEmbeddingModelsTo").Get<string[]>() ?? [];

                _logger.LogDebug("[ModelsService] Embedding filter list: [{FilterList}]", string.Join(", ", allowedModelNames));

                // restrict the models if the configuration requested it
                // Use prefix matching to handle quantization suffixes (e.g., model:Q4_0, model:F16)
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m =>
                        allowedModelNames.Any(filter => m.Id.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                _logger.LogDebug("[ModelsService] Embedding models after filter: {Count}", allowedModels.Count);

                allowedModels.ForEach(m =>
                {
                    _logger.LogDebug("[ModelsService] Adding embedding model: {ModelId}", m.Id);
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

                _logger.LogDebug("[ModelsService] Chat filter list: [{FilterList}]", string.Join(", ", allowedModelNames));

                // restrict the models if the configuration requested it
                // Use prefix matching to handle quantization suffixes (e.g., model:Q4_0, model:F16)
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m =>
                        allowedModelNames.Any(filter => m.Id.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                _logger.LogDebug("[ModelsService] Chat models after filter: {Count}", allowedModels.Count);

                allowedModels.ForEach(m =>
                {
                    _logger.LogDebug("[ModelsService] Adding chat model: {ModelId}", m.Id);
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

                _logger.LogDebug("[ModelsService] Vision filter list: [{FilterList}]", string.Join(", ", allowedModelNames));

                // restrict the models if the configuration requested it
                // Use prefix matching to handle quantization suffixes (e.g., model:Q4_0, model:F16)
                var allowedModels = openAiModels.ToList();
                if (allowedModelNames.Length > 0)
                    allowedModels = openAiModels.Where(m =>
                        allowedModelNames.Any(filter => m.Id.StartsWith(filter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                _logger.LogDebug("[ModelsService] Vision models after filter: {Count}", allowedModels.Count);

                allowedModels.ForEach(m =>
                {
                    _logger.LogDebug("[ModelsService] Adding vision model: {ModelId}", m.Id);
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

        _logger.LogDebug("[ModelsService] Returning {Count} total models for category: {Category}", models.Count, category);
        return models;
    }

    /// Asynchronously unloads all specified models from the system using the OpenAI API.
    /// This operation is currently a no-op and does not perform any functional task.
    /// <returns>
    /// A task representing the asynchronous unload operation.
    /// </returns>
    public Task UnloadModelsAsync(string[] modelIds)
    {
        // no op - OpenAI doesn't support model unloading
        return Task.CompletedTask;
    }
}

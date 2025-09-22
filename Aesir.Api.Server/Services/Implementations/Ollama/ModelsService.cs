using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using OllamaSharp;
using OllamaSharp.Models;

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

            var ollamaModels = (await api.ListLocalModelsAsync()).ToList();
            var modelDetails = new Dictionary<string, ShowModelResponse>(StringComparer.Ordinal);
            var throttler = new SemaphoreSlim(Environment.ProcessorCount);
            var tasks = new List<Task>(ollamaModels.Count);
            tasks.AddRange(ollamaModels.Select(model => FetchDetailsAsync(model.Name)));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            async Task FetchDetailsAsync(string name)
            {
                await throttler.WaitAsync().ConfigureAwait(false);
                try
                {
                    var details = await api.ShowModelAsync(name).ConfigureAwait(false);
                    lock (modelDetails)
                    {
                        modelDetails[name] = details;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to get details for model {ModelName}", name);
                }
                finally
                {
                    throttler.Release();
                }
            }
            
            IEnumerable<TModel> Restrict<TModel>(IEnumerable<TModel> source, string configKey, Func<TModel, string> nameSelector)
            {
                var allowed = configuration.GetSection(configKey).Get<string[]>() ?? [];
                return allowed.Length == 0 ? source : source.Where(m => allowed.Contains(nameSelector(m)));
            }

            AesirModelInfo MapToAesirModelInfo(Model m, ShowModelResponse? modelDetail, bool isChat, bool isEmbedding) => new()
            {
                Id = m.Name,
                OwnedBy = "Aesir",
                CreatedAt = m.ModifiedAt,
                IsChatModel = isChat,
                IsEmbeddingModel = isEmbedding,
                Details = new AesirModelDetails
                {
                    ParentModel = m.Details.ParentModel,
                    Format = m.Details.Format,
                    Family = m.Details.Family,
                    Families = m.Details.Families,
                    ParameterSize = m.Details.ParameterSize,
                    QuantizationLevel = m.Details.QuantizationLevel,
                    License = modelDetail?.License,
                    Capabilities = modelDetail?.Capabilities,
                    ExtraInfo = modelDetail?.Info.ExtraInfo
                }
            };

            var categories = new[]
            {
                new { Category = ModelCategory.Embedding, ConfigKey = "Configuration:RestrictEmbeddingModelsTo", IsChat = false, IsEmbedding = true },
                new { Category = ModelCategory.Chat,      ConfigKey = "Configuration:RestrictChatModelsTo",      IsChat = true,  IsEmbedding = false },
                new { Category = ModelCategory.Vision,    ConfigKey = "Configuration:RestrictVisionModelsTo",    IsChat = false, IsEmbedding = false }
            };

            foreach (var c in categories)
            {
                if (category is not null && category != c.Category) continue;
                
                var allowed = Restrict(ollamaModels, c.ConfigKey, m => m.Name);
                models.AddRange(
                    allowed.Select(m => 
                        MapToAesirModelInfo(m, modelDetails.TryGetValue(m.Name, out var details) ? 
                            details : null, c.IsChat, c.IsEmbedding)
                ));
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
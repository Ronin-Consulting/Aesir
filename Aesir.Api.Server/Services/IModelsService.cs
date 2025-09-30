using Aesir.Common.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides model management functionality for AI models.
/// </summary>
public interface IModelsService
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
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync(ModelCategory? category);

    /// <summary>
    /// Unloads the selected model from memory.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnloadModelsAsync(string[] modelIds);
}

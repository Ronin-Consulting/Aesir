using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides model management functionality for AI models.
/// </summary>
public interface IModelsService
{
    /// <summary>
    /// Retrieves information about available AI models.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns a collection of model information.</returns>
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync();
    
    /// <summary>
    /// Unloads the currently loaded chat model from memory.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnloadChatModelAsync();
    
    /// <summary>
    /// Unloads the currently loaded embedding model from memory.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnloadEmbeddingModelAsync();
    
    /// <summary>
    /// Unloads the currently loaded vision model from memory.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnloadVisionModelAsync();
    
    /// <summary>
    /// Asynchronously unloads all models from the system.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UnloadAllModelsAsync();
}

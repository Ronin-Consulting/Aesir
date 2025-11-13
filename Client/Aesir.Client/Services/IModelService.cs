using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;

namespace Aesir.Client.Services;

/// <summary>
/// Defines a service for retrieving model information asynchronously.
/// </summary>
public interface IModelService
{   
    /// <summary>
    /// Asynchronously retrieves a collection of available Aesir models.
    /// This method makes an HTTP request to fetch model information and returns
    /// a collection of AesirModelInfo objects representing the models. If an error
    /// occurs during the request, it logs the exception and rethrows it.
    /// </summary>
    /// <param name="inferenceEngineId">The id of the model to retrieve</param>
    /// <param name="category">
    /// An optional category of models to filter the results. If null, all models are returned.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes,
    /// it contains a collection of AesirModelInfo objects representing the available models.
    /// </returns>
    Task<IEnumerable<AesirModelInfo>> GetModelsAsync(Guid inferenceEngineId, ModelCategory? category);
}
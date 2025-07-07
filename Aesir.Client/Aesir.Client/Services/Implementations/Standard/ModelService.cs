using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Provides functionality for managing and retrieving model information
/// from a remote service.
/// </summary>
/// <remarks>
/// This class implements <see cref="IModelService"/> to interact with external APIs
/// and perform operations related to model retrieval. It uses HTTP requests to
/// fetch data asynchronously and logs potential exceptions.
/// </remarks>
public class ModelService(
    ILogger<ModelService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IModelService
{
    /// <summary>
    /// Represents the Flurl client instance used to make HTTP requests to the
    /// specified inference models API endpoint. This client is cached and configured
    /// with a base URL retrieved from the application's configuration settings.
    /// </summary>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ModelClient",
            configuration.GetValue<string>("Inference:Models"));

    /// Asynchronously retrieves a collection of available Aesir models.
    /// This method makes an HTTP request to fetch model information and returns
    /// a collection of AesirModelInfo objects representing the models. If an
    /// error occurs during the request, it logs the exception and rethrows it.
    /// <returns>
    /// A task representing the asynchronous operation that contains a collection
    /// of AesirModelInfo objects when completed.
    /// </returns>
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        try
        {
            return await _flurlClient.Request().GetJsonAsync<IEnumerable<AesirModelInfo>>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
}
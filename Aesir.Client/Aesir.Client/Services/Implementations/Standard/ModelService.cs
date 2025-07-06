using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ModelService(
    ILogger<ModelService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IModelService
{
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ModelClient",
            configuration.GetValue<string>("Inference:Models"));

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
using System.Collections.Generic;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ModelService : IModelService
{
    private readonly ILogger<ModelService> _logger;
    private readonly IFlurlClient _flurlClient;

    public ModelService(ILogger<ModelService> logger, 
        IConfiguration configuration, IFlurlClientCache flurlClientCache)
    {
        _logger = logger;

        _flurlClient = flurlClientCache
            .GetOrAdd("ModelClient",
                configuration.GetValue<string>("Inference:Models"));
    }
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        try
        {
            return await _flurlClient.Request().GetJsonAsync<IEnumerable<AesirModelInfo>>();
        }
        catch (FlurlHttpException ex)
        {
            await _logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
}
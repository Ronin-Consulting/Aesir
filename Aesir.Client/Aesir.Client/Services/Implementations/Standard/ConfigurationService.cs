using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ConfigurationService(
    ILogger<ConfigurationService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache) : IConfigurationService
{
    /// <summary>
    /// Represents an instance of an <see cref="IFlurlClient"/> used to make HTTP requests.
    /// </summary>
    /// <remarks>
    /// This client is managed via an <see cref="IFlurlClientCache"/>, which ensures efficient
    /// and reusable HTTP client handling. It is configured for use with the Configuration Service
    /// by utilizing a specific endpoint defined in the configuration settings.
    /// </remarks>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ConfigurationClient",
            configuration.GetValue<string>("Inference:Configuration"));
    
    public async Task<IEnumerable<AesirAgentBase>> GetAgentsAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("agents")
                .GetJsonAsync<IEnumerable<AesirAgentBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    public async Task<AesirAgentBase> GetAgentAsync(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"agents/{id}")
                .GetJsonAsync<AesirAgentBase>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }

    public async Task<IEnumerable<AesirToolBase>> GetToolsAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("tools")
                .GetJsonAsync<IEnumerable<AesirToolBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }

    public async Task<AesirToolBase> GetToolAsync(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"tools/{id}")
                .GetJsonAsync<AesirToolBase>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }      
    }

    public async Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid agentId)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"agents/{agentId}/tools")
                .GetJsonAsync<IEnumerable<AesirToolBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }      
    }
}
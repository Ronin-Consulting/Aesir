using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// The ConfigurationService class provides methods to interact with and retrieve
/// configuration-related data from the Aesir services, including agents, tools,
/// and their relationships.
/// </summary>
/// <remarks>
/// This service is implemented using dependency injection and relies on ILogger for logging,
/// IConfiguration for configuration settings, and IFlurlClientCache for managing HTTP requests.
/// The primary purpose is to interact with remote endpoints to fetch configuration data related to
/// agents and tools.
/// </remarks>
/// <example>
/// Methods included in this service enable fetching agents, tools, or specific configurations
/// based on provided identifiers (e.g., agent or tool IDs).
/// </example>
public class ConfigurationService(
    ILogger<ConfigurationService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache) : IConfigurationService
{
    /// <summary>
    /// An instance of <see cref="IFlurlClient"/> used for performing HTTP requests within the Configuration Service.
    /// </summary>
    /// <remarks>
    /// This instance is retrieved and managed through an <see cref="IFlurlClientCache"/> to enable efficient HTTP client
    /// reuse. It is preconfigured with an endpoint obtained from application configuration settings.
    /// </remarks>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ConfigurationClient",
            configuration.GetValue<string>("Inference:Configuration"));

    /// <summary>
    /// Retrieves a collection of Aesir agents asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of <see cref="AesirAgentBase"/> objects.
    /// </returns>
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

    /// <summary>
    /// Retrieves an agent's details using the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the agent details as an instance of <see cref="Aesir.Common.Models.AesirAgentBase"/>.
    /// </returns>
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

    /// <summary>
    /// Asynchronously retrieves a collection of tools available in the system.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirToolBase"/> objects.
    /// </returns>
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

    /// <summary>
    /// Retrieves a tool by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the tool to retrieve.</param>
    /// <returns>An asynchronous task that returns an <see cref="AesirToolBase"/> object representing the requested tool.</returns>
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

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent using the agent's unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent whose tools are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AesirToolBase"/> objects representing the tools linked to the specified agent.</returns>
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
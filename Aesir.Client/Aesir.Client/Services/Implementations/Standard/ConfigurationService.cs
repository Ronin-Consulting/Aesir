using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
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
    /// Creates an agent
    /// </summary>
    /// <param name="agent">The <see cref="AesirAgentBase"/> object to create</param>
    public async Task CreateAgentAsync(AesirAgentBase agent)
    {    
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment("agents")
                .PostJsonAsync(agent);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Updates an agent
    /// </summary>
    /// <param name="agent">The <see cref="AesirAgentBase"/> object to update</param>
    public async Task UpdateAgentAsync(AesirAgentBase agent)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"agents/{agent.Id}")
                .PutJsonAsync(agent);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }


    /// <summary>
    /// Deletes an agent by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to delete.</param>
    public async Task DeleteAgentAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"agents/{id}")
                .DeleteAsync();
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

    /// <summary>
    /// Creates a tool
    /// </summary>
    /// <param name="tool">The <see cref="AesirToolBase"/> object to create</param>
    public async Task CreateToolAsync(AesirToolBase tool)
    {    
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment("tools")
                .PostJsonAsync(tool);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Updates a tool
    /// </summary>
    /// <param name="tool">The <see cref="AesirToolBase"/> object to update</param>
    public async Task UpdateToolAsync(AesirToolBase tool)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"tools/{tool.Id}")
                .PutJsonAsync(tool);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }


    /// <summary>
    /// Deletes a tool by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the tool to delete.</param>
    public async Task DeleteToolAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"tools/{id}")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Asynchronously retrieves a collection of MCP Servers available in the system.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirMcpServerBase"/> objects.
    /// </returns>
    public async Task<IEnumerable<AesirMcpServerBase>> GetMcpServersAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("mcpservers")
                .GetJsonAsync<IEnumerable<AesirMcpServerBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }

    /// <summary>
    /// Retrieves an MCP Server by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP Server to retrieve.</param>
    /// <returns>An asynchronous task that returns an <see cref="AesirMcpServerBase"/> object representing the requested MCP Server.</returns>
    public async Task<AesirMcpServerBase> GetMcpServerAsync(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"mcpservers/{id}")
                .GetJsonAsync<AesirMcpServerBase>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }      
    }

    /// <summary>
    /// Creates an MCP Server
    /// </summary>
    /// <param name="mcpServer">The <see cref="AesirMcpServerBase"/> object to create</param>
    public async Task CreateMcpServerAsync(AesirMcpServerBase mcpServer)
    {    
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment("mcpservers")
                .PostJsonAsync(mcpServer);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Updates an MCP Server
    /// </summary>
    /// <param name="mcpServer">The <see cref="AesirMcpServerBase"/> object to update</param>
    public async Task UpdateMcpServerAsync(AesirMcpServerBase mcpServer)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"mcpservers/{mcpServer.Id}")
                .PutJsonAsync(mcpServer);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }


    /// <summary>
    /// Deletes an MCP Server by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP Server to delete.</param>
    public async Task DeleteMcpServerAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"mcpservers/{id}")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the default prompt persona asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a <see cref="PromptPersona"/> value representing the default prompt persona.
    /// </returns>
    public async Task<PromptPersona> GetDefaultPersonaAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"personas")
                .AppendPathSegment("default")
                .GetJsonAsync<PromptPersona>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
}
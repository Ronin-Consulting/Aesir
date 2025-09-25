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
    /// Retrieves an indicator if the system is fully ready for use or needs further configuration
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains true if the system is ready; false otherwise.
    /// </returns>
    public async Task<AesirConfigurationReadinessBase> GetIsSystemConfigurationReadyAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"systemready")
                .GetJsonAsync<AesirConfigurationReadinessBase>());

        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
    
    /// <summary>
    /// Retrieves an indicator if the system is being run in database configuration mode.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains true if it's in database configuration mode; false otherwise.
    /// </returns>
    public async Task<bool> GetIsInDatabaseModeAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"databaseconfigurationmode")
                .GetJsonAsync<bool>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }

    /// <summary>
    /// Retrieves the general settings
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the general settings as an instance of <see cref="Aesir.Common.Models.AesirGeneralSettingsBase"/>.
    /// </returns>
    public async Task<AesirGeneralSettingsBase> GetGeneralSettingsAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"generalsettings")
                .GetJsonAsync<AesirGeneralSettingsBase>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
    
    /// <summary>
    /// Updates the general settings
    /// </summary>
    /// <param name="generalSettings">The <see cref="AesirGeneralSettingsBase"/> object to update</param>
    public async Task UpdateGeneralSettingsAsync(AesirGeneralSettingsBase generalSettings)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"generalsettings")
                .PutJsonAsync(generalSettings);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Retrieves a collection of Aesir inference engines asynchronously.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a collection of <see cref="AesirInferenceEngineBase"/> objects.
    /// </returns>
    public async Task<IEnumerable<AesirInferenceEngineBase>> GetInferenceEnginesAsync()
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment("inferenceengines")
                .GetJsonAsync<IEnumerable<AesirInferenceEngineBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Retrieves an inference engine's details using the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the inference engine to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the agent details as an instance of <see cref="Aesir.Common.Models.AesirInferenceEngineBase"/>.
    /// </returns>
    public async Task<AesirInferenceEngineBase> GetInferenceEngineAsync(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"inferenceengines/{id}")
                .GetJsonAsync<AesirInferenceEngineBase>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }

    /// <summary>
    /// Creates an inference engine
    /// </summary>
    /// <param name="inferenceEngine">The <see cref="AesirInferenceEngineBase"/> object to create</param>
    public async Task CreateInferenceEngineAsync(AesirInferenceEngineBase inferenceEngine)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment("inferenceengines")
                .PostJsonAsync(inferenceEngine);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Updates an inference engine
    /// </summary>
    /// <param name="inferenceEngine">The <see cref="AesirInferenceEngineBase"/> object to update</param>
    public async Task UpdateInferenceEngineAsync(AesirInferenceEngineBase inferenceEngine)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"inferenceengines/{inferenceEngine.Id}")
                .PutJsonAsync(inferenceEngine);
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Deletes an inference engine by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the inference engine to delete.</param>
    public async Task DeleteInferenceEngineAsync(Guid id)
    {
        try
        {
            await _flurlClient.Request()
                .AppendPathSegment($"inferenceengines/{id}")
                .DeleteAsync();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

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
    /// <param name="id">The unique identifier of the agent whose tools are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AesirToolBase"/> objects representing the tools linked to the specified agent.</returns>
    public async Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"agents/{id}/tools")
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
    /// Creates an unsaved instance of an MCP server based on the provided JSON configuration.
    /// </summary>
    /// <param name="clientConfigurationJson">The JSON string containing the client configuration to create the MCP server.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The result of the task
    /// is an instance of <see cref="AesirMcpServerBase"/> representing the created MCP server.
    /// </returns>
    public async Task<AesirMcpServerBase> CreateMcpServerFromConfigAsync(string clientConfigurationJson)
    {
        try
        {
            var result = await _flurlClient.Request()
                .AppendPathSegment("mcpservers/from-config")
                .PostJsonAsync(clientConfigurationJson);
            
            return await result.GetJsonAsync<AesirMcpServerBase>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }
    
    /// <summary>
    /// Retrieves a collection of tools associated with a specific MCP Server using the server's unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP Server whose tools are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AesirMcpServerToolBase"/> objects representing the tools linked to the specified MCP server.</returns>
    public async Task<IEnumerable<AesirMcpServerToolBase>> GetMcpServerTools(Guid id)
    {
        try
        {
            return (await _flurlClient.Request()
                .AppendPathSegment($"mcpservers/{id}/tools")
                .GetJsonAsync<IEnumerable<AesirMcpServerToolBase>>());
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }   
    }
}
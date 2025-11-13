using Aesir.Infrastructure.Models;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Represents a service responsible for managing configuration data related to agents and tools.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Indicates if the service is in database mode or file mode
    /// </summary>
    public bool DatabaseMode { get; }
    
    /// <summary>
    /// Asynchronously retrieves the Aesir general settings.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The task result contains the <see cref="AesirGeneralSettings"/>.
    /// </returns>
    Task<AesirGeneralSettings> GetGeneralSettingsAsync();

    /// <summary>
    /// Updates the AesirGeneralSettings in the database.
    /// </summary>
    /// <param name="generalSettings">The general setting with updated values.</param>
    Task UpdateGeneralSettingsAsync(AesirGeneralSettings generalSettings);
    
    /// <summary>
    /// Asynchronously retrieves a collection of Aesir inference engines.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The result contains an enumerable collection of <c>AesirInferenceEngine</c> objects.
    /// </returns>
    Task<IEnumerable<AesirInferenceEngine>> GetInferenceEnginesAsync();

    /// <summary>
    /// Retrieves an AesirInferenceEngine by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirInferenceEngine to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirInferenceEngine"/> corresponding to the given identifier.
    /// If no inference engine is found, returns null.
    /// </returns>
    Task<AesirInferenceEngine> GetInferenceEngineAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirInferenceEngine into the database.
    /// </summary>
    /// <param name="InferenceEngine">The inference engine to insert.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains unique identifier of the created inference engine.
    /// </returns>
    Task<Guid> CreateInferenceEngineAsync(AesirInferenceEngine inferenceEngine);

    /// <summary>
    /// Updates an existing AesirInferenceEngine in the database.
    /// </summary>
    /// <param name="inferenceEngine">The inference engine with updated values.</param>
    Task UpdateInferenceEngineAsync(AesirInferenceEngine inferenceEngine);

    /// <summary>
    /// Delete an existing AesirInferenceEngine from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirInferenceEngine to delete.</param>
    Task DeleteInferenceEngineAsync(Guid id);
    
    /// <summary>
    /// Asynchronously retrieves a collection of Aesir agents.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The result contains an enumerable collection of <c>AesirAgent</c> objects.
    /// </returns>
    Task<IEnumerable<AesirAgent>> GetAgentsAsync();

    /// <summary>
    /// Retrieves an AesirAgent by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirAgent to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirAgent"/> corresponding to the given identifier.
    /// If no agent is found, returns null.
    /// </returns>
    Task<AesirAgent> GetAgentAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirAgent into the database.
    /// </summary>
    /// <param name="agent">The agent to insert.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains unique identifier of the created agent.
    /// </returns>
    Task<Guid> CreateAgentAsync(AesirAgent agent);

    /// <summary>
    /// Updates an existing AesirAgentServer in the database.
    /// </summary>
    /// <param name="agent">The agent with updated values.</param>
    Task UpdateAgentAsync(AesirAgent agent);

    /// <summary>
    /// Delete an existing AesirAgent from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirAgent to delete.</param>
    Task DeleteAgentAsync(Guid id);

    /// <summary>
    /// Asynchronously retrieves a collection of tools from the configuration database.
    /// This method queries and returns a list of all tools available within the database by fetching relevant records.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an enumerable collection of `AesirTool` objects.
    /// </returns>
    Task<IEnumerable<AesirTool>> GetToolsAsync();

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent whose tools are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirTool"/> objects used by the specified agent.</returns>
    Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid id);

    /// <summary>
    /// Updates the tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="toolIds">An array of unique identifiers for the tools to associate with the agent. If null, the tools will be cleared.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateToolsForAgentAsync(Guid id, Guid[]? toolIds);

    /// <summary>
    /// Asynchronously retrieves an Aesir tool by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the Aesir tool to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="AesirTool"/> object if it exists, or null if not found.</returns>
    Task<AesirTool> GetToolAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirTool into the database.
    /// </summary>
    /// <param name="tool">The tool to insert.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains unique identifier of the created tool.
    /// </returns>
    Task<Guid> CreateToolAsync(AesirTool tool);

    /// <summary>
    /// Updates an existing AesirTool in the database.
    /// </summary>
    /// <param name="tool">The tool with updated values.</param>
    Task UpdateToolAsync(AesirTool agent);

    /// <summary>
    /// Delete an existing AesirTool from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirTool to delete.</param>
    Task DeleteToolAsync(Guid id);
    
    /// <summary>
    /// Retrieves a list of Aesir MCP Servers stored in the database asynchronously.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <c>AesirMcpServer</c> representing the MCP Servers retrieved from the database.
    /// </returns>
    Task<IEnumerable<AesirMcpServer>> GetMcpServersAsync();

    /// <summary>
    /// Retrieves an AesirMcpServer by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirMcpServer to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirMcpServer"/> object corresponding to the given identifier.
    /// If no MCP Server is found, returns null.
    /// </returns>
    Task<AesirMcpServer> GetMcpServerAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirMcpServer into the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server to insert.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains unique identifier of the created MCP Server.
    /// </returns>
    Task<Guid> CreateMcpServerAsync(AesirMcpServer mcpServer);

    /// <summary>
    /// Updates an existing AesirMcpServer in the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server with updated values.</param>
    Task UpdateMcpServerAsync(AesirMcpServer mcpServer);

    /// <summary>
    /// Delete an existing AesirMcpServer from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirMcpServer to delete.</param>
    Task DeleteMcpServerAsync(Guid id);

    /// <summary>
    /// Prepares and validates database configuration during system boot.
    /// Checks for required configuration and reports any missing items.
    /// </summary>
    /// <param name="configurationReadinessService">Service to track configuration readiness issues.</param>
    Task PrepareDatabaseConfigurationAsync(IConfigurationReadinessService configurationReadinessService);

    /// <summary>
    /// Prepares and validates file-based configuration during system boot.
    /// Ensures all configuration items have valid IDs and are properly structured.
    /// </summary>
    void PrepareFileConfigurationAsync();
}
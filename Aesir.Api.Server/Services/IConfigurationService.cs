using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Represents a service responsible for managing configuration data related to agents and tools.
/// </summary>
public interface IConfigurationService
{
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
    /// <param name="agentId">The unique identifier of the agent whose tools are to be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirTool"/> objects used by the specified agent.</returns>
    Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid agentId);

    /// <summary>
    /// Asynchronously retrieves an Aesir tool by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the Aesir tool to retrieve.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the <see cref="AesirTool"/> object if it exists, or null if not found.</returns>
    Task<AesirTool> GetToolAsync(Guid id);
}
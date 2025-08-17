using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a service for managing and retrieving configuration data
/// related to agents and tools within the system.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Asynchronously retrieves a collection of Aesir agents.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The result of the task
    /// is an enumerable collection of <see cref="AesirAgentBase"/> objects, which represent the agents.
    /// </returns>
    Task<IEnumerable<AesirAgentBase>> GetAgentsAsync();

    /// <summary>
    /// Retrieves an agent's details using the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent to be retrieved.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the agent details as an instance of <see cref="Aesir.Common.Models.AesirAgentBase"/>.
    /// </returns>
    Task<AesirAgentBase> GetAgentAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirAgentBase into the database.
    /// </summary>
    /// <param name="agent">The agent to insert.</param>
    Task CreateAgentAsync(AesirAgentBase agent);

    /// <summary>
    /// Updates an existing AesirAgentBase in the database.
    /// </summary>
    /// <param name="agent">The agent with updated values.</param>
    Task UpdateAgentAsync(AesirAgentBase agent);

    /// <summary>
    /// Delete an existing AesirAgentBase from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirAgentBase to delete.</param>
    Task DeleteAgentAsync(Guid id);

    /// <summary>
    /// Asynchronously retrieves a collection of tools available in the system.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of <see cref="AesirToolBase"/> objects.
    /// </returns>
    Task<IEnumerable<AesirToolBase>> GetToolsAsync();

    /// <summary>
    /// Retrieves a tool by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the tool to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AesirToolBase"/> object representing the requested tool.</returns>
    Task<AesirToolBase> GetToolAsync(Guid id);

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent based on the agent's unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the agent for which tools should be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AesirToolBase"/> instances representing the tools associated with the specified agent.</returns>
    Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirToolBase into the database.
    /// </summary>
    /// <param name="tool">The tool to insert.</param>
    Task CreateToolAsync(AesirToolBase tool);

    /// <summary>
    /// Updates an existing AesirToolBase in the database.
    /// </summary>
    /// <param name="tool">The tool with updated values.</param>
    Task UpdateToolAsync(AesirToolBase tool);

    /// <summary>
    /// Delete an existing AesirToolBase from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirToolBase to delete.</param>
    Task DeleteToolAsync(Guid id);
    
    /// <summary>
    /// Retrieves a list of Aesir MCP Servers stored in the database asynchronously.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <c>AesirMcpServerBase</c> representing the MCP Servers retrieved from the database.
    /// </returns>
    Task<IEnumerable<AesirMcpServerBase>> GetMcpServersAsync();

    /// <summary>
    /// Retrieves an AesirMcpServer by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the MCP Server for which tools should be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AesirMcpServerBase"/> object representing the requested tool.</returns>
    Task<AesirMcpServerBase> GetMcpServerAsync(Guid id);

    /// <summary>
    /// Inserts a new AesirMcpServerBase into the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server to insert.</param>
    Task CreateMcpServerAsync(AesirMcpServerBase mcpServer);

    /// <summary>
    /// Updates an existing AesirMcpServerBase in the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server with updated values.</param>
    Task UpdateMcpServerAsync(AesirMcpServerBase mcpServer);

    /// <summary>
    /// Delete an existing AesirMcpServer from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirMcpServer to delete.</param>
    Task DeleteMcpServerAsync(Guid id);

    /// <summary>
    /// Asynchronously retrieves the default persona for generating prompts within the system.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="PromptPersona"/> value
    /// indicating the default persona.
    /// </returns>
    Task<PromptPersona> GetDefaultPersonaAsync();
}
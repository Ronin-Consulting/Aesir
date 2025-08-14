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
    /// <param name="agentId">The unique identifier of the agent for which tools should be retrieved.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AesirToolBase"/> instances representing the tools associated with the specified agent.</returns>
    Task<IEnumerable<AesirToolBase>> GetToolsForAgentAsync(Guid agentId);

    /// <summary>
    /// Asynchronously retrieves the default persona for generating prompts within the system.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a <see cref="PromptPersona"/> value
    /// indicating the default persona.
    /// </returns>
    Task<PromptPersona> GetDefaultPersonaAsync();
}
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing agent tools, including RAG tool detection.
/// Caches tools per agent to avoid repeated API calls.
/// </summary>
public interface IAgentToolsService
{
    /// <summary>
    /// Gets the tools assigned to a specific agent.
    /// Results are cached per agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of tools assigned to the agent.</returns>
    Task<IReadOnlyList<AesirToolBase>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Checks if the given tools collection contains the RAG tool.
    /// </summary>
    /// <param name="tools">Collection of tools to check.</param>
    /// <returns>True if the RAG tool is present.</returns>
    bool HasRagTool(IEnumerable<AesirToolBase> tools);

    /// <summary>
    /// Checks if a specific agent has the RAG tool assigned.
    /// This is a convenience method that combines GetAgentToolsAsync and HasRagTool.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the agent has the RAG tool assigned.</returns>
    Task<bool> AgentHasRagToolAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Clears the tools cache for a specific agent.
    /// Call this when agent tool assignments change.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    void InvalidateCache(Guid agentId);

    /// <summary>
    /// Clears the entire tools cache.
    /// </summary>
    void ClearCache();
}

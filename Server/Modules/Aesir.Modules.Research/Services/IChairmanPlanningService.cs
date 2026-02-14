using Aesir.Common.Models;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for Chairman to create a single unified research plan for all agents.
/// </summary>
public interface IChairmanPlanningService
{
    /// <summary>
    /// Creates a unified research plan that assigns sub-tasks to each agent.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="chairman">The Chairman agent.</param>
    /// <param name="teamAgents">The research team agents (excluding Chairman).</param>
    /// <param name="refinedQuery">The refined research query.</param>
    /// <param name="priorHistory">Optional prior conversation history to provide context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping each agent's team member ID to their assigned sub-plan.</returns>
    Task<Dictionary<Guid, string>> CreateUnifiedPlanAsync(
        ResearchSession session,
        ResearchAgent chairman,
        IReadOnlyList<ResearchAgent> teamAgents,
        string refinedQuery,
        IReadOnlyList<AesirChatMessage>? priorHistory = null,
        CancellationToken cancellationToken = default);
}
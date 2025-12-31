using Aesir.Tools.LegalValidator.Models;

namespace Aesir.Tools.LegalValidator.Services;

/// <summary>
/// Client for interacting with the AESIR API.
/// </summary>
public interface IAesirApiClient
{
    /// <summary>
    /// Gets all available agents from AESIR.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of agent information.</returns>
    Task<IReadOnlyList<AgentInfo>> GetAgentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Agent information or null if not found.</returns>
    Task<AgentInfo?> GetAgentAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Sends a question to an agent and gets the response.
    /// </summary>
    /// <param name="agentId">The agent to query.</param>
    /// <param name="question">The legal question to ask.</param>
    /// <param name="customSystemPrompt">Optional custom system prompt to override the agent's prompt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    Task<AgentResponse> SendQuestionAsync(
        Guid agentId,
        LegalQuestion question,
        string? customSystemPrompt = null,
        CancellationToken ct = default);
}

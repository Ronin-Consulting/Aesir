using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Settings.Services;

/// <summary>
/// Service interface for managing agents.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Gets all agents.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirAgentBase>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    Task<ApiResult<AesirAgentBase>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new agent.
    /// </summary>
    Task<ApiResult<Guid>> CreateAsync(AesirAgentBase agent, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing agent.
    /// </summary>
    Task<ApiResult> UpdateAsync(AesirAgentBase agent, CancellationToken ct = default);

    /// <summary>
    /// Deletes an agent.
    /// </summary>
    Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets tools assigned to an agent.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirToolBase>>> GetAgentToolsAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Updates tools assigned to an agent.
    /// </summary>
    Task<ApiResult> UpdateAgentToolsAsync(Guid agentId, IEnumerable<Guid> toolIds, CancellationToken ct = default);
}

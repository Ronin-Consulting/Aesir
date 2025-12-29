using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for Research Team API operations.
/// Provides typed methods for managing research team configurations.
/// </summary>
public interface IResearchTeamApiService
{
    /// <summary>
    /// Gets all research teams for a user.
    /// </summary>
    /// <param name="userId">The user ID (defaults to hardcoded user).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<IReadOnlyList<ResearchTeamBase>>> GetTeamsAsync(string userId = "blangford@gmail.com", CancellationToken ct = default);

    /// <summary>
    /// Gets all active research teams for a user.
    /// </summary>
    /// <param name="userId">The user ID (defaults to hardcoded user).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<IReadOnlyList<ResearchTeamBase>>> GetActiveTeamsAsync(string userId = "blangford@gmail.com", CancellationToken ct = default);

    /// <summary>
    /// Gets a research team by ID.
    /// </summary>
    /// <param name="id">The team ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchTeamBase>> GetTeamAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new research team.
    /// </summary>
    /// <param name="team">The team to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchTeamBase>> CreateTeamAsync(ResearchTeamBase team, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing research team.
    /// </summary>
    /// <param name="team">The team to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchTeamBase>> UpdateTeamAsync(ResearchTeamBase team, CancellationToken ct = default);

    /// <summary>
    /// Deletes a research team.
    /// </summary>
    /// <param name="id">The team ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult> DeleteTeamAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Duplicates a research team with a new name.
    /// </summary>
    /// <param name="id">The team ID to duplicate.</param>
    /// <param name="newName">The name for the duplicate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchTeamBase>> DuplicateTeamAsync(Guid id, string newName, CancellationToken ct = default);

    /// <summary>
    /// Validates that agent IDs are valid.
    /// </summary>
    /// <param name="agentIds">The agent IDs to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<bool>> ValidateAgentsAsync(IEnumerable<Guid> agentIds, CancellationToken ct = default);
}

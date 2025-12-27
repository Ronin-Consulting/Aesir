using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Repository interface for managing research team configurations.
/// </summary>
public interface IResearchTeamRepository
{
    /// <summary>
    /// Gets a research team by its unique identifier, including its members.
    /// </summary>
    /// <param name="id">The unique identifier of the research team.</param>
    /// <returns>The research team with members if found, null otherwise.</returns>
    Task<ResearchTeam?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all research teams for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of research teams for the user.</returns>
    Task<IEnumerable<ResearchTeam>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets all active research teams for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of active research teams for the user.</returns>
    Task<IEnumerable<ResearchTeam>> GetActiveByUserIdAsync(string userId);

    /// <summary>
    /// Creates a new research team with its members.
    /// </summary>
    /// <param name="team">The research team to create.</param>
    /// <returns>The created research team with its generated ID.</returns>
    Task<ResearchTeam> AddAsync(ResearchTeam team);

    /// <summary>
    /// Updates an existing research team and its members.
    /// </summary>
    /// <param name="team">The research team to update.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateAsync(ResearchTeam team);

    /// <summary>
    /// Deletes a research team and all its members (cascaded).
    /// </summary>
    /// <param name="id">The unique identifier of the research team to delete.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    Task<bool> RemoveAsync(Guid id);
}

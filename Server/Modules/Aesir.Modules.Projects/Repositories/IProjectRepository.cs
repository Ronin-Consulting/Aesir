using Aesir.Infrastructure.Models;

namespace Aesir.Modules.Projects.Repositories;

/// <summary>
/// Repository interface for managing projects.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Gets a project by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the project.</param>
    /// <returns>The project if found and not deleted, null otherwise.</returns>
    Task<AesirProject?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a project by its name.
    /// </summary>
    /// <param name="name">The name of the project.</param>
    /// <returns>The project if found and not deleted, null otherwise.</returns>
    Task<AesirProject?> GetByNameAsync(string name);

    /// <summary>
    /// Gets all projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of non-deleted projects for the user.</returns>
    Task<IEnumerable<AesirProject>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets all active projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of active, non-deleted projects for the user.</returns>
    Task<IEnumerable<AesirProject>> GetActiveByUserIdAsync(string userId);

    /// <summary>
    /// Checks if a project name already exists.
    /// </summary>
    /// <param name="name">The project name to check.</param>
    /// <param name="excludeId">Optional project ID to exclude from the check (for updates).</param>
    /// <returns>True if the name exists, false otherwise.</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The created project with its generated ID.</returns>
    Task<AesirProject> AddAsync(AesirProject project);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="project">The project to update.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateAsync(AesirProject project);

    /// <summary>
    /// Soft deletes a project.
    /// </summary>
    /// <param name="id">The unique identifier of the project to delete.</param>
    /// <param name="deletedBy">The identifier of the user performing the deletion.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    Task<bool> SoftDeleteAsync(Guid id, string deletedBy);

    /// <summary>
    /// Gets the count of conversations associated with a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <returns>The number of conversations in the project.</returns>
    Task<int> GetConversationCountAsync(Guid projectId);

    /// <summary>
    /// Associates a conversation with a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> AssociateConversationAsync(Guid projectId, Guid conversationId);

    /// <summary>
    /// Removes a conversation's association with a project.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> DisassociateConversationAsync(Guid conversationId);

    /// <summary>
    /// Removes all conversation associations for a project (sets project_id to null).
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <returns>The number of conversations that were disassociated.</returns>
    Task<int> DisassociateAllConversationsAsync(Guid projectId);
}

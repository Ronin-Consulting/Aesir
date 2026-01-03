using Aesir.Infrastructure.Models;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Service interface for managing projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Gets a project by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the project.</param>
    /// <returns>The project if found, null otherwise.</returns>
    Task<AesirProject?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a project by its name.
    /// </summary>
    /// <param name="name">The name of the project.</param>
    /// <returns>The project if found, null otherwise.</returns>
    Task<AesirProject?> GetByNameAsync(string name);

    /// <summary>
    /// Gets all projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of projects for the user.</returns>
    Task<IEnumerable<AesirProject>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets all active projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of active projects for the user.</returns>
    Task<IEnumerable<AesirProject>> GetActiveByUserIdAsync(string userId);

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="request">The project creation request.</param>
    /// <param name="userId">The identifier of the user creating the project.</param>
    /// <returns>The created project.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a project with the same name already exists.</exception>
    Task<AesirProject> CreateAsync(CreateProjectRequest request, string userId);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="request">The project update request.</param>
    /// <param name="userId">The identifier of the user performing the update.</param>
    /// <returns>The updated project.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the project is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a project with the new name already exists.</exception>
    Task<AesirProject> UpdateAsync(Guid id, UpdateProjectRequest request, string userId);

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="userId">The identifier of the user performing the deletion.</param>
    /// <param name="deleteConversations">If true, deletes associated conversations. If false, orphans them.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    Task<bool> DeleteAsync(Guid id, string userId, bool deleteConversations = false);

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
}

using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Projects.Services;

/// <summary>
/// Service interface for project API operations.
/// </summary>
public interface IProjectApiService
{
    /// <summary>
    /// Gets all projects for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of projects.</returns>
    Task<ApiResult<IReadOnlyList<AesirProjectBase>>> GetProjectsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active projects for the specified user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of active projects.</returns>
    Task<ApiResult<IReadOnlyList<AesirProjectBase>>> GetActiveProjectsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific project by ID.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The project if found.</returns>
    Task<ApiResult<AesirProjectBase>> GetProjectAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The project creation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created project.</returns>
    Task<ApiResult<AesirProjectBase>> CreateProjectAsync(string userId, CreateProjectRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The project update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated project.</returns>
    Task<ApiResult<AesirProjectBase>> UpdateProjectAsync(Guid id, string userId, UpdateProjectRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="deleteConversations">Whether to delete associated conversations.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted successfully.</returns>
    Task<ApiResult> DeleteProjectAsync(Guid id, string userId, bool deleteConversations = false, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of conversations in a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The conversation count.</returns>
    Task<ApiResult<int>> GetConversationCountAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Associates a conversation with a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<ApiResult> AssociateConversationAsync(Guid projectId, Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Removes a conversation's association with a project.
    /// </summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful.</returns>
    Task<ApiResult> DisassociateConversationAsync(Guid projectId, Guid conversationId, CancellationToken ct = default);
}

/// <summary>
/// Request model for creating a new project.
/// </summary>
public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
}

/// <summary>
/// Request model for updating an existing project.
/// </summary>
public class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public bool IsActive { get; set; } = true;
}

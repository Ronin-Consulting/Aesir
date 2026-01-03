using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Projects.Controllers;

/// <summary>
/// API controller for managing projects.
/// Projects allow users to create self-contained workspaces with their own
/// chat histories and knowledge bases.
/// </summary>
[ApiController]
[Route("projects")]
[Produces("application/json")]
public class ProjectsController(
    ILogger<ProjectsController> logger,
    IProjectService projectService)
    : ControllerBase
{
    /// <summary>
    /// Gets all projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of projects for the user.</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<AesirProject>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AesirProject>> GetProjectsAsync([FromRoute] string userId)
    {
        var projects = await projectService.GetByUserIdAsync(userId);
        logger.LogDebug("Found {Count} projects for user {UserId}", projects.Count(), userId);
        return projects;
    }

    /// <summary>
    /// Gets all active projects for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of active projects for the user.</returns>
    [HttpGet("user/{userId}/active")]
    [ProducesResponseType(typeof(IEnumerable<AesirProject>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AesirProject>> GetActiveProjectsAsync([FromRoute] string userId)
    {
        var projects = await projectService.GetActiveByUserIdAsync(userId);
        logger.LogDebug("Found {Count} active projects for user {UserId}", projects.Count(), userId);
        return projects;
    }

    /// <summary>
    /// Gets a project by its unique identifier.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <returns>The project if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AesirProject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectAsync([FromRoute] Guid id)
    {
        var project = await projectService.GetByIdAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="request">The project creation request.</param>
    /// <returns>The created project.</returns>
    [HttpPost("user/{userId}")]
    [ProducesResponseType(typeof(AesirProject), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProjectAsync(
        [FromRoute] string userId,
        [FromBody] CreateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Project name is required.");
        }

        try
        {
            var project = await projectService.CreateAsync(request, userId);
            logger.LogInformation("Created project {ProjectId} for user {UserId}", project.Id, userId);
            return CreatedAtAction(nameof(GetProjectAsync), new { id = project.Id }, project);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to create project: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="userId">The user identifier performing the update.</param>
    /// <param name="request">The project update request.</param>
    /// <returns>The updated project.</returns>
    [HttpPut("{id:guid}/user/{userId}")]
    [ProducesResponseType(typeof(AesirProject), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProjectAsync(
        [FromRoute] Guid id,
        [FromRoute] string userId,
        [FromBody] UpdateProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Project name is required.");
        }

        try
        {
            var project = await projectService.UpdateAsync(id, request, userId);
            logger.LogInformation("Updated project {ProjectId} by user {UserId}", id, userId);
            return Ok(project);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Project not found: {ProjectId}", id);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Failed to update project: {Message}", ex.Message);
            return Conflict(ex.Message);
        }
    }

    /// <summary>
    /// Deletes a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="userId">The user identifier performing the deletion.</param>
    /// <param name="request">Optional request specifying whether to delete conversations.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/user/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProjectAsync(
        [FromRoute] Guid id,
        [FromRoute] string userId,
        [FromBody] DeleteProjectRequest? request = null)
    {
        var deleted = await projectService.DeleteAsync(id, userId, request?.DeleteConversations ?? false);

        if (!deleted)
        {
            return NotFound();
        }

        logger.LogInformation("Deleted project {ProjectId} by user {UserId}", id, userId);
        return NoContent();
    }

    /// <summary>
    /// Gets the count of conversations in a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <returns>The conversation count.</returns>
    [HttpGet("{id:guid}/conversations/count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationCountAsync([FromRoute] Guid id)
    {
        var project = await projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        var count = await projectService.GetConversationCountAsync(id);
        return Ok(count);
    }

    /// <summary>
    /// Associates a conversation with a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/conversations/{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssociateConversationAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid conversationId)
    {
        try
        {
            await projectService.AssociateConversationAsync(id, conversationId);
            logger.LogInformation("Associated conversation {ConversationId} with project {ProjectId}",
                conversationId, id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Project not found: {ProjectId}", id);
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Removes a conversation's association with a project.
    /// </summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}/conversations/{conversationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisassociateConversationAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid conversationId)
    {
        await projectService.DisassociateConversationAsync(conversationId);
        logger.LogInformation("Disassociated conversation {ConversationId} from project {ProjectId}",
            conversationId, id);
        return NoContent();
    }
}

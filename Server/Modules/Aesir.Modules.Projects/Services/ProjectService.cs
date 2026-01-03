using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Projects.Repositories;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Projects.Services;

/// <summary>
/// Service implementation for managing projects.
/// </summary>
public class ProjectService(
    ILogger<ProjectService> logger,
    IProjectRepository projectRepository) : IProjectService
{
    /// <inheritdoc />
    public async Task<AesirProject?> GetByIdAsync(Guid id)
    {
        return await projectRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<AesirProject?> GetByNameAsync(string name)
    {
        return await projectRepository.GetByNameAsync(name);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirProject>> GetByUserIdAsync(string userId)
    {
        return await projectRepository.GetByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirProject>> GetActiveByUserIdAsync(string userId)
    {
        return await projectRepository.GetActiveByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<AesirProject> CreateAsync(CreateProjectRequest request, string userId)
    {
        // Validate name uniqueness
        if (await projectRepository.NameExistsAsync(request.Name))
        {
            logger.LogWarning("Project creation failed: name '{ProjectName}' already exists", request.Name);
            throw new InvalidOperationException($"A project with the name '{request.Name}' already exists.");
        }

        var project = new AesirProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            IsActive = true,
            IsDeleted = false,
            CreatedBy = userId
        };

        var createdProject = await projectRepository.AddAsync(project);

        logger.LogInformation("Created project '{ProjectName}' with ID {ProjectId} for user {UserId}",
            project.Name, project.Id, userId);

        return createdProject;
    }

    /// <inheritdoc />
    public async Task<AesirProject> UpdateAsync(Guid id, UpdateProjectRequest request, string userId)
    {
        var existingProject = await projectRepository.GetByIdAsync(id);
        if (existingProject == null)
        {
            logger.LogWarning("Project update failed: project {ProjectId} not found", id);
            throw new KeyNotFoundException($"Project with ID '{id}' not found.");
        }

        // Validate name uniqueness if name is being changed
        if (!string.Equals(existingProject.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (await projectRepository.NameExistsAsync(request.Name, id))
            {
                logger.LogWarning("Project update failed: name '{ProjectName}' already exists", request.Name);
                throw new InvalidOperationException($"A project with the name '{request.Name}' already exists.");
            }
        }

        existingProject.Name = request.Name;
        existingProject.Description = request.Description;
        existingProject.Instructions = request.Instructions;
        existingProject.IsActive = request.IsActive;
        existingProject.UpdatedBy = userId;

        await projectRepository.UpdateAsync(existingProject);

        logger.LogInformation("Updated project {ProjectId} by user {UserId}", id, userId);

        return existingProject;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, string userId, bool deleteConversations = false)
    {
        var existingProject = await projectRepository.GetByIdAsync(id);
        if (existingProject == null)
        {
            logger.LogWarning("Project deletion failed: project {ProjectId} not found", id);
            return false;
        }

        if (deleteConversations)
        {
            // TODO: Implement conversation deletion when chat service integration is added
            // For now, just disassociate all conversations
            logger.LogWarning("Conversation deletion requested but not yet implemented. Disassociating instead.");
        }

        // Disassociate all conversations from this project
        var disassociatedCount = await projectRepository.DisassociateAllConversationsAsync(id);
        logger.LogInformation("Disassociated {Count} conversations from project {ProjectId}",
            disassociatedCount, id);

        // Soft delete the project
        var result = await projectRepository.SoftDeleteAsync(id, userId);

        if (result)
        {
            logger.LogInformation("Deleted project {ProjectId} by user {UserId}", id, userId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<int> GetConversationCountAsync(Guid projectId)
    {
        return await projectRepository.GetConversationCountAsync(projectId);
    }

    /// <inheritdoc />
    public async Task<bool> AssociateConversationAsync(Guid projectId, Guid conversationId)
    {
        // Verify project exists
        var project = await projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            logger.LogWarning("Conversation association failed: project {ProjectId} not found", projectId);
            throw new KeyNotFoundException($"Project with ID '{projectId}' not found.");
        }

        return await projectRepository.AssociateConversationAsync(projectId, conversationId);
    }

    /// <inheritdoc />
    public async Task<bool> DisassociateConversationAsync(Guid conversationId)
    {
        return await projectRepository.DisassociateConversationAsync(conversationId);
    }
}

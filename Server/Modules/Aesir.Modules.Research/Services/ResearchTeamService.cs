using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service implementation for managing research team configurations.
/// Provides business logic layer between controllers and repositories.
/// </summary>
public class ResearchTeamService(
    ILogger<ResearchTeamService> logger,
    IResearchTeamRepository repository,
    IConfigurationService configurationService) : IResearchTeamService
{
    /// <inheritdoc />
    public async Task<ResearchTeam?> GetByIdAsync(Guid id)
    {
        logger.LogDebug("Getting research team {TeamId}", id);
        return await repository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchTeam>> GetByUserIdAsync(string userId)
    {
        logger.LogDebug("Getting research teams for user {UserId}", userId);
        return await repository.GetByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchTeam>> GetActiveByUserIdAsync(string userId)
    {
        logger.LogDebug("Getting active research teams for user {UserId}", userId);
        return await repository.GetActiveByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<ResearchTeam> CreateAsync(ResearchTeam team)
    {
        logger.LogInformation("Creating research team {TeamName} for user {UserId}", team.Name, team.UserId);

        // Validate agent IDs if members are provided
        if (team.Members != null && team.Members.Count > 0)
        {
            var agentIds = team.Members.Select(m => m.AgentId).Distinct().ToList();
            if (!await ValidateAgentIdsAsync(agentIds))
            {
                throw new ArgumentException("One or more agent IDs are invalid");
            }

            // Validate that all required roles are assigned
            ValidateRoleAssignments(team.Members);
        }

        var createdTeam = await repository.AddAsync(team);
        logger.LogInformation("Created research team {TeamId} with {MemberCount} members",
            createdTeam.Id, createdTeam.Members?.Count ?? 0);

        return createdTeam;
    }

    /// <inheritdoc />
    public async Task<ResearchTeam> UpdateAsync(ResearchTeam team)
    {
        logger.LogInformation("Updating research team {TeamId}", team.Id);

        // Verify team exists
        var existingTeam = await repository.GetByIdAsync(team.Id);
        if (existingTeam == null)
        {
            throw new KeyNotFoundException($"Research team {team.Id} not found");
        }

        // Validate agent IDs if members are provided
        if (team.Members != null && team.Members.Count > 0)
        {
            var agentIds = team.Members.Select(m => m.AgentId).Distinct().ToList();
            if (!await ValidateAgentIdsAsync(agentIds))
            {
                throw new ArgumentException("One or more agent IDs are invalid");
            }

            // Validate that all required roles are assigned
            ValidateRoleAssignments(team.Members);
        }

        var success = await repository.UpdateAsync(team);
        if (!success)
        {
            throw new InvalidOperationException($"Failed to update research team {team.Id}");
        }

        logger.LogInformation("Updated research team {TeamId}", team.Id);

        // Return the updated team with members
        return (await repository.GetByIdAsync(team.Id))!;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id)
    {
        logger.LogInformation("Deleting research team {TeamId}", id);

        var result = await repository.RemoveAsync(id);
        if (result)
        {
            logger.LogInformation("Deleted research team {TeamId}", id);
        }
        else
        {
            logger.LogWarning("Research team {TeamId} not found for deletion", id);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateAgentIdsAsync(IEnumerable<Guid> agentIds)
    {
        var agents = await configurationService.GetAgentsAsync();
        var validAgentIds = agents.Select(a => a.Id).ToHashSet();

        foreach (var agentId in agentIds)
        {
            if (!validAgentIds.Contains(agentId))
            {
                logger.LogWarning("Invalid agent ID: {AgentId}", agentId);
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<ResearchTeam> DuplicateAsync(Guid id, string newName)
    {
        logger.LogInformation("Duplicating research team {TeamId} as {NewName}", id, newName);

        var existingTeam = await repository.GetByIdAsync(id);
        if (existingTeam == null)
        {
            throw new KeyNotFoundException($"Research team {id} not found");
        }

        // Create a new team with the same configuration
        var newTeam = new ResearchTeam
        {
            UserId = existingTeam.UserId,
            Name = newName,
            Description = existingTeam.Description,
            IsActive = true,
            Members = existingTeam.Members?.Select(m => new ResearchTeamMember
            {
                Role = m.Role,
                AgentId = m.AgentId,
                IsActive = m.IsActive,
                OverrideTemperature = m.OverrideTemperature,
                OverridePersona = m.OverridePersona,
                OverridePlanningPrompt = m.OverridePlanningPrompt,
                OverrideResearchPrompt = m.OverrideResearchPrompt,
                OverrideThinkingMode = m.OverrideThinkingMode,
                OverrideTools = m.OverrideTools?.ToList()
            }).ToList()
        };

        var createdTeam = await repository.AddAsync(newTeam);
        logger.LogInformation("Duplicated research team {OriginalId} as {NewId}",
            id, createdTeam.Id);

        return createdTeam;
    }

    /// <summary>
    /// Validates that all required research roles are assigned to team members.
    /// </summary>
    private static void ValidateRoleAssignments(List<ResearchTeamMember> members)
    {
        var assignedRoles = members.Where(m => m.IsActive).Select(m => m.Role).ToHashSet();

        // All roles except Chairman are required (Chairman synthesizes, can be optional in Quick mode)
        var requiredRoles = new[]
        {
            ResearchRole.DeepDiver,
            ResearchRole.Synthesizer,
            ResearchRole.DevilsAdvocate
        };

        var missingRoles = requiredRoles.Where(r => !assignedRoles.Contains(r)).ToList();
        if (missingRoles.Count > 0)
        {
            throw new ArgumentException(
                $"Missing required role assignments: {string.Join(", ", missingRoles)}");
        }
    }
}

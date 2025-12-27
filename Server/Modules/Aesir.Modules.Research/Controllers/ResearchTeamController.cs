using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Controllers;

/// <summary>
/// Controller for managing research team configurations.
/// Provides CRUD operations for research teams and their members.
/// </summary>
[ApiController]
[Route("research/teams")]
[Produces("application/json")]
public class ResearchTeamController(
    ILogger<ResearchTeamController> logger,
    IResearchTeamService teamService) : ControllerBase
{
    /// <summary>
    /// Gets all research teams for the current user.
    /// </summary>
    /// <param name="userId">The user ID to filter teams by.</param>
    /// <returns>A list of research teams.</returns>
    [HttpGet]
    public async Task<IActionResult> GetTeams([FromQuery] string userId = "default")
    {
        try
        {
            var teams = await teamService.GetByUserIdAsync(userId);
            return Ok(teams);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving research teams for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving research teams");
        }
    }

    /// <summary>
    /// Gets all active research teams for the current user.
    /// </summary>
    /// <param name="userId">The user ID to filter teams by.</param>
    /// <returns>A list of active research teams.</returns>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTeams([FromQuery] string userId = "default")
    {
        try
        {
            var teams = await teamService.GetActiveByUserIdAsync(userId);
            return Ok(teams);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active research teams for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving research teams");
        }
    }

    /// <summary>
    /// Gets a specific research team by ID.
    /// </summary>
    /// <param name="id">The team ID.</param>
    /// <returns>The research team with its members.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeam(Guid id)
    {
        try
        {
            var team = await teamService.GetByIdAsync(id);
            if (team == null)
            {
                return NotFound();
            }
            return Ok(team);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving research team {TeamId}", id);
            return StatusCode(500, "An error occurred while retrieving the research team");
        }
    }

    /// <summary>
    /// Creates a new research team.
    /// </summary>
    /// <param name="team">The research team to create.</param>
    /// <returns>The created research team.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] ResearchTeam team)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(team.Name))
            {
                return BadRequest("Team name is required");
            }

            if (string.IsNullOrWhiteSpace(team.UserId))
            {
                team.UserId = "default";
            }

            var createdTeam = await teamService.CreateAsync(team);
            return CreatedAtAction(nameof(GetTeam), new { id = createdTeam.Id }, createdTeam);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error creating research team");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating research team");
            return StatusCode(500, "An error occurred while creating the research team");
        }
    }

    /// <summary>
    /// Updates an existing research team.
    /// </summary>
    /// <param name="id">The team ID.</param>
    /// <param name="team">The updated team data.</param>
    /// <returns>The updated research team.</returns>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(Guid id, [FromBody] ResearchTeam team)
    {
        try
        {
            if (id != team.Id)
            {
                return BadRequest("Team ID mismatch");
            }

            if (string.IsNullOrWhiteSpace(team.Name))
            {
                return BadRequest("Team name is required");
            }

            var updatedTeam = await teamService.UpdateAsync(team);
            return Ok(updatedTeam);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Validation error updating research team {TeamId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating research team {TeamId}", id);
            return StatusCode(500, "An error occurred while updating the research team");
        }
    }

    /// <summary>
    /// Deletes a research team.
    /// </summary>
    /// <param name="id">The team ID to delete.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        try
        {
            var result = await teamService.DeleteAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting research team {TeamId}", id);
            return StatusCode(500, "An error occurred while deleting the research team");
        }
    }

    /// <summary>
    /// Duplicates an existing research team with a new name.
    /// </summary>
    /// <param name="id">The team ID to duplicate.</param>
    /// <param name="request">The duplication request with new name.</param>
    /// <returns>The newly created duplicate team.</returns>
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> DuplicateTeam(Guid id, [FromBody] DuplicateTeamRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewName))
            {
                return BadRequest("New team name is required");
            }

            var duplicatedTeam = await teamService.DuplicateAsync(id, request.NewName);
            return CreatedAtAction(nameof(GetTeam), new { id = duplicatedTeam.Id }, duplicatedTeam);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error duplicating research team {TeamId}", id);
            return StatusCode(500, "An error occurred while duplicating the research team");
        }
    }

    /// <summary>
    /// Validates that the provided agent IDs are valid.
    /// </summary>
    /// <param name="agentIds">The agent IDs to validate.</param>
    /// <returns>True if all agent IDs are valid.</returns>
    [HttpPost("validate-agents")]
    public async Task<IActionResult> ValidateAgents([FromBody] List<Guid> agentIds)
    {
        try
        {
            var isValid = await teamService.ValidateAgentIdsAsync(agentIds);
            return Ok(new { IsValid = isValid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating agent IDs");
            return StatusCode(500, "An error occurred while validating agent IDs");
        }
    }
}

/// <summary>
/// Request model for duplicating a research team.
/// </summary>
public class DuplicateTeamRequest
{
    /// <summary>
    /// The name for the duplicated team.
    /// </summary>
    public string NewName { get; set; } = string.Empty;
}

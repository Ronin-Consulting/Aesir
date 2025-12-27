using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Controllers;

/// <summary>
/// Controller for managing research sessions.
/// Provides endpoints for starting research, submitting clarifications, and retrieving results.
/// </summary>
[ApiController]
[Route("research/sessions")]
[Produces("application/json")]
public class ResearchController(
    ILogger<ResearchController> logger,
    IResearchOrchestrator orchestrator,
    IResearchSessionRepository sessionRepository,
    IResearchProgressBroadcaster progressBroadcaster) : ControllerBase
{
    /// <summary>
    /// Gets all research sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID to filter by.</param>
    /// <returns>A list of research sessions.</returns>
    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] string userId = "default")
    {
        try
        {
            var sessions = await sessionRepository.GetByUserIdAsync(userId);
            var sessionList = sessions.ToList();
            var response = new ResearchSessionListResponse
            {
                Sessions = sessionList.Select(ResearchSessionResponse.FromSession).ToList(),
                TotalCount = sessionList.Count
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving research sessions for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving research sessions");
        }
    }

    /// <summary>
    /// Gets a specific research session by ID.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The research session.</returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }
            return Ok(ResearchSessionResponse.FromSession(session));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving research session {SessionId}", id);
            return StatusCode(500, "An error occurred while retrieving the research session");
        }
    }

    /// <summary>
    /// Gets the full report for a completed research session.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The full research report.</returns>
    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            if (session.Report == null)
            {
                return NotFound("Report not yet generated");
            }

            return Ok(session.Report);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving report for session {SessionId}", id);
            return StatusCode(500, "An error occurred while retrieving the research report");
        }
    }

    /// <summary>
    /// Gets the full report as markdown.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The report as markdown text.</returns>
    [HttpGet("{id:guid}/report/markdown")]
    [Produces("text/markdown")]
    public async Task<IActionResult> GetReportMarkdown(Guid id)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            if (session.Report?.FullMarkdown == null)
            {
                return NotFound("Report not yet generated");
            }

            return Content(session.Report.FullMarkdown, "text/markdown");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving markdown report for session {SessionId}", id);
            return StatusCode(500, "An error occurred while retrieving the research report");
        }
    }

    /// <summary>
    /// Starts a new research session.
    /// </summary>
    /// <param name="request">The research session request.</param>
    /// <returns>The created research session.</returns>
    [HttpPost]
    public async Task<IActionResult> StartResearch([FromBody] CreateResearchSessionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest("Research query is required");
            }

            if (request.TeamId == Guid.Empty)
            {
                return BadRequest("Research team ID is required");
            }

            // Create progress callback for SignalR broadcast
            async Task ProgressCallback(ResearchPhaseProgress progress)
            {
                await progressBroadcaster.BroadcastProgressAsync(progress);
            }

            var session = await orchestrator.StartResearchAsync(
                request.Query,
                request.TeamId,
                request.Mode,
                request.DocumentCollectionIds,
                request.UserId,
                ProgressCallback);

            var response = ResearchSessionResponse.FromSession(session);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Research team not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid research configuration");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting research session");
            return StatusCode(500, "An error occurred while starting the research session");
        }
    }

    /// <summary>
    /// Submits clarification answers and continues the research.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <param name="request">The clarification answers.</param>
    /// <returns>The updated research session.</returns>
    [HttpPost("{id:guid}/clarify")]
    public async Task<IActionResult> SubmitClarification(Guid id, [FromBody] SubmitClarificationRequest request)
    {
        try
        {
            if (request.Answers == null || request.Answers.Count == 0)
            {
                return BadRequest("Clarification answers are required");
            }

            // Create progress callback for SignalR broadcast
            async Task ProgressCallback(ResearchPhaseProgress progress)
            {
                await progressBroadcaster.BroadcastProgressAsync(progress);
            }

            await orchestrator.SubmitClarificationAnswersAsync(
                id,
                request.Answers,
                ProgressCallback);

            var session = await orchestrator.GetSessionStatusAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            return Ok(ResearchSessionResponse.FromSession(session));
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Research session not found: {SessionId}", id);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid clarification submission for session {SessionId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error submitting clarification for session {SessionId}", id);
            return StatusCode(500, "An error occurred while submitting clarification answers");
        }
    }

    /// <summary>
    /// Cancels an in-progress research session.
    /// </summary>
    /// <param name="id">The session ID to cancel.</param>
    /// <returns>No content if successful.</returns>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelResearch(Guid id)
    {
        try
        {
            await orchestrator.CancelResearchAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling research session {SessionId}", id);
            return StatusCode(500, "An error occurred while cancelling the research session");
        }
    }

    /// <summary>
    /// Deletes a research session.
    /// </summary>
    /// <param name="id">The session ID to delete.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            await sessionRepository.RemoveAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting research session {SessionId}", id);
            return StatusCode(500, "An error occurred while deleting the research session");
        }
    }
}

using Aesir.Infrastructure.Documents;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DocFormat = Aesir.Infrastructure.Documents.DocumentFormat;

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
    IResearchReportExporter reportExporter) : ControllerBase
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
    /// Gets all research sessions for a specific chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session ID (aesir_chat_session.id) to filter by.</param>
    /// <returns>A list of research sessions linked to the chat session.</returns>
    [HttpGet("chat-session/{chatSessionId:guid}")]
    public async Task<IActionResult> GetSessionsByChatSession(Guid chatSessionId)
    {
        try
        {
            var sessions = await sessionRepository.GetByChatSessionIdAsync(chatSessionId);
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
            logger.LogError(ex, "Error retrieving research sessions for chat session {ChatSessionId}", chatSessionId);
            return StatusCode(500, "An error occurred while retrieving research sessions");
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
        logger.LogDebug("[RESEARCH-API] POST /research/sessions called");
        logger.LogDebug("[RESEARCH-API] Request: Query='{Query}', TeamId={TeamId}, Mode={Mode}, UserId={UserId}, ChatSessionId={ChatSessionId}",
            request.Query, request.TeamId, request.Mode, request.UserId, request.ChatSessionId);
        logger.LogDebug("[RESEARCH-API] DocumentCollectionIds: {Ids}",
            request.DocumentCollectionIds != null ? string.Join(",", request.DocumentCollectionIds) : "null");

        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                logger.LogWarning("[RESEARCH-API] Query is empty or null");
                return BadRequest("Research query is required");
            }

            if (request.TeamId == Guid.Empty)
            {
                logger.LogWarning("[RESEARCH-API] TeamId is empty");
                return BadRequest("Research team ID is required");
            }

            logger.LogDebug("[RESEARCH-API] Calling orchestrator.StartResearchAsync...");
            var session = await orchestrator.StartResearchAsync(
                request.Query,
                request.TeamId,
                request.Mode,
                request.DocumentCollectionIds,
                request.UserId,
                request.ChatSessionId);  // Link research to ChatSession

            logger.LogDebug("[RESEARCH-API] Research session created: Id={SessionId}, Status={Status}",
                session.Id, session.Status);

            var response = ResearchSessionResponse.FromSession(session);
            logger.LogDebug("[RESEARCH-API] Returning CreatedAtAction with session {SessionId}", session.Id);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, response);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "[RESEARCH-API] Research team not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "[RESEARCH-API] Invalid research configuration");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[RESEARCH-API] Error starting research session");
            logger.LogError("[RESEARCH-API] Exception type: {ExType}, Message: {ExMessage}", ex.GetType().Name, ex.Message);
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

            await orchestrator.SubmitClarificationAnswersAsync(
                id,
                request.Answers);

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

    /// <summary>
    /// Exports the research report as a PDF document.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The report as a PDF file.</returns>
    [HttpGet("{id:guid}/report/export/pdf")]
    [Produces("application/pdf")]
    public async Task<IActionResult> ExportReportPdf(Guid id)
    {
        return await ExportReport(id, DocFormat.Pdf);
    }

    /// <summary>
    /// Exports the research report as a Word document.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The report as a Word file.</returns>
    [HttpGet("{id:guid}/report/export/word")]
    [Produces("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task<IActionResult> ExportReportWord(Guid id)
    {
        return await ExportReport(id, DocFormat.Word);
    }

    /// <summary>
    /// Gets the available export formats for reports.
    /// </summary>
    /// <returns>A list of available export formats.</returns>
    [HttpGet("export-formats")]
    public IActionResult GetExportFormats()
    {
        var formats = reportExporter.GetAvailableFormats()
            .Select(f => new
            {
                Format = f.ToString(),
                ContentType = GetContentTypeForFormat(f),
                Extension = GetExtensionForFormat(f)
            })
            .ToList();

        return Ok(formats);
    }

    /// <summary>
    /// Helper method to export a report in the specified format.
    /// </summary>
    private async Task<IActionResult> ExportReport(Guid sessionId, DocFormat format)
    {
        try
        {
            var session = await sessionRepository.GetByIdAsync(sessionId);
            if (session == null)
            {
                return NotFound();
            }

            if (session.Report == null)
            {
                return NotFound("Report not yet generated");
            }

            var result = await reportExporter.ExportAsync(session.Report, format);

            if (!result.Success)
            {
                logger.LogWarning(
                    "Failed to export report for session {SessionId} to {Format}: {Error}",
                    sessionId,
                    format,
                    result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }

            return File(result.Data!, result.ContentType!, result.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting report for session {SessionId} to {Format}", sessionId, format);
            return StatusCode(500, "An error occurred while exporting the research report");
        }
    }

    private static string GetContentTypeForFormat(DocFormat format) => format switch
    {
        DocFormat.Pdf => "application/pdf",
        DocFormat.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        _ => "application/octet-stream"
    };

    private static string GetExtensionForFormat(DocFormat format) => format switch
    {
        DocFormat.Pdf => ".pdf",
        DocFormat.Word => ".docx",
        _ => ".bin"
    };
}

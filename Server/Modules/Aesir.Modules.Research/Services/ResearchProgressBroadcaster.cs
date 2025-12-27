using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Hubs;
using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for broadcasting research progress updates via SignalR.
/// </summary>
public interface IResearchProgressBroadcaster
{
    /// <summary>
    /// Broadcasts a phase progress update to subscribed clients.
    /// </summary>
    /// <param name="progress">The progress update.</param>
    Task BroadcastProgressAsync(ResearchPhaseProgress progress);

    /// <summary>
    /// Broadcasts a status change to subscribed clients.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="phase">The current phase.</param>
    /// <param name="message">Optional status message.</param>
    Task BroadcastStatusChangeAsync(
        Guid sessionId,
        ResearchStatus status,
        ResearchPhase? phase = null,
        string? message = null);

    /// <summary>
    /// Broadcasts a research completion notification.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="reportId">The generated report ID.</param>
    Task BroadcastCompletionAsync(Guid sessionId, Guid reportId);

    /// <summary>
    /// Broadcasts a research error notification.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="errorMessage">The error message.</param>
    Task BroadcastErrorAsync(Guid sessionId, string errorMessage);

    /// <summary>
    /// Sets the current session ID for progress broadcasts.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    void SetCurrentSession(Guid sessionId);
}

/// <summary>
/// Implementation of the research progress broadcaster using SignalR.
/// </summary>
public class ResearchProgressBroadcaster : IResearchProgressBroadcaster
{
    private readonly ILogger<ResearchProgressBroadcaster> _logger;
    private readonly IHubContext<ResearchHub> _hubContext;
    private Guid _currentSessionId;

    public ResearchProgressBroadcaster(
        ILogger<ResearchProgressBroadcaster> logger,
        IHubContext<ResearchHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public void SetCurrentSession(Guid sessionId)
    {
        _currentSessionId = sessionId;
    }

    /// <inheritdoc />
    public async Task BroadcastProgressAsync(ResearchPhaseProgress progress)
    {
        if (_currentSessionId == Guid.Empty)
        {
            _logger.LogWarning("Cannot broadcast progress: no session ID set");
            return;
        }

        try
        {
            var update = new ResearchProgressUpdate
            {
                SessionId = _currentSessionId,
                Status = PhaseToStatus(progress.Phase),
                Phase = progress.Phase,
                Message = progress.Message,
                ProgressPercent = progress.PercentComplete,
                AgentRole = progress.AgentRole,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.SendProgressAsync(
                _currentSessionId,
                "PhaseProgress",
                update);

            _logger.LogDebug(
                "Broadcast progress for session {SessionId}: {Phase} - {Message} ({Percent}%)",
                _currentSessionId, progress.Phase, progress.Message, progress.PercentComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast progress for session {SessionId}", _currentSessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastStatusChangeAsync(
        Guid sessionId,
        ResearchStatus status,
        ResearchPhase? phase = null,
        string? message = null)
    {
        try
        {
            await _hubContext.SendStatusUpdateAsync(sessionId, status, phase, message);

            _logger.LogDebug(
                "Broadcast status change for session {SessionId}: {Status} ({Phase})",
                sessionId, status, phase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast status change for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastCompletionAsync(Guid sessionId, Guid reportId)
    {
        try
        {
            await _hubContext.SendResearchCompletedAsync(sessionId, reportId);

            _logger.LogInformation(
                "Broadcast completion for session {SessionId} with report {ReportId}",
                sessionId, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast completion for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastErrorAsync(Guid sessionId, string errorMessage)
    {
        try
        {
            await _hubContext.SendResearchErrorAsync(sessionId, errorMessage);

            _logger.LogWarning(
                "Broadcast error for session {SessionId}: {Error}",
                sessionId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast error for session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Maps a research phase to its corresponding status.
    /// </summary>
    private static ResearchStatus PhaseToStatus(ResearchPhase phase)
    {
        return phase switch
        {
            ResearchPhase.Clarification => ResearchStatus.AwaitingClarification,
            ResearchPhase.Planning => ResearchStatus.Planning,
            ResearchPhase.Research => ResearchStatus.Researching,
            ResearchPhase.Anonymization => ResearchStatus.Anonymizing,
            ResearchPhase.PeerReview => ResearchStatus.PeerReviewing,
            ResearchPhase.Synthesis => ResearchStatus.Synthesizing,
            _ => ResearchStatus.Researching
        };
    }
}

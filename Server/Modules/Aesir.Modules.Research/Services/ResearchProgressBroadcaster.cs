using Aesir.Common.Models;
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
/// Maps phase-local progress (0-100% within each phase) to overall progress (0-100%)
/// to prevent progress bar resets when transitioning between phases.
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
        _logger.LogDebug("[BROADCASTER] SetCurrentSession: {SessionId}", sessionId);
        _currentSessionId = sessionId;
    }

    /// <inheritdoc />
    public async Task BroadcastProgressAsync(ResearchPhaseProgress progress)
    {
        _logger.LogDebug("[BROADCASTER] BroadcastProgressAsync called:");
        _logger.LogDebug("[BROADCASTER]   Phase: {Phase}", progress.Phase);
        _logger.LogDebug("[BROADCASTER]   AgentRole: {AgentRole}", progress.AgentRole);
        _logger.LogDebug("[BROADCASTER]   Message: {Message}", progress.Message);
        _logger.LogDebug("[BROADCASTER]   PhaseLocalPercent: {Percent}%", progress.PercentComplete);
        _logger.LogDebug("[BROADCASTER]   CurrentSessionId: {SessionId}", _currentSessionId);

        if (_currentSessionId == Guid.Empty)
        {
            _logger.LogWarning("[BROADCASTER] Cannot broadcast progress: no session ID set");
            return;
        }

        try
        {
            // Map phase-local progress to overall progress to prevent resets on phase transitions
            // Uses shared helper from Aesir.Common (cast to int since server uses ResearchPhase enum)
            var overallPercent = ResearchPhaseProgressHelper.MapPhaseProgressToOverall(
                (int)progress.Phase, progress.PercentComplete);

            _logger.LogDebug(
                "[BROADCASTER] Progress mapping: {Phase} {LocalPercent}% (local) → {OverallPercent}% (overall)",
                progress.Phase, progress.PercentComplete, overallPercent);

            var update = new ResearchProgressUpdate
            {
                SessionId = _currentSessionId,
                Status = PhaseToStatus(progress.Phase),
                Phase = progress.Phase,
                Message = progress.Message,
                ProgressPercent = overallPercent, // Use mapped overall progress
                AgentRole = progress.AgentRole,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("[BROADCASTER] Sending 'PhaseProgress' event via SignalR...");
            await _hubContext.SendProgressAsync(
                _currentSessionId,
                "PhaseProgress",
                update).ConfigureAwait(false);

            _logger.LogDebug(
                "[BROADCASTER] SUCCESS - Progress broadcast for session {SessionId}: {Phase} - {Message} ({OverallPercent}% overall, {LocalPercent}% local)",
                _currentSessionId, progress.Phase, progress.Message, overallPercent, progress.PercentComplete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BROADCASTER] FAILED to broadcast progress for session {SessionId}", _currentSessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastStatusChangeAsync(
        Guid sessionId,
        ResearchStatus status,
        ResearchPhase? phase = null,
        string? message = null)
    {
        _logger.LogDebug("[BROADCASTER] BroadcastStatusChangeAsync called:");
        _logger.LogDebug("[BROADCASTER]   SessionId: {SessionId}", sessionId);
        _logger.LogDebug("[BROADCASTER]   Status: {Status}", status);
        _logger.LogDebug("[BROADCASTER]   Phase: {Phase}", phase);
        _logger.LogDebug("[BROADCASTER]   Message: {Message}", message);

        try
        {
            _logger.LogDebug("[BROADCASTER] Sending 'StatusUpdate' event via SignalR...");
            await _hubContext.SendStatusUpdateAsync(sessionId, status, phase, message).ConfigureAwait(false);

            _logger.LogDebug(
                "[BROADCASTER] SUCCESS - Status change broadcast for session {SessionId}: {Status} ({Phase})",
                sessionId, status, phase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BROADCASTER] FAILED to broadcast status change for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastCompletionAsync(Guid sessionId, Guid reportId)
    {
        _logger.LogDebug("[BROADCASTER] BroadcastCompletionAsync called:");
        _logger.LogDebug("[BROADCASTER]   SessionId: {SessionId}", sessionId);
        _logger.LogDebug("[BROADCASTER]   ReportId: {ReportId}", reportId);

        try
        {
            _logger.LogDebug("[BROADCASTER] Sending 'ResearchCompleted' event via SignalR...");
            await _hubContext.SendResearchCompletedAsync(sessionId, reportId).ConfigureAwait(false);

            _logger.LogInformation(
                "[BROADCASTER] SUCCESS - Completion broadcast for session {SessionId} with report {ReportId}",
                sessionId, reportId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BROADCASTER] FAILED to broadcast completion for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastErrorAsync(Guid sessionId, string errorMessage)
    {
        _logger.LogDebug("[BROADCASTER] BroadcastErrorAsync called:");
        _logger.LogDebug("[BROADCASTER]   SessionId: {SessionId}", sessionId);
        _logger.LogDebug("[BROADCASTER]   Error: {Error}", errorMessage);

        try
        {
            _logger.LogDebug("[BROADCASTER] Sending 'ResearchError' event via SignalR...");
            await _hubContext.SendResearchErrorAsync(sessionId, errorMessage).ConfigureAwait(false);

            _logger.LogWarning(
                "[BROADCASTER] SUCCESS - Error broadcast for session {SessionId}: {Error}",
                sessionId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BROADCASTER] FAILED to broadcast error for session {SessionId}", sessionId);
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

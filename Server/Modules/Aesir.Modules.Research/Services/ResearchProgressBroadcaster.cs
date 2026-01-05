using Aesir.Common.Models;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Hubs;
using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for broadcasting research progress updates via SignalR.
/// This service is stateless - session ID is passed explicitly to all methods.
/// </summary>
public interface IResearchProgressBroadcaster
{
    /// <summary>
    /// Broadcasts a unified progress update with optional active agent info.
    /// </summary>
    /// <param name="sessionId">The session ID to broadcast to.</param>
    /// <param name="phase">The current research phase.</param>
    /// <param name="phaseLocalPercent">Progress within the phase (0-100).</param>
    /// <param name="message">Progress description.</param>
    /// <param name="activeAgents">Optional list of currently active agents.</param>
    Task BroadcastProgressAsync(
        Guid sessionId,
        ResearchPhase phase,
        int phaseLocalPercent,
        string message,
        List<ActiveAgentInfo>? activeAgents = null);

    /// <summary>
    /// Legacy method for broadcasting phase progress.
    /// </summary>
    /// <param name="sessionId">The session ID to broadcast to.</param>
    /// <param name="progress">The progress information.</param>
    Task BroadcastProgressAsync(Guid sessionId, ResearchPhaseProgress progress);

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
}

/// <summary>
/// Implementation of the research progress broadcaster using SignalR.
/// This service is stateless - session ID is passed explicitly to all methods.
/// Maps phase-local progress (0-100% within each phase) to overall progress (0-100%)
/// to prevent progress bar resets when transitioning between phases.
/// </summary>
public class ResearchProgressBroadcaster : IResearchProgressBroadcaster
{
    private readonly ILogger<ResearchProgressBroadcaster> _logger;
    private readonly IHubContext<ResearchHub> _hubContext;

    public ResearchProgressBroadcaster(
        ILogger<ResearchProgressBroadcaster> logger,
        IHubContext<ResearchHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task BroadcastProgressAsync(
        Guid sessionId,
        ResearchPhase phase,
        int phaseLocalPercent,
        string message,
        List<ActiveAgentInfo>? activeAgents = null)
    {
        _logger.LogDebug("[BROADCASTER] BroadcastProgressAsync called:");
        _logger.LogDebug("[BROADCASTER]   SessionId: {SessionId}", sessionId);
        _logger.LogDebug("[BROADCASTER]   Phase: {Phase}", phase);
        _logger.LogDebug("[BROADCASTER]   PhaseLocalPercent: {Percent}%", phaseLocalPercent);
        _logger.LogDebug("[BROADCASTER]   Message: {Message}", message);
        _logger.LogDebug("[BROADCASTER]   ActiveAgents: {Count}", activeAgents?.Count ?? 0);

        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("[BROADCASTER] Cannot broadcast progress: session ID is empty");
            return;
        }

        try
        {
            // Map phase-local progress to overall progress to prevent resets on phase transitions
            var overallPercent = ResearchPhaseProgressHelper.MapPhaseProgressToOverall(
                (int)phase, phaseLocalPercent);

            _logger.LogDebug(
                "[BROADCASTER] Progress mapping: {Phase} {LocalPercent}% (local) → {OverallPercent}% (overall)",
                phase, phaseLocalPercent, overallPercent);

            var update = new ResearchProgressUpdate
            {
                SessionId = sessionId,
                Status = PhaseToStatus(phase),
                Phase = phase,
                Message = message,
                ProgressPercent = overallPercent,
                ActiveAgents = activeAgents ?? new List<ActiveAgentInfo>(),
                // For backward compatibility, set AgentRole from first active agent
                AgentRole = activeAgents?.FirstOrDefault()?.Role,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.SendResearchProgressAsync(sessionId, update).ConfigureAwait(false);

            _logger.LogDebug(
                "[BROADCASTER] SUCCESS - Progress broadcast: {Phase} - {Message} ({OverallPercent}% overall, {AgentCount} agents)",
                phase, message, overallPercent, activeAgents?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BROADCASTER] FAILED to broadcast progress for session {SessionId}", sessionId);
        }
    }

    /// <inheritdoc />
    public async Task BroadcastProgressAsync(Guid sessionId, ResearchPhaseProgress progress)
    {
        // Convert legacy ResearchPhaseProgress to new format
        List<ActiveAgentInfo>? activeAgents = null;
        if (progress.AgentRole.HasValue)
        {
            activeAgents = new List<ActiveAgentInfo>
            {
                new ActiveAgentInfo
                {
                    TeamMemberId = Guid.Empty, // Unknown for legacy calls
                    Role = progress.AgentRole.Value,
                    RoleName = GetRoleDisplayName(progress.AgentRole.Value),
                    Activity = progress.Message,
                    AgentProgressPercent = null
                }
            };
        }

        await BroadcastProgressAsync(
            sessionId,
            progress.Phase,
            progress.PercentComplete,
            progress.Message,
            activeAgents).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a human-readable display name for a research role.
    /// </summary>
    private static string GetRoleDisplayName(ResearchRole role)
    {
        return role switch
        {
            ResearchRole.DeepDiver => "Deep Diver",
            ResearchRole.Synthesizer => "Synthesizer",
            ResearchRole.DevilsAdvocate => "Devil's Advocate",
            ResearchRole.Chairman => "Chairman",
            _ => role.ToString()
        };
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

using System.Text.Json;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Implementation of research state management for chat integration.
/// Supports multi-agent tracking with the simplified SignalR event model.
/// </summary>
public class ResearchStateService : IResearchStateService
{
    // TODO: Replace with claims-based user ID from authentication context
    // Must match the value in ChatHistoryService.UserIdValue
    private const string UserIdValue = "blangford@gmail.com";

    private readonly IResearchSessionApiService _sessionApi;
    private readonly IResearchSignalRService _signalRService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ResearchStateService>? _logger;

    // Event handler delegates for proper unsubscription
    private Action<ResearchProgressUpdate>? _researchProgressHandler;
    private Action<ResearchCompletedEvent>? _researchCompletedHandler;
    private Action<ResearchErrorEvent>? _researchErrorHandler;
    private bool _disposed;

    // Multi-agent tracking
    private List<ActiveAgentInfo> _activeAgents = new();

    public ResearchStateService(
        IResearchSessionApiService sessionApi,
        IResearchSignalRService signalRService,
        IChatHistoryService chatHistoryService,
        ILogger<ResearchStateService>? logger = null)
    {
        _sessionApi = sessionApi;
        _signalRService = signalRService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;

        // Wire up SignalR event handlers
        WireUpSignalREvents();
    }

    /// <summary>
    /// Wires up SignalR event handlers for the 3 core events.
    /// Uses named handlers that can be properly unsubscribed in Dispose.
    /// </summary>
    private void WireUpSignalREvents()
    {
        _logger?.LogInformation("[RESEARCH-UI] Wiring up SignalR event handlers (3 core events)");

        _researchProgressHandler = HandleResearchProgressEvent;
        _researchCompletedHandler = HandleResearchCompletedEvent;
        _researchErrorHandler = HandleResearchErrorEvent;

        _signalRService.OnResearchProgress += _researchProgressHandler;
        _signalRService.OnResearchCompleted += _researchCompletedHandler;
        _signalRService.OnResearchError += _researchErrorHandler;
    }

    private void HandleResearchProgressEvent(ResearchProgressUpdate update)
    {
        _logger?.LogDebug("[RESEARCH-UI] SignalR OnResearchProgress received: SessionId={SessionId}, Phase={Phase}, Progress={Progress}%, ActiveAgents={AgentCount}",
            update.SessionId, update.Phase, update.ProgressPercent, update.ActiveAgents.Count);

        if (ActiveSession?.Id != update.SessionId)
        {
            _logger?.LogDebug("[RESEARCH-UI] Ignoring progress update - SessionId mismatch. Active={Active}, Received={Received}",
                ActiveSession?.Id, update.SessionId);
            return;
        }

        _logger?.LogInformation("[RESEARCH-UI] Processing progress update: {Phase} - {Message} ({Percent}%), {AgentCount} active agents",
            update.Phase, update.Message, update.ProgressPercent, update.ActiveAgents.Count);

        // Update multi-agent tracking
        _activeAgents = update.ActiveAgents.ToList();

        // Log active agents for debugging
        foreach (var agent in _activeAgents)
        {
            _logger?.LogDebug("[RESEARCH-UI] Active agent: {RoleName} - {Activity}",
                agent.RoleName, agent.Activity);
        }

        // Create a progress base for backward compatibility
        var progress = new ResearchProgressBase
        {
            SessionId = update.SessionId,
            Status = update.Status,
            Phase = update.Phase,
            Message = update.Message,
            ProgressPercent = update.ProgressPercent,
            AgentRole = update.AgentRole
        };

        HandleProgressUpdate(progress);
    }

    private void HandleResearchCompletedEvent(ResearchCompletedEvent e)
    {
        _logger?.LogDebug("[RESEARCH-UI] SignalR OnResearchCompleted received: SessionId={SessionId}", e.SessionId);

        if (ActiveSession?.Id != e.SessionId)
        {
            _logger?.LogDebug("[RESEARCH-UI] Ignoring completion - SessionId mismatch");
            return;
        }

        _logger?.LogInformation("[RESEARCH-UI] Research completed: {SessionId}, calling RefreshSessionAsync...", e.SessionId);

        // Clear active agents on completion
        _activeAgents.Clear();

        // Refresh to get the full report - with error handling
        _ = RefreshSessionWithErrorHandlingAsync();
    }

    private void HandleResearchErrorEvent(ResearchErrorEvent e)
    {
        _logger?.LogDebug("[RESEARCH-UI] SignalR OnResearchError received: SessionId={SessionId}, Error={Error}",
            e.SessionId, e.ErrorMessage);

        if (ActiveSession?.Id != e.SessionId)
        {
            _logger?.LogDebug("[RESEARCH-UI] Ignoring error - SessionId mismatch");
            return;
        }

        _logger?.LogWarning("[RESEARCH-UI] Research error: {SessionId} - {Error}", e.SessionId, e.ErrorMessage);

        // Clear active agents on error
        _activeAgents.Clear();

        OnResearchError?.Invoke(e.ErrorMessage);
    }

    /// <summary>
    /// Refreshes the session with proper error handling for fire-and-forget scenarios.
    /// </summary>
    private async Task RefreshSessionWithErrorHandlingAsync()
    {
        try
        {
            await RefreshSessionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-UI] Error refreshing session in completion handler");
            OnResearchError?.Invoke($"Failed to refresh research session: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads chat sessions with proper error handling for fire-and-forget scenarios.
    /// </summary>
    private async Task LoadSessionsWithErrorHandlingAsync()
    {
        try
        {
            await _chatHistoryService.LoadSessionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-UI] Error loading chat sessions after research started");
            // Don't invoke OnResearchError here - this is a non-critical background refresh
        }
    }

    /// <inheritdoc />
    public ResearchSessionBase? ActiveSession { get; private set; }

    /// <inheritdoc />
    public bool IsResearchInProgress => ActiveSession != null &&
        ActiveSession.Status != ResearchStatusBase.Completed &&
        ActiveSession.Status != ResearchStatusBase.Failed &&
        ActiveSession.Status != ResearchStatusBase.Cancelled;

    /// <inheritdoc />
    public ResearchTeamBase? SelectedTeam { get; private set; }

    /// <inheritdoc />
    public bool IsTeamSelected => SelectedTeam != null;

    /// <inheritdoc />
    public string? CurrentProgressMessage { get; private set; }

    /// <inheritdoc />
    public int CurrentProgressPercent { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ActiveAgentInfo> ActiveAgents => _activeAgents;

    /// <inheritdoc />
    public bool HasActiveAgents => _activeAgents.Count > 0;

    /// <inheritdoc />
    public ResearchRoleBase? CurrentAgentRole =>
        _activeAgents.FirstOrDefault()?.Role;

    /// <inheritdoc />
    public string? CurrentAgentActivity =>
        _activeAgents.Count > 0 ? FormatMultiAgentActivity() : null;

    /// <inheritdoc />
    public bool IsAgentActive => HasActiveAgents && IsResearchInProgress;

    /// <inheritdoc />
    public event Action? OnSessionChanged;

    /// <inheritdoc />
    public event Action<ResearchProgressBase>? OnProgressUpdate;

    /// <inheritdoc />
    public event Action<ResearchSessionBase>? OnResearchCompleted;

    /// <inheritdoc />
    public event Action<string>? OnResearchError;

    /// <inheritdoc />
    public event Action? OnTeamChanged;

    /// <summary>
    /// Formats the multi-agent activity message.
    /// For single agent: "Deep Diver is researching..."
    /// For multiple agents: "Deep Diver and Synthesizer are researching..."
    /// </summary>
    private string FormatMultiAgentActivity()
    {
        if (_activeAgents.Count == 0)
            return string.Empty;

        if (_activeAgents.Count == 1)
        {
            var agent = _activeAgents[0];
            return $"{agent.RoleName} is {GetActivityVerb(agent.Activity)}...";
        }

        // Multiple agents
        var names = _activeAgents.Select(a => a.RoleName).ToList();
        if (names.Count == 2)
        {
            return $"{names[0]} and {names[1]} are working in parallel...";
        }

        // 3 or more agents
        var lastAgent = names.Last();
        var otherAgents = string.Join(", ", names.Take(names.Count - 1));
        return $"{otherAgents}, and {lastAgent} are working in parallel...";
    }

    /// <summary>
    /// Extracts the activity verb from the activity message.
    /// </summary>
    private static string GetActivityVerb(string activity)
    {
        // Activity is typically something like "Conducting research..." or "Planning research..."
        // Try to extract the key action, or use the whole thing if it's short
        if (string.IsNullOrWhiteSpace(activity))
            return "working";

        // Remove trailing ellipsis if present
        var cleaned = activity.TrimEnd('.').Trim();
        if (cleaned.Length > 40)
            return "working";

        return cleaned.ToLowerInvariant();
    }

    /// <inheritdoc />
    public void SelectTeam(ResearchTeamBase? team)
    {
        if (SelectedTeam?.Id != team?.Id)
        {
            SelectedTeam = team;
            _logger?.LogDebug("Research team selected: {TeamName} ({TeamId})", team?.Name, team?.Id);
            OnTeamChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task<ResearchSessionBase?> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchModeBase mode = ResearchModeBase.Standard,
        List<Guid>? documentCollectionIds = null,
        Guid? conversationId = null)
    {
        var startTime = DateTime.UtcNow;
        _logger?.LogInformation("[RESEARCH-UI] StartResearchAsync called at {Time}", startTime);
        _logger?.LogInformation("[RESEARCH-UI] Query='{Query}', TeamId={TeamId}, UserId={UserId}, ConversationId={ConversationId}",
            query, teamId, UserIdValue, conversationId);

        try
        {
            // Ensure SignalR is connected before starting research
            if (!_signalRService.IsConnected)
            {
                _logger?.LogInformation("[RESEARCH-UI] SignalR not connected, attempting to connect...");
                var connectStart = DateTime.UtcNow;
                var connected = await _signalRService.ConnectAsync();
                var connectElapsed = (DateTime.UtcNow - connectStart).TotalMilliseconds;
                _logger?.LogInformation("[RESEARCH-UI] SignalR connect completed in {Elapsed}ms, connected={Connected}",
                    connectElapsed, connected);
                if (!connected)
                {
                    _logger?.LogWarning("[RESEARCH-UI] Failed to connect to SignalR, research will not receive real-time updates");
                }
            }
            else
            {
                _logger?.LogDebug("[RESEARCH-UI] SignalR already connected");
            }

            var request = new CreateResearchSessionRequestBase
            {
                Query = query,
                TeamId = teamId,
                Mode = mode,
                DocumentCollectionIds = documentCollectionIds,
                ConversationId = conversationId,
                UserId = UserIdValue  // Must match ChatHistoryService.UserIdValue
            };

            _logger?.LogInformation("[RESEARCH-UI] Calling API StartResearchAsync...");
            var apiStart = DateTime.UtcNow;
            var result = await _sessionApi.StartResearchAsync(request);
            var apiElapsed = (DateTime.UtcNow - apiStart).TotalMilliseconds;
            _logger?.LogInformation("[RESEARCH-UI] API StartResearchAsync completed in {Elapsed}ms, success={Success}",
                apiElapsed, result.IsSuccess);

            if (result.IsSuccess && result.Value != null)
            {
                ActiveSession = result.Value;
                CurrentProgressMessage = GetStatusMessage(result.Value.Status);
                CurrentProgressPercent = 0;
                _activeAgents.Clear(); // Reset active agents for new session
                _logger?.LogDebug("[RESEARCH-UI] Session created with status: {Status}", result.Value.Status);
                OnSessionChanged?.Invoke();

                // Subscribe to session updates via SignalR
                if (_signalRService.IsConnected)
                {
                    try
                    {
                        _logger?.LogDebug("[RESEARCH-UI] Subscribing to SignalR session: {SessionId}", result.Value.Id);
                        var subStart = DateTime.UtcNow;
                        await _signalRService.SubscribeToSessionAsync(result.Value.Id);
                        var subElapsed = (DateTime.UtcNow - subStart).TotalMilliseconds;
                        _logger?.LogInformation("[RESEARCH-UI] Subscribed to SignalR session in {Elapsed}ms: {SessionId}",
                            subElapsed, result.Value.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "[RESEARCH-UI] Failed to subscribe to SignalR session");
                    }
                }

                // Refresh chat history so the new research session appears in sidebar immediately
                _logger?.LogDebug("[RESEARCH-UI] Refreshing chat history to show new research session...");
                _ = LoadSessionsWithErrorHandlingAsync();

                var totalElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger?.LogInformation("[RESEARCH-UI] Research session started successfully in {Elapsed}ms: {SessionId}",
                    totalElapsed, result.Value.Id);
                return result.Value;
            }

            _logger?.LogWarning("[RESEARCH-UI] Failed to start research: {Error}, StatusCode={StatusCode}",
                result.Error, result.StatusCode);
            OnResearchError?.Invoke(result.Error ?? "Failed to start research");
            return null;
        }
        catch (Exception ex)
        {
            var totalElapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger?.LogError(ex, "[RESEARCH-UI] Error starting research after {Elapsed}ms: {Message}",
                totalElapsed, ex.Message);
            OnResearchError?.Invoke($"Error starting research: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ResearchSessionBase?> SubmitClarificationAsync(Dictionary<string, string> answers)
    {
        if (ActiveSession == null)
        {
            _logger?.LogWarning("Cannot submit clarification: no active session");
            return null;
        }

        try
        {
            _logger?.LogInformation("Submitting clarification for session {SessionId}", ActiveSession.Id);

            var result = await _sessionApi.SubmitClarificationAsync(ActiveSession.Id, answers);

            if (result.IsSuccess && result.Value != null)
            {
                ActiveSession = result.Value;
                CurrentProgressMessage = GetStatusMessage(result.Value.Status);
                OnSessionChanged?.Invoke();

                _logger?.LogInformation("Clarification submitted, session status: {Status}", result.Value.Status);
                return result.Value;
            }

            _logger?.LogWarning("Failed to submit clarification: {Error}", result.Error);
            OnResearchError?.Invoke(result.Error ?? "Failed to submit clarification");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error submitting clarification");
            OnResearchError?.Invoke($"Error submitting clarification: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task CancelResearchAsync()
    {
        if (ActiveSession == null)
        {
            return;
        }

        try
        {
            _logger?.LogInformation("Cancelling research session {SessionId}", ActiveSession.Id);

            var result = await _sessionApi.CancelResearchAsync(ActiveSession.Id);

            if (result.IsSuccess)
            {
                ActiveSession.Status = ResearchStatusBase.Cancelled;
                _activeAgents.Clear();
                OnSessionChanged?.Invoke();
                _logger?.LogInformation("Research session cancelled");
            }
            else
            {
                _logger?.LogWarning("Failed to cancel research: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error cancelling research");
        }
    }

    /// <inheritdoc />
    public async Task RefreshSessionAsync()
    {
        if (ActiveSession == null)
        {
            _logger?.LogDebug("[RESEARCH-UI] RefreshSessionAsync called but no active session");
            return;
        }

        _logger?.LogDebug("[RESEARCH-UI] RefreshSessionAsync called for session: {SessionId}", ActiveSession.Id);
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _sessionApi.GetSessionAsync(ActiveSession.Id);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger?.LogDebug("[RESEARCH-UI] GetSessionAsync completed in {Elapsed}ms, success={Success}",
                elapsed, result.IsSuccess);

            if (result.IsSuccess && result.Value != null)
            {
                var previousStatus = ActiveSession.Status;
                ActiveSession = result.Value;
                CurrentProgressMessage = GetStatusMessage(result.Value.Status);

                _logger?.LogDebug("[RESEARCH-UI] Session refreshed: {PrevStatus} -> {NewStatus}",
                    previousStatus, result.Value.Status);

                // Check for completion
                if (result.Value.Status == ResearchStatusBase.Completed && previousStatus != ResearchStatusBase.Completed)
                {
                    _logger?.LogInformation("[RESEARCH-UI] Research completed! HasReport={HasReport}",
                        result.Value.Report != null);
                    CurrentProgressPercent = 100;
                    _activeAgents.Clear();
                    OnResearchCompleted?.Invoke(result.Value);
                }
                else if (result.Value.Status == ResearchStatusBase.Failed)
                {
                    _logger?.LogWarning("[RESEARCH-UI] Research failed: {Error}", result.Value.ErrorMessage);
                    _activeAgents.Clear();
                    OnResearchError?.Invoke(result.Value.ErrorMessage ?? "Research failed");
                }

                OnSessionChanged?.Invoke();
            }
            else
            {
                _logger?.LogWarning("[RESEARCH-UI] RefreshSessionAsync failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger?.LogError(ex, "[RESEARCH-UI] Error refreshing session after {Elapsed}ms", elapsed);
        }
    }

    /// <inheritdoc />
    public void HandleProgressUpdate(ResearchProgressBase progress)
    {
        if (ActiveSession == null || ActiveSession.Id != progress.SessionId)
        {
            return;
        }

        // Track if phase changed (to clear agent activity on phase transitions)
        var previousPhase = ActiveSession.CurrentPhase;
        var phaseChanged = previousPhase != progress.Phase;

        CurrentProgressMessage = progress.Message;

        // Validate progress never decreases (safety net for network delays or out-of-order messages)
        // The backend now sends overall progress, but we add this validation as a safety net
        var receivedPercent = progress.ProgressPercent;
        var validatedPercent = Math.Max(CurrentProgressPercent, receivedPercent);

        if (validatedPercent != receivedPercent)
        {
            _logger?.LogWarning(
                "[RESEARCH-UI] Progress validation: received {ReceivedPercent}%, validated to {ValidatedPercent}% (prevented decrease from backend)",
                receivedPercent, validatedPercent);
        }

        CurrentProgressPercent = validatedPercent;
        ActiveSession.Status = progress.Status;
        ActiveSession.CurrentPhase = progress.Phase;

        // Note: Agent activity is now managed via _activeAgents list from SignalR events
        // The legacy AgentRole in progress is still supported for backward compatibility

        _logger?.LogDebug("Research progress: {Phase} - {Message} ({Percent}%), {AgentCount} active agents",
            progress.Phase, progress.Message, CurrentProgressPercent, _activeAgents.Count);

        OnProgressUpdate?.Invoke(progress);
        OnSessionChanged?.Invoke();
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        ActiveSession = null;
        CurrentProgressMessage = null;
        CurrentProgressPercent = 0;
        _activeAgents.Clear();
        OnSessionChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task<bool> RestoreActiveSessionAsync()
    {
        _logger?.LogDebug("[RESEARCH-UI] RestoreActiveSessionAsync called - checking for in-progress research");

        try
        {
            // Query for in-progress research sessions for this user
            var result = await _sessionApi.GetSessionsAsync(UserIdValue);

            if (!result.IsSuccess || result.Value?.Sessions == null)
            {
                _logger?.LogDebug("[RESEARCH-UI] No sessions found or error fetching sessions");
                return false;
            }

            // Find any session that is still in progress
            var inProgressSession = result.Value.Sessions.FirstOrDefault(s =>
                s.Status != ResearchStatusBase.Completed &&
                s.Status != ResearchStatusBase.Failed &&
                s.Status != ResearchStatusBase.Cancelled);

            if (inProgressSession == null)
            {
                _logger?.LogDebug("[RESEARCH-UI] No in-progress research sessions found");
                return false;
            }

            _logger?.LogInformation("[RESEARCH-UI] Found in-progress research session: {SessionId}, Status={Status}",
                inProgressSession.Id, inProgressSession.Status);

            // Restore the active session
            ActiveSession = inProgressSession;
            CurrentProgressMessage = GetStatusMessage(inProgressSession.Status);
            CurrentProgressPercent = EstimateProgressFromPhase(inProgressSession.CurrentPhase ?? ResearchPhaseBase.Planning);
            _activeAgents.Clear(); // Will be populated by SignalR events

            // Reconnect to SignalR for updates
            if (!_signalRService.IsConnected)
            {
                _logger?.LogDebug("[RESEARCH-UI] Connecting to SignalR for session updates...");
                await _signalRService.ConnectAsync();
            }

            if (_signalRService.IsConnected)
            {
                _logger?.LogDebug("[RESEARCH-UI] Subscribing to SignalR session: {SessionId}", inProgressSession.Id);
                await _signalRService.SubscribeToSessionAsync(inProgressSession.Id);
            }

            OnSessionChanged?.Invoke();

            _logger?.LogInformation("[RESEARCH-UI] Successfully restored in-progress research session: {SessionId}",
                inProgressSession.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-UI] Error restoring active session");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreSessionForConversationAsync(Guid conversationId)
    {
        _logger?.LogDebug("[RESEARCH-UI] RestoreSessionForConversationAsync called for conversation: {ConversationId}", conversationId);

        try
        {
            // Query for research sessions linked to this conversation
            var result = await _sessionApi.GetSessionsByConversationAsync(conversationId);

            if (!result.IsSuccess || result.Value?.Sessions == null || result.Value.Sessions.Count == 0)
            {
                _logger?.LogDebug("[RESEARCH-UI] No research sessions found for conversation {ConversationId}", conversationId);
                // Clear active session if navigating to a conversation without research
                ClearSession();
                return false;
            }

            // Prioritize in-progress sessions over completed ones
            var inProgressSession = result.Value.Sessions.FirstOrDefault(s =>
                s.Status != ResearchStatusBase.Completed &&
                s.Status != ResearchStatusBase.Failed &&
                s.Status != ResearchStatusBase.Cancelled);

            // Fall back to most recent completed session if no in-progress
            var sessionToRestore = inProgressSession ?? result.Value.Sessions.FirstOrDefault(s =>
                s.Status == ResearchStatusBase.Completed);

            if (sessionToRestore == null)
            {
                _logger?.LogDebug("[RESEARCH-UI] No active or completed research sessions for conversation");
                ClearSession();
                return false;
            }

            _logger?.LogInformation("[RESEARCH-UI] Found research session for conversation: {SessionId}, Status={Status}",
                sessionToRestore.Id, sessionToRestore.Status);

            // Restore the session
            ActiveSession = sessionToRestore;
            CurrentProgressMessage = GetStatusMessage(sessionToRestore.Status);
            CurrentProgressPercent = sessionToRestore.Status == ResearchStatusBase.Completed
                ? 100
                : EstimateProgressFromPhase(sessionToRestore.CurrentPhase ?? ResearchPhaseBase.Planning);
            _activeAgents.Clear(); // Will be populated by SignalR events if in-progress

            // Only reconnect to SignalR for in-progress sessions
            var isInProgress = sessionToRestore.Status != ResearchStatusBase.Completed &&
                               sessionToRestore.Status != ResearchStatusBase.Failed &&
                               sessionToRestore.Status != ResearchStatusBase.Cancelled;

            if (isInProgress)
            {
                if (!_signalRService.IsConnected)
                {
                    _logger?.LogDebug("[RESEARCH-UI] Connecting to SignalR for session updates...");
                    await _signalRService.ConnectAsync();
                }

                if (_signalRService.IsConnected)
                {
                    _logger?.LogDebug("[RESEARCH-UI] Subscribing to SignalR session: {SessionId}", sessionToRestore.Id);
                    await _signalRService.SubscribeToSessionAsync(sessionToRestore.Id);
                }
            }

            OnSessionChanged?.Invoke();

            _logger?.LogInformation("[RESEARCH-UI] Successfully restored research session for conversation: {SessionId}",
                sessionToRestore.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[RESEARCH-UI] Error restoring session for conversation {ConversationId}", conversationId);
            return false;
        }
    }

    /// <summary>
    /// Estimates progress percentage based on current phase.
    /// Uses the shared helper from Aesir.Common for consistent progress estimation.
    /// </summary>
    private static int EstimateProgressFromPhase(ResearchPhaseBase phase)
    {
        return ResearchPhaseProgressHelper.EstimateProgressFromPhase(phase);
    }

    private static string GetStatusMessage(ResearchStatusBase status)
    {
        return status switch
        {
            ResearchStatusBase.Created => "Initializing research...",
            ResearchStatusBase.AwaitingClarification => "Awaiting clarification answers...",
            ResearchStatusBase.Planning => "Agents are planning their research...",
            ResearchStatusBase.Researching => "Agents are conducting research...",
            ResearchStatusBase.Anonymizing => "Preparing submissions for peer review...",
            ResearchStatusBase.PeerReviewing => "Agents are reviewing each other's work...",
            ResearchStatusBase.Synthesizing => "Chairman is synthesizing the final report...",
            ResearchStatusBase.Completed => "Research complete!",
            ResearchStatusBase.Failed => "Research failed",
            ResearchStatusBase.Cancelled => "Research cancelled",
            _ => "Processing..."
        };
    }

    /// <summary>
    /// Disposes resources and unsubscribes from SignalR events.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Unsubscribe from SignalR events to prevent memory leaks
            if (_researchProgressHandler != null)
                _signalRService.OnResearchProgress -= _researchProgressHandler;
            if (_researchCompletedHandler != null)
                _signalRService.OnResearchCompleted -= _researchCompletedHandler;
            if (_researchErrorHandler != null)
                _signalRService.OnResearchError -= _researchErrorHandler;

            _logger?.LogDebug("[RESEARCH-UI] ResearchStateService disposed, event handlers unsubscribed");
        }

        _disposed = true;
    }
}

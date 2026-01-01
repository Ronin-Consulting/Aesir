using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Implementation of research state management for chat integration.
/// </summary>
public class ResearchStateService : IResearchStateService
{
    private readonly IResearchSessionApiService _sessionApi;
    private readonly IResearchSignalRService _signalRService;
    private readonly ILogger<ResearchStateService>? _logger;

    public ResearchStateService(
        IResearchSessionApiService sessionApi,
        IResearchSignalRService signalRService,
        ILogger<ResearchStateService>? logger = null)
    {
        _sessionApi = sessionApi;
        _signalRService = signalRService;
        _logger = logger;

        // Wire up SignalR event handlers
        WireUpSignalREvents();
    }

    /// <summary>
    /// Wires up SignalR event handlers to update state.
    /// </summary>
    private void WireUpSignalREvents()
    {
        _signalRService.OnStatusUpdate += (update) =>
        {
            if (ActiveSession?.Id != update.SessionId) return;

            _logger?.LogDebug("SignalR status update: {Status} - {Phase} - {Message}",
                update.Status, update.Phase, update.Message);

            HandleProgressUpdate(new ResearchProgressBase
            {
                SessionId = update.SessionId,
                Status = update.Status,
                Phase = update.Phase ?? ResearchPhaseBase.Planning,
                Message = update.Message ?? GetStatusMessage(update.Status),
                ProgressPercent = CurrentProgressPercent
            });
        };

        _signalRService.OnResearchCompleted += (e) =>
        {
            if (ActiveSession?.Id != e.SessionId) return;

            _logger?.LogInformation("SignalR research completed: {SessionId}", e.SessionId);

            // Refresh to get the full report
            _ = RefreshSessionAsync();
        };

        _signalRService.OnResearchError += (e) =>
        {
            if (ActiveSession?.Id != e.SessionId) return;

            _logger?.LogWarning("SignalR research error: {SessionId} - {Error}", e.SessionId, e.ErrorMessage);
            OnResearchError?.Invoke(e.ErrorMessage);
        };

        _signalRService.OnProgress += (e) =>
        {
            if (ActiveSession?.Id != e.SessionId) return;

            _logger?.LogDebug("SignalR progress event: {EventType}", e.EventType);
            // Handle generic progress events if needed
        };
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
    public event Action? OnSessionChanged;

    /// <inheritdoc />
    public event Action<ResearchProgressBase>? OnProgressUpdate;

    /// <inheritdoc />
    public event Action<ResearchSessionBase>? OnResearchCompleted;

    /// <inheritdoc />
    public event Action<string>? OnResearchError;

    /// <inheritdoc />
    public event Action? OnTeamChanged;

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
        List<Guid>? documentCollectionIds = null)
    {
        try
        {
            _logger?.LogInformation("Starting research for team {TeamId}: {Query}", teamId, query);

            // Ensure SignalR is connected before starting research
            if (!_signalRService.IsConnected)
            {
                _logger?.LogDebug("Connecting to SignalR...");
                var connected = await _signalRService.ConnectAsync();
                if (!connected)
                {
                    _logger?.LogWarning("Failed to connect to SignalR, research will not receive real-time updates");
                }
            }

            var request = new CreateResearchSessionRequestBase
            {
                Query = query,
                TeamId = teamId,
                Mode = mode,
                DocumentCollectionIds = documentCollectionIds
            };

            var result = await _sessionApi.StartResearchAsync(request);

            if (result.IsSuccess && result.Value != null)
            {
                ActiveSession = result.Value;
                CurrentProgressMessage = GetStatusMessage(result.Value.Status);
                CurrentProgressPercent = 0;
                OnSessionChanged?.Invoke();

                // Subscribe to session updates via SignalR
                if (_signalRService.IsConnected)
                {
                    try
                    {
                        await _signalRService.SubscribeToSessionAsync(result.Value.Id);
                        _logger?.LogDebug("Subscribed to SignalR session: {SessionId}", result.Value.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to subscribe to SignalR session, polling will be used instead");
                    }
                }

                _logger?.LogInformation("Research session started: {SessionId}", result.Value.Id);
                return result.Value;
            }

            _logger?.LogWarning("Failed to start research: {Error}", result.Error);
            OnResearchError?.Invoke(result.Error ?? "Failed to start research");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting research");
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
            return;
        }

        try
        {
            var result = await _sessionApi.GetSessionAsync(ActiveSession.Id);

            if (result.IsSuccess && result.Value != null)
            {
                var previousStatus = ActiveSession.Status;
                ActiveSession = result.Value;
                CurrentProgressMessage = GetStatusMessage(result.Value.Status);

                // Check for completion
                if (result.Value.Status == ResearchStatusBase.Completed && previousStatus != ResearchStatusBase.Completed)
                {
                    CurrentProgressPercent = 100;
                    OnResearchCompleted?.Invoke(result.Value);
                }
                else if (result.Value.Status == ResearchStatusBase.Failed)
                {
                    OnResearchError?.Invoke(result.Value.ErrorMessage ?? "Research failed");
                }

                OnSessionChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing session");
        }
    }

    /// <inheritdoc />
    public void HandleProgressUpdate(ResearchProgressBase progress)
    {
        if (ActiveSession == null || ActiveSession.Id != progress.SessionId)
        {
            return;
        }

        CurrentProgressMessage = progress.Message;
        CurrentProgressPercent = progress.ProgressPercent;
        ActiveSession.Status = progress.Status;
        ActiveSession.CurrentPhase = progress.Phase;

        _logger?.LogDebug("Research progress: {Phase} - {Message} ({Percent}%)",
            progress.Phase, progress.Message, progress.ProgressPercent);

        OnProgressUpdate?.Invoke(progress);
        OnSessionChanged?.Invoke();
    }

    /// <inheritdoc />
    public void ClearSession()
    {
        ActiveSession = null;
        CurrentProgressMessage = null;
        CurrentProgressPercent = 0;
        OnSessionChanged?.Invoke();
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
}

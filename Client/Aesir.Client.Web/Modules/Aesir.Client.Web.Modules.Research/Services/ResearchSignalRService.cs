using System.Collections.Concurrent;
using Aesir.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Research.Services;

/// <summary>
/// SignalR-based service for receiving real-time research session updates.
/// </summary>
public class ResearchSignalRService : IResearchSignalRService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ResearchSignalRService>? _logger;
    private HubConnection? _hubConnection;
    private bool _disposed;
    private readonly ConcurrentDictionary<Guid, byte> _subscribedSessions = new();

    /// <inheritdoc />
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    /// <inheritdoc />
    public event Action<ResearchStatusUpdate>? OnStatusUpdate;

    /// <inheritdoc />
    public event Action<ResearchAgentEvent>? OnAgentStarted;

    /// <inheritdoc />
    public event Action<ResearchAgentCompletedEvent>? OnAgentCompleted;

    /// <inheritdoc />
    public event Action<ResearchPeerReviewEvent>? OnPeerReviewCompleted;

    /// <inheritdoc />
    public event Action<ResearchCompletedEvent>? OnResearchCompleted;

    /// <inheritdoc />
    public event Action<ResearchErrorEvent>? OnResearchError;

    /// <inheritdoc />
    public event Action<ResearchProgressEvent>? OnProgress;

    /// <summary>
    /// Creates a new ResearchSignalRService.
    /// </summary>
    public ResearchSignalRService(IConfiguration configuration, ILogger<ResearchSignalRService>? logger = null)
    {
        _configuration = configuration;
        _logger = logger;
        _logger?.LogInformation("[RESEARCH-SIGNALR] Service created");
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("[RESEARCH-SIGNALR] ConnectAsync called, disposed={Disposed}, connected={Connected}",
            _disposed, IsConnected);

        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (IsConnected)
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] Already connected, returning true");
            return true;
        }

        var startTime = DateTime.UtcNow;
        try
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://aesir.localhost";
            var hubUrl = $"{baseUrl}/researchhub";
            _logger?.LogInformation("[RESEARCH-SIGNALR] Connecting to hub at: {HubUrl}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Register event handlers
            RegisterEventHandlers();

            _logger?.LogDebug("[RESEARCH-SIGNALR] Starting hub connection...");
            await _hubConnection.StartAsync(cancellationToken);
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger?.LogInformation("[RESEARCH-SIGNALR] Connected to Research hub in {Elapsed}ms, state={State}",
                elapsed, _hubConnection.State);
            return true;
        }
        catch (Exception ex)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger?.LogError(ex, "[RESEARCH-SIGNALR] Failed to connect to Research hub after {Elapsed}ms: {Message}",
                elapsed, ex.Message);
            return false;
        }
    }

    private void RegisterEventHandlers()
    {
        if (_hubConnection == null) return;
        _logger?.LogDebug("[RESEARCH-SIGNALR] Registering event handlers");

        _hubConnection.On<StatusUpdateDto>("StatusUpdate", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] StatusUpdate received: SessionId={SessionId}, Status={Status}, Phase={Phase}",
                dto.SessionId, dto.Status, dto.Phase);
            OnStatusUpdate?.Invoke(new ResearchStatusUpdate(
                dto.SessionId,
                dto.Status,
                dto.Phase,
                dto.Message,
                dto.Timestamp));
        });

        _hubConnection.On<AgentStartedDto>("AgentStarted", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] AgentStarted received: SessionId={SessionId}, Role={Role}",
                dto.SessionId, dto.Role);
            OnAgentStarted?.Invoke(new ResearchAgentEvent(
                dto.SessionId,
                dto.Role,
                dto.AgentId,
                dto.Timestamp));
        });

        _hubConnection.On<AgentCompletedDto>("AgentCompleted", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] AgentCompleted received: SessionId={SessionId}, Role={Role}",
                dto.SessionId, dto.Role);
            OnAgentCompleted?.Invoke(new ResearchAgentCompletedEvent(
                dto.SessionId,
                dto.Role,
                dto.SubmissionId,
                dto.TokensUsed,
                dto.Timestamp));
        });

        _hubConnection.On<PeerReviewCompletedDto>("PeerReviewCompleted", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] PeerReviewCompleted received: SessionId={SessionId}",
                dto.SessionId);
            OnPeerReviewCompleted?.Invoke(new ResearchPeerReviewEvent(
                dto.SessionId,
                dto.ReviewId,
                dto.ReviewerRole,
                dto.WeightedScore,
                dto.Timestamp));
        });

        _hubConnection.On<ResearchCompletedDto>("ResearchCompleted", dto =>
        {
            _logger?.LogInformation("[RESEARCH-SIGNALR] ResearchCompleted received: SessionId={SessionId}, ReportId={ReportId}",
                dto.SessionId, dto.ReportId);
            OnResearchCompleted?.Invoke(new ResearchCompletedEvent(
                dto.SessionId,
                dto.ReportId,
                dto.Timestamp));
        });

        _hubConnection.On<ResearchErrorDto>("ResearchError", dto =>
        {
            _logger?.LogWarning("[RESEARCH-SIGNALR] ResearchError received: SessionId={SessionId}, Error={Error}",
                dto.SessionId, dto.ErrorMessage);
            OnResearchError?.Invoke(new ResearchErrorEvent(
                dto.SessionId,
                dto.ErrorMessage,
                dto.Timestamp));
        });

        _hubConnection.On<ProgressDto>("Progress", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] Progress received: SessionId={SessionId}, EventType={EventType}",
                dto.SessionId, dto.EventType);
            OnProgress?.Invoke(new ResearchProgressEvent(
                dto.SessionId,
                dto.EventType,
                dto.Data,
                dto.Timestamp));
        });

        // Handle reconnection - resubscribe to all sessions
        _hubConnection.Reconnected += async _ =>
        {
            // Take a thread-safe snapshot of session IDs to avoid collection modification during enumeration
            var sessionIds = _subscribedSessions.Keys.ToArray();
            _logger?.LogInformation("[RESEARCH-SIGNALR] Reconnected to hub, resubscribing to {Count} sessions",
                sessionIds.Length);
            foreach (var sessionId in sessionIds)
            {
                _logger?.LogDebug("[RESEARCH-SIGNALR] Resubscribing to session: {SessionId}", sessionId);
                await _hubConnection.InvokeAsync("SubscribeToSession", sessionId);
            }
        };

        _hubConnection.Closed += error =>
        {
            _logger?.LogWarning("[RESEARCH-SIGNALR] Connection closed: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnecting += error =>
        {
            _logger?.LogWarning("[RESEARCH-SIGNALR] Reconnecting: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _logger?.LogDebug("[RESEARCH-SIGNALR] Event handlers registered");
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        _logger?.LogDebug("[RESEARCH-SIGNALR] DisconnectAsync called");
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            _logger?.LogInformation("[RESEARCH-SIGNALR] Disconnected from Research hub");
        }
        _subscribedSessions.Clear();
    }

    /// <inheritdoc />
    public async Task SubscribeToSessionAsync(Guid sessionId)
    {
        _logger?.LogDebug("[RESEARCH-SIGNALR] SubscribeToSessionAsync called: {SessionId}", sessionId);

        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            _logger?.LogWarning("[RESEARCH-SIGNALR] Cannot subscribe - hub not connected (state={State})",
                _hubConnection?.State);
            throw new InvalidOperationException("Research hub is not connected. Call ConnectAsync first.");
        }

        var startTime = DateTime.UtcNow;
        await _hubConnection.InvokeAsync("SubscribeToSession", sessionId);
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
        _subscribedSessions.TryAdd(sessionId, 0);
        _logger?.LogInformation("[RESEARCH-SIGNALR] Subscribed to session in {Elapsed}ms: {SessionId}", elapsed, sessionId);
    }

    /// <inheritdoc />
    public async Task UnsubscribeFromSessionAsync(Guid sessionId)
    {
        _logger?.LogDebug("[RESEARCH-SIGNALR] UnsubscribeFromSessionAsync called: {SessionId}", sessionId);

        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromSession", sessionId);
            _logger?.LogDebug("[RESEARCH-SIGNALR] Unsubscribed from session: {SessionId}", sessionId);
        }
        _subscribedSessions.TryRemove(sessionId, out _);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        await DisconnectAsync();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        GC.SuppressFinalize(this);
    }

    // DTOs for deserialization
    private record StatusUpdateDto(
        Guid SessionId,
        ResearchStatusBase Status,
        ResearchPhaseBase? Phase,
        string? Message,
        DateTime Timestamp);

    private record AgentStartedDto(
        Guid SessionId,
        ResearchRoleBase Role,
        Guid AgentId,
        DateTime Timestamp);

    private record AgentCompletedDto(
        Guid SessionId,
        ResearchRoleBase Role,
        Guid SubmissionId,
        int? TokensUsed,
        DateTime Timestamp);

    private record PeerReviewCompletedDto(
        Guid SessionId,
        Guid ReviewId,
        ResearchRoleBase ReviewerRole,
        double WeightedScore,
        DateTime Timestamp);

    private record ResearchCompletedDto(
        Guid SessionId,
        Guid ReportId,
        DateTime Timestamp);

    private record ResearchErrorDto(
        Guid SessionId,
        string ErrorMessage,
        DateTime Timestamp);

    private record ProgressDto(
        Guid SessionId,
        string EventType,
        object? Data,
        DateTime Timestamp);
}

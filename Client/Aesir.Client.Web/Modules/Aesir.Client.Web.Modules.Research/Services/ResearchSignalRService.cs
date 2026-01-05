using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Aesir.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Web.Modules.Research.Services;

/// <summary>
/// SignalR-based service for receiving real-time research session updates.
/// Simplified to handle 3 core events: ResearchProgress, ResearchCompleted, ResearchError.
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
    public event Action<ResearchProgressUpdate>? OnResearchProgress;

    /// <inheritdoc />
    public event Action<ResearchCompletedEvent>? OnResearchCompleted;

    /// <inheritdoc />
    public event Action<ResearchErrorEvent>? OnResearchError;

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

            // Register event handlers for the 3 core events
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
        _logger?.LogDebug("[RESEARCH-SIGNALR] Registering 3 core event handlers");

        // Unified progress event with multi-agent tracking
        _hubConnection.On<ResearchProgressDto>("ResearchProgress", dto =>
        {
            _logger?.LogDebug("[RESEARCH-SIGNALR] ResearchProgress received: SessionId={SessionId}, Phase={Phase}, Progress={Progress}%, ActiveAgents={AgentCount}",
                dto.SessionId, dto.Phase, dto.ProgressPercent, dto.ActiveAgents?.Count ?? 0);

            OnResearchProgress?.Invoke(new ResearchProgressUpdate(
                dto.SessionId,
                dto.Status,
                dto.Phase,
                dto.Message ?? string.Empty,
                dto.ProgressPercent,
                dto.ActiveAgents ?? new List<ActiveAgentInfo>(),
                dto.AgentRole,
                dto.Timestamp));
        });

        // Research completed event
        _hubConnection.On<ResearchCompletedDto>("ResearchCompleted", dto =>
        {
            _logger?.LogInformation("[RESEARCH-SIGNALR] ResearchCompleted received: SessionId={SessionId}, ReportId={ReportId}",
                dto.SessionId, dto.ReportId);
            OnResearchCompleted?.Invoke(new ResearchCompletedEvent(
                dto.SessionId,
                dto.ReportId,
                dto.Timestamp));
        });

        // Research error event
        _hubConnection.On<ResearchErrorDto>("ResearchError", dto =>
        {
            _logger?.LogWarning("[RESEARCH-SIGNALR] ResearchError received: SessionId={SessionId}, Error={Error}",
                dto.SessionId, dto.ErrorMessage);
            OnResearchError?.Invoke(new ResearchErrorEvent(
                dto.SessionId,
                dto.ErrorMessage,
                dto.Timestamp));
        });

        // Handle reconnection - resubscribe to all sessions
        _hubConnection.Reconnected += async _ =>
        {
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

    // DTOs for deserialization - use snake_case to match server JSON

    private record ResearchProgressDto(
        [property: JsonPropertyName("session_id")] Guid SessionId,
        [property: JsonPropertyName("status")] ResearchStatusBase Status,
        [property: JsonPropertyName("phase")] ResearchPhaseBase Phase,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("progress_percent")] int ProgressPercent,
        [property: JsonPropertyName("active_agents")] List<ActiveAgentInfo>? ActiveAgents,
        [property: JsonPropertyName("agent_role")] ResearchRoleBase? AgentRole,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp);

    private record ResearchCompletedDto(
        [property: JsonPropertyName("session_id")] Guid SessionId,
        [property: JsonPropertyName("report_id")] Guid ReportId,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp);

    private record ResearchErrorDto(
        [property: JsonPropertyName("session_id")] Guid SessionId,
        [property: JsonPropertyName("error_message")] string ErrorMessage,
        [property: JsonPropertyName("timestamp")] DateTime Timestamp);
}

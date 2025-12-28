using Aesir.Common.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace Aesir.Client.Web.Modules.Research.Services;

/// <summary>
/// SignalR-based service for receiving real-time research session updates.
/// </summary>
public class ResearchSignalRService : IResearchSignalRService
{
    private readonly IConfiguration _configuration;
    private HubConnection? _hubConnection;
    private bool _disposed;
    private readonly HashSet<Guid> _subscribedSessions = [];

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
    public ResearchSignalRService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (IsConnected)
            return true;

        try
        {
            var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://aesir.localhost";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/researchhub")
                .WithAutomaticReconnect()
                .Build();

            // Register event handlers
            RegisterEventHandlers();

            await _hubConnection.StartAsync(cancellationToken);

            Console.WriteLine("Connected to Research hub");
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to connect to Research hub: {ex.Message}");
            return false;
        }
    }

    private void RegisterEventHandlers()
    {
        if (_hubConnection == null) return;

        _hubConnection.On<StatusUpdateDto>("StatusUpdate", dto =>
        {
            OnStatusUpdate?.Invoke(new ResearchStatusUpdate(
                dto.SessionId,
                dto.Status,
                dto.Phase,
                dto.Message,
                dto.Timestamp));
        });

        _hubConnection.On<AgentStartedDto>("AgentStarted", dto =>
        {
            OnAgentStarted?.Invoke(new ResearchAgentEvent(
                dto.SessionId,
                dto.Role,
                dto.AgentId,
                dto.Timestamp));
        });

        _hubConnection.On<AgentCompletedDto>("AgentCompleted", dto =>
        {
            OnAgentCompleted?.Invoke(new ResearchAgentCompletedEvent(
                dto.SessionId,
                dto.Role,
                dto.SubmissionId,
                dto.TokensUsed,
                dto.Timestamp));
        });

        _hubConnection.On<PeerReviewCompletedDto>("PeerReviewCompleted", dto =>
        {
            OnPeerReviewCompleted?.Invoke(new ResearchPeerReviewEvent(
                dto.SessionId,
                dto.ReviewId,
                dto.ReviewerRole,
                dto.WeightedScore,
                dto.Timestamp));
        });

        _hubConnection.On<ResearchCompletedDto>("ResearchCompleted", dto =>
        {
            OnResearchCompleted?.Invoke(new ResearchCompletedEvent(
                dto.SessionId,
                dto.ReportId,
                dto.Timestamp));
        });

        _hubConnection.On<ResearchErrorDto>("ResearchError", dto =>
        {
            OnResearchError?.Invoke(new ResearchErrorEvent(
                dto.SessionId,
                dto.ErrorMessage,
                dto.Timestamp));
        });

        _hubConnection.On<ProgressDto>("Progress", dto =>
        {
            OnProgress?.Invoke(new ResearchProgressEvent(
                dto.SessionId,
                dto.EventType,
                dto.Data,
                dto.Timestamp));
        });

        // Handle reconnection - resubscribe to all sessions
        _hubConnection.Reconnected += async _ =>
        {
            Console.WriteLine("Reconnected to Research hub, resubscribing to sessions...");
            foreach (var sessionId in _subscribedSessions)
            {
                await _hubConnection.InvokeAsync("SubscribeToSession", sessionId);
            }
        };
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            Console.WriteLine("Disconnected from Research hub");
        }
        _subscribedSessions.Clear();
    }

    /// <inheritdoc />
    public async Task SubscribeToSessionAsync(Guid sessionId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("Research hub is not connected. Call ConnectAsync first.");
        }

        await _hubConnection.InvokeAsync("SubscribeToSession", sessionId);
        _subscribedSessions.Add(sessionId);
        Console.WriteLine($"Subscribed to research session: {sessionId}");
    }

    /// <inheritdoc />
    public async Task UnsubscribeFromSessionAsync(Guid sessionId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ResearchSignalRService));

        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("UnsubscribeFromSession", sessionId);
        }
        _subscribedSessions.Remove(sessionId);
        Console.WriteLine($"Unsubscribed from research session: {sessionId}");
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

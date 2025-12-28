using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Research.Services;

/// <summary>
/// SignalR service for receiving real-time research session updates.
/// </summary>
public interface IResearchSignalRService : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the SignalR connection is established.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connects to the research SignalR hub.
    /// </summary>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the research SignalR hub.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Subscribes to updates for a specific research session.
    /// </summary>
    Task SubscribeToSessionAsync(Guid sessionId);

    /// <summary>
    /// Unsubscribes from updates for a specific research session.
    /// </summary>
    Task UnsubscribeFromSessionAsync(Guid sessionId);

    /// <summary>
    /// Event raised when a status update is received.
    /// </summary>
    event Action<ResearchStatusUpdate>? OnStatusUpdate;

    /// <summary>
    /// Event raised when an agent starts research.
    /// </summary>
    event Action<ResearchAgentEvent>? OnAgentStarted;

    /// <summary>
    /// Event raised when an agent completes research.
    /// </summary>
    event Action<ResearchAgentCompletedEvent>? OnAgentCompleted;

    /// <summary>
    /// Event raised when a peer review is completed.
    /// </summary>
    event Action<ResearchPeerReviewEvent>? OnPeerReviewCompleted;

    /// <summary>
    /// Event raised when research is completed.
    /// </summary>
    event Action<ResearchCompletedEvent>? OnResearchCompleted;

    /// <summary>
    /// Event raised when a research error occurs.
    /// </summary>
    event Action<ResearchErrorEvent>? OnResearchError;

    /// <summary>
    /// Event raised for general progress updates.
    /// </summary>
    event Action<ResearchProgressEvent>? OnProgress;
}

/// <summary>
/// Status update event data.
/// </summary>
public record ResearchStatusUpdate(
    Guid SessionId,
    ResearchStatusBase Status,
    ResearchPhaseBase? Phase,
    string? Message,
    DateTime Timestamp);

/// <summary>
/// Agent event data.
/// </summary>
public record ResearchAgentEvent(
    Guid SessionId,
    ResearchRoleBase Role,
    Guid AgentId,
    DateTime Timestamp);

/// <summary>
/// Agent completed event data.
/// </summary>
public record ResearchAgentCompletedEvent(
    Guid SessionId,
    ResearchRoleBase Role,
    Guid SubmissionId,
    int? TokensUsed,
    DateTime Timestamp);

/// <summary>
/// Peer review event data.
/// </summary>
public record ResearchPeerReviewEvent(
    Guid SessionId,
    Guid ReviewId,
    ResearchRoleBase ReviewerRole,
    double WeightedScore,
    DateTime Timestamp);

/// <summary>
/// Research completed event data.
/// </summary>
public record ResearchCompletedEvent(
    Guid SessionId,
    Guid ReportId,
    DateTime Timestamp);

/// <summary>
/// Research error event data.
/// </summary>
public record ResearchErrorEvent(
    Guid SessionId,
    string ErrorMessage,
    DateTime Timestamp);

/// <summary>
/// General progress event data.
/// </summary>
public record ResearchProgressEvent(
    Guid SessionId,
    string EventType,
    object? Data,
    DateTime Timestamp);

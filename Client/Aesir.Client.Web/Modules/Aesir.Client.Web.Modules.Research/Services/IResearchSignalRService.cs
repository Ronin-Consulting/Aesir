using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Research.Services;

/// <summary>
/// SignalR service for receiving real-time research session updates.
/// Simplified to 3 core events: Progress, Completed, Error.
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
    /// Event raised for unified progress updates with multi-agent tracking.
    /// </summary>
    event Action<ResearchProgressUpdate>? OnResearchProgress;

    /// <summary>
    /// Event raised when research is completed.
    /// </summary>
    event Action<ResearchCompletedEvent>? OnResearchCompleted;

    /// <summary>
    /// Event raised when a research error occurs.
    /// </summary>
    event Action<ResearchErrorEvent>? OnResearchError;
}

/// <summary>
/// Unified progress update with multi-agent tracking.
/// </summary>
public record ResearchProgressUpdate(
    Guid SessionId,
    ResearchStatusBase Status,
    ResearchPhaseBase Phase,
    string Message,
    int ProgressPercent,
    List<ActiveAgentInfo> ActiveAgents,
    ResearchRoleBase? AgentRole,
    DateTime Timestamp);

/// <summary>
/// Information about an active agent during research.
/// </summary>
public class ActiveAgentInfo
{
    /// <summary>
    /// The team member ID (unique per session).
    /// </summary>
    [JsonPropertyName("team_member_id")]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// The agent's research role.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRoleBase Role { get; set; }

    /// <summary>
    /// Display name for the role.
    /// </summary>
    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// What the agent is currently doing.
    /// </summary>
    [JsonPropertyName("activity")]
    public string Activity { get; set; } = string.Empty;

    /// <summary>
    /// Agent-specific progress within the current activity (0-100).
    /// Null if progress not applicable (e.g., waiting for LLM response).
    /// </summary>
    [JsonPropertyName("agent_progress_percent")]
    public int? AgentProgressPercent { get; set; }
}

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

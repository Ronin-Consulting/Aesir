using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Hubs;

/// <summary>
/// SignalR Hub for real-time research session updates.
/// Clients can subscribe to specific research sessions to receive progress updates.
/// </summary>
public class ResearchHub : Hub
{
    private readonly ILogger<ResearchHub> _logger;

    public ResearchHub(ILogger<ResearchHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("[RESEARCH-HUB] Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("[RESEARCH-HUB] Client disconnected: {ConnectionId}, Exception: {Exception}",
            Context.ConnectionId, exception?.Message ?? "None");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribes the client to receive updates for a specific research session.
    /// </summary>
    /// <param name="sessionId">The research session ID to subscribe to.</param>
    public async Task SubscribeToSession(Guid sessionId)
    {
        var groupName = GetSessionGroupName(sessionId);
        _logger.LogDebug("[RESEARCH-HUB] SubscribeToSession called:");
        _logger.LogDebug("[RESEARCH-HUB]   ConnectionId: {ConnectionId}", Context.ConnectionId);
        _logger.LogDebug("[RESEARCH-HUB]   SessionId: {SessionId}", sessionId);
        _logger.LogDebug("[RESEARCH-HUB]   GroupName: {GroupName}", groupName);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("[RESEARCH-HUB] Client {ConnectionId} added to group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Unsubscribes the client from updates for a specific research session.
    /// </summary>
    /// <param name="sessionId">The research session ID to unsubscribe from.</param>
    public async Task UnsubscribeFromSession(Guid sessionId)
    {
        var groupName = GetSessionGroupName(sessionId);
        _logger.LogDebug("[RESEARCH-HUB] UnsubscribeFromSession called:");
        _logger.LogDebug("[RESEARCH-HUB]   ConnectionId: {ConnectionId}", Context.ConnectionId);
        _logger.LogDebug("[RESEARCH-HUB]   SessionId: {SessionId}", sessionId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("[RESEARCH-HUB] Client {ConnectionId} removed from group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Gets the group name for a research session.
    /// </summary>
    private static string GetSessionGroupName(Guid sessionId) => $"research-{sessionId}";
}

/// <summary>
/// Provides extension methods for sending research updates via SignalR.
/// </summary>
public static class ResearchHubExtensions
{
    /// <summary>
    /// Notifies clients of a research session status change.
    /// </summary>
    public static async Task SendStatusUpdateAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        ResearchStatus status,
        ResearchPhase? phase = null,
        string? message = null)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("StatusUpdate", new
            {
                SessionId = sessionId,
                Status = status,
                Phase = phase,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent starting research.
    /// </summary>
    public static async Task SendAgentStartedAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        ResearchRole role,
        Guid agentId)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentStarted", new
            {
                SessionId = sessionId,
                Role = role,
                AgentId = agentId,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent completing research.
    /// </summary>
    public static async Task SendAgentCompletedAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        ResearchRole role,
        Guid submissionId,
        int? tokensUsed = null)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentCompleted", new
            {
                SessionId = sessionId,
                Role = role,
                SubmissionId = submissionId,
                TokensUsed = tokensUsed,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of a peer review being completed.
    /// </summary>
    public static async Task SendPeerReviewCompletedAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid reviewId,
        ResearchRole reviewerRole,
        double weightedScore)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("PeerReviewCompleted", new
            {
                SessionId = sessionId,
                ReviewId = reviewId,
                ReviewerRole = reviewerRole,
                WeightedScore = weightedScore,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of research completion with final report.
    /// </summary>
    public static async Task SendResearchCompletedAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid reportId)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("ResearchCompleted", new
            {
                SessionId = sessionId,
                ReportId = reportId,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of a research error.
    /// </summary>
    public static async Task SendResearchErrorAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        string errorMessage)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("ResearchError", new
            {
                SessionId = sessionId,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Sends a progress update with arbitrary data.
    /// </summary>
    public static async Task SendProgressAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        string eventType,
        object data)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("Progress", new
            {
                SessionId = sessionId,
                EventType = eventType,
                Data = data,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent's reasoning/thinking content chunk.
    /// </summary>
    public static async Task SendAgentThinkingAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid teamMemberId,
        ResearchRole role,
        string thinkingChunk)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentThinking", new
            {
                SessionId = sessionId,
                TeamMemberId = teamMemberId,
                Role = role,
                ThinkingChunk = thinkingChunk,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent's content chunk (non-thinking response).
    /// </summary>
    public static async Task SendAgentContentAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid teamMemberId,
        ResearchRole role,
        string contentChunk)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentContent", new
            {
                SessionId = sessionId,
                TeamMemberId = teamMemberId,
                Role = role,
                ContentChunk = contentChunk,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent's tool call starting.
    /// </summary>
    public static async Task SendAgentToolCallStartAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid teamMemberId,
        ResearchRole role,
        string toolCallId,
        string functionName,
        string? pluginName,
        object? arguments)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentToolCallStart", new
            {
                SessionId = sessionId,
                TeamMemberId = teamMemberId,
                Role = role,
                ToolCallId = toolCallId,
                FunctionName = functionName,
                PluginName = pluginName,
                Arguments = arguments,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent's tool call completing.
    /// </summary>
    public static async Task SendAgentToolCallResultAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid teamMemberId,
        ResearchRole role,
        string toolCallId,
        string functionName,
        object? result,
        bool success)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentToolCallResult", new
            {
                SessionId = sessionId,
                TeamMemberId = teamMemberId,
                Role = role,
                ToolCallId = toolCallId,
                FunctionName = functionName,
                Result = result,
                Success = success,
                Timestamp = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Notifies clients of an agent's phase change (planning -> researching).
    /// </summary>
    public static async Task SendAgentPhaseChangedAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Guid teamMemberId,
        ResearchRole role,
        string phase,
        string? message = null)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("AgentPhaseChanged", new
            {
                SessionId = sessionId,
                TeamMemberId = teamMemberId,
                Role = role,
                Phase = phase,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
    }
}

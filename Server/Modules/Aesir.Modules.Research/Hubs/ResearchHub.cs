using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;

namespace Aesir.Modules.Research.Hubs;

/// <summary>
/// SignalR Hub for real-time research session updates.
/// Clients can subscribe to specific research sessions to receive progress updates.
/// </summary>
public class ResearchHub : Hub
{
    /// <summary>
    /// Subscribes the client to receive updates for a specific research session.
    /// </summary>
    /// <param name="sessionId">The research session ID to subscribe to.</param>
    public async Task SubscribeToSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
    }

    /// <summary>
    /// Unsubscribes the client from updates for a specific research session.
    /// </summary>
    /// <param name="sessionId">The research session ID to unsubscribe from.</param>
    public async Task UnsubscribeFromSession(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionId));
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
}

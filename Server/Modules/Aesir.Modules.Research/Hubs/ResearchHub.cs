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
/// Simplified to 3 core events: ResearchProgress, ResearchCompleted, ResearchError.
/// </summary>
public static class ResearchHubExtensions
{
    /// <summary>
    /// Sends a unified research progress update with multi-agent tracking.
    /// This is the primary event for all progress updates.
    /// </summary>
    public static async Task SendResearchProgressAsync(
        this IHubContext<ResearchHub> hubContext,
        Guid sessionId,
        Contracts.ResearchProgressUpdate progress)
    {
        await hubContext.Clients
            .Group($"research-{sessionId}")
            .SendAsync("ResearchProgress", progress);
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
                session_id = sessionId,
                report_id = reportId,
                timestamp = DateTime.UtcNow
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
                session_id = sessionId,
                error_message = errorMessage,
                timestamp = DateTime.UtcNow
            });
    }
}

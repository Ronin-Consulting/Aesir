using Aesir.Modules.Logging.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Controllers;

/// <summary>
/// API controller for querying kernel execution logs.
/// Provides endpoints to retrieve logs by time range, chat session, or conversation.
/// </summary>
[ApiController]
[Route("logs")]
[Produces("application/json")]
public class LogsController : ControllerBase
{
    private readonly IKernelLogService _kernelLogService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(
        IKernelLogService kernelLogService,
        ILogger<LogsController> logger)
    {
        _kernelLogService = kernelLogService ?? throw new ArgumentNullException(nameof(kernelLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves kernel logs within a specific time range.
    /// </summary>
    /// <param name="from">Start time (UTC).</param>
    /// <param name="to">End time (UTC).</param>
    /// <returns>Collection of kernel logs.</returns>
    [HttpGet("kernel")]
    public async Task<IEnumerable<KernelLog>> GetKernelLogs([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        _logger.LogDebug("GET /logs/kernel?from={From}&to={To}", from, to);

        return await _kernelLogService.GetLogsAsync(from, to);
    }

    /// <summary>
    /// Retrieves all kernel logs for a specific chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <returns>Collection of kernel logs for the session.</returns>
    [HttpGet("kernel/chatsession/{chatSessionId}")]
    public async Task<IEnumerable<KernelLog>> GetKernelLogsBySession([FromRoute] Guid chatSessionId)
    {
        _logger.LogDebug("GET /logs/kernel/chatsession/{ChatSessionId}", chatSessionId);

        return await _kernelLogService.GetLogsByChatSessionAsync(chatSessionId);
    }

    /// <summary>
    /// Retrieves all kernel logs for a specific conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>Collection of kernel logs for the conversation.</returns>
    [HttpGet("kernel/conversation/{conversationId}")]
    public async Task<IEnumerable<KernelLog>> GetKernelLogsByConversation([FromRoute] Guid conversationId)
    {
        _logger.LogDebug("GET /logs/kernel/conversation/{ConversationId}", conversationId);

        return await _kernelLogService.GetLogsByConversationAsync(conversationId);
    }
}

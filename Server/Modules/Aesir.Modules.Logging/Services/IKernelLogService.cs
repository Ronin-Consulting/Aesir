using Aesir.Common.Models;
using Aesir.Modules.Logging.Models;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service interface for managing kernel execution logs.
/// Provides methods to log and query kernel execution activity.
/// </summary>
public interface IKernelLogService
{
    /// <summary>
    /// Logs a kernel execution event with details.
    /// </summary>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="message">The log message.</param>
    /// <param name="details">Detailed information about the execution.</param>
    Task LogAsync(KernelLogLevel logLevel, string message, KernelLogDetails details);

    /// <summary>
    /// Retrieves kernel logs within a specific time range.
    /// </summary>
    /// <param name="from">Start time (UTC).</param>
    /// <param name="to">End time (UTC).</param>
    /// <returns>Collection of kernel logs.</returns>
    Task<IEnumerable<KernelLog>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to);

    /// <summary>
    /// Retrieves all kernel logs for a specific chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <returns>Collection of kernel logs for the session.</returns>
    Task<IEnumerable<KernelLog>> GetLogsByChatSessionAsync(Guid chatSessionId);

    /// <summary>
    /// Retrieves all kernel logs for a specific conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>Collection of kernel logs for the conversation.</returns>
    Task<IEnumerable<KernelLog>> GetLogsByConversationAsync(Guid conversationId);
}

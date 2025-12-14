using Aesir.Common.Models;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service interface for managing inference execution logs.
/// Provides persistence and retrieval of inference operations including tool calls.
/// </summary>
public interface IInferenceLogService
{
    /// <summary>
    /// Logs a complete inference operation including all tool calls.
    /// </summary>
    /// <param name="log">The inference log entry to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogInferenceAsync(AesirInferenceLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an inference log by its ID with full tool call details.
    /// </summary>
    /// <param name="id">The inference log ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full inference log with tool calls, or null if not found.</returns>
    Task<AesirInferenceLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches inference logs with filtering and pagination.
    /// Returns summaries without full tool call details for performance.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated inference log summaries.</returns>
    Task<PagedInferenceLogResponse> SearchAsync(
        InferenceLogFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent inference log for a chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The most recent inference log, or null if none found.</returns>
    Task<AesirInferenceLog?> GetLatestByChatSessionAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets inference logs for a specific chat session.
    /// Returns summaries without full tool call details.
    /// </summary>
    /// <param name="chatSessionId">The chat session ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of inference log summaries for the session.</returns>
    Task<IEnumerable<AesirInferenceLogSummary>> GetByChatSessionAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default);
}

using Aesir.Common.Models;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service interface for managing document operation logs.
/// Provides persistence and retrieval of document operations including uploads, embeddings, and deletions.
/// </summary>
public interface IDocumentLogService
{
    /// <summary>
    /// Logs a document operation.
    /// </summary>
    /// <param name="log">The document log entry to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogDocumentOperationAsync(AesirDocumentLog log, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document log entry with completion data.
    /// Used to update InProgress logs when the operation completes.
    /// </summary>
    /// <param name="id">The document log ID.</param>
    /// <param name="status">The final status.</param>
    /// <param name="chunkCount">Number of chunks created (optional).</param>
    /// <param name="embeddingCount">Number of embeddings generated (optional).</param>
    /// <param name="tokenCount">Total token count (optional).</param>
    /// <param name="processingDurationMs">Processing duration in milliseconds (optional).</param>
    /// <param name="errorMessage">Error message if failed (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDocumentLogAsync(
        Guid id,
        DocumentLogStatus status,
        int? chunkCount = null,
        int? embeddingCount = null,
        int? tokenCount = null,
        long? processingDurationMs = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document log by its ID.
    /// </summary>
    /// <param name="id">The document log ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full document log, or null if not found.</returns>
    Task<AesirDocumentLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches document logs with filtering and pagination.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated document log summaries.</returns>
    Task<PagedDocumentLogResponse> SearchAsync(
        DocumentLogFilterRequest filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets document logs for a specific conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of document log summaries for the conversation.</returns>
    Task<IEnumerable<AesirDocumentLogSummary>> GetByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}

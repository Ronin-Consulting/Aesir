using Aesir.Common.Models;

namespace Aesir.Modules.Documents.Events;

/// <summary>
/// Interface for logging document operations to the observability system.
/// Provides a simplified API for starting, completing, and failing document operations.
/// </summary>
public interface IDocumentOperationLogger
{
    /// <summary>
    /// Starts logging a document operation. Returns the log ID for subsequent updates.
    /// </summary>
    /// <param name="operationType">The type of operation being performed.</param>
    /// <param name="fileName">The name of the file being processed.</param>
    /// <param name="filePath">The storage path of the file (optional).</param>
    /// <param name="fileSizeBytes">The file size in bytes (optional).</param>
    /// <param name="contentType">The MIME content type (optional).</param>
    /// <param name="conversationId">The conversation ID for conversation-specific documents (optional).</param>
    /// <param name="chatSessionId">The chat session ID (optional).</param>
    /// <param name="metadata">Additional metadata (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The log ID for subsequent updates.</returns>
    Task<Guid> StartOperationAsync(
        DocumentOperationType operationType,
        string fileName,
        string? filePath = null,
        long? fileSizeBytes = null,
        string? contentType = null,
        Guid? conversationId = null,
        Guid? chatSessionId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a document operation log with final stats.
    /// </summary>
    /// <param name="logId">The log ID returned from StartOperationAsync.</param>
    /// <param name="chunkCount">Number of text chunks created (optional).</param>
    /// <param name="embeddingCount">Number of embeddings generated (optional).</param>
    /// <param name="tokenCount">Total token count (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteOperationAsync(
        Guid logId,
        int? chunkCount = null,
        int? embeddingCount = null,
        int? tokenCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a document operation as failed.
    /// </summary>
    /// <param name="logId">The log ID returned from StartOperationAsync.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task FailOperationAsync(
        Guid logId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}

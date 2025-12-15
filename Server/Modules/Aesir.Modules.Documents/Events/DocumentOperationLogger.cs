using System.Collections.Concurrent;
using System.Diagnostics;
using Aesir.Common.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Documents.Events;

/// <summary>
/// Implementation of document operation logging to the observability system.
/// Tracks operation timing and delegates to IDocumentLogService for persistence.
/// </summary>
public class DocumentOperationLogger : IDocumentOperationLogger
{
    private readonly IDocumentLogService _documentLogService;
    private readonly ILogger<DocumentOperationLogger> _logger;
    private readonly ConcurrentDictionary<Guid, Stopwatch> _operationTimers = new();

    public DocumentOperationLogger(
        IDocumentLogService documentLogService,
        ILogger<DocumentOperationLogger> logger)
    {
        _documentLogService = documentLogService ?? throw new ArgumentNullException(nameof(documentLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Guid> StartOperationAsync(
        DocumentOperationType operationType,
        string fileName,
        string? filePath = null,
        long? fileSizeBytes = null,
        string? contentType = null,
        Guid? conversationId = null,
        Guid? chatSessionId = null,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var logId = Guid.NewGuid();
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug("Starting document operation: {LogId} - {OperationType} for {FileName}",
            logId, operationType, fileName);

        // Start timing
        var stopwatch = Stopwatch.StartNew();
        _operationTimers[logId] = stopwatch;

        var log = new AesirDocumentLog
        {
            Id = logId,
            ChatSessionId = chatSessionId,
            ConversationId = conversationId,
            OperationType = operationType,
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            ContentType = contentType,
            StartedAt = startTime,
            Status = DocumentLogStatus.InProgress,
            Metadata = metadata
        };

        try
        {
            await _documentLogService.LogDocumentOperationAsync(log, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document operation start: {LogId}", logId);
            // Don't throw - logging should not break the main operation
            _operationTimers.TryRemove(logId, out _);
        }

        return logId;
    }

    /// <inheritdoc />
    public async Task CompleteOperationAsync(
        Guid logId,
        int? chunkCount = null,
        int? embeddingCount = null,
        int? tokenCount = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Completing document operation: {LogId}", logId);

        long? durationMs = null;
        if (_operationTimers.TryRemove(logId, out var stopwatch))
        {
            stopwatch.Stop();
            durationMs = stopwatch.ElapsedMilliseconds;
        }

        try
        {
            await _documentLogService.UpdateDocumentLogAsync(
                logId,
                DocumentLogStatus.Completed,
                chunkCount,
                embeddingCount,
                tokenCount,
                durationMs,
                errorMessage: null,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Document operation completed: {LogId} - Chunks: {ChunkCount}, Embeddings: {EmbeddingCount}, Duration: {Duration}ms",
                logId, chunkCount, embeddingCount, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document operation completion: {LogId}", logId);
            // Don't throw - logging should not break the main operation
        }
    }

    /// <inheritdoc />
    public async Task FailOperationAsync(
        Guid logId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Failing document operation: {LogId} - {Error}", logId, errorMessage);

        long? durationMs = null;
        if (_operationTimers.TryRemove(logId, out var stopwatch))
        {
            stopwatch.Stop();
            durationMs = stopwatch.ElapsedMilliseconds;
        }

        try
        {
            await _documentLogService.UpdateDocumentLogAsync(
                logId,
                DocumentLogStatus.Failed,
                chunkCount: null,
                embeddingCount: null,
                tokenCount: null,
                durationMs,
                errorMessage,
                cancellationToken).ConfigureAwait(false);

            _logger.LogWarning("Document operation failed: {LogId} - {Error}", logId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document operation failure: {LogId}", logId);
            // Don't throw - logging should not break the main operation
        }
    }
}

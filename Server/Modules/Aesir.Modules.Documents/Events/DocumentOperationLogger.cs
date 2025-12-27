using System.Collections.Concurrent;
using System.Diagnostics;
using Aesir.Common.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Polly.Retry;

namespace Aesir.Modules.Documents.Events;

/// <summary>
/// Implementation of document operation logging to the observability system.
/// Tracks operation timing and delegates to IDocumentLogService for persistence.
/// Uses Polly retry for transient database failures.
/// </summary>
public class DocumentOperationLogger : IDocumentOperationLogger
{
    private readonly IDocumentLogService _documentLogService;
    private readonly ILogger<DocumentOperationLogger> _logger;
    private readonly ConcurrentDictionary<Guid, Stopwatch> _operationTimers = new();

    /// <summary>
    /// Retry pipeline for transient database errors during logging operations.
    /// Uses exponential backoff with jitter: 100ms, 200ms, 400ms base delays.
    /// </summary>
    private readonly ResiliencePipeline _retryPipeline;

    public DocumentOperationLogger(
        IDocumentLogService documentLogService,
        ILogger<DocumentOperationLogger> logger)
    {
        _documentLogService = documentLogService ?? throw new ArgumentNullException(nameof(documentLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure retry pipeline for transient database errors
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(100),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<NpgsqlException>()
                    .Handle<OperationCanceledException>()
                    .Handle<TimeoutException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retrying logging operation after {Delay}ms (attempt {Attempt}/{Max}). Error: {Error}",
                        args.RetryDelay.TotalMilliseconds,
                        args.AttemptNumber + 1,
                        3,
                        args.Outcome.Exception?.Message ?? "Unknown");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
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
            await _retryPipeline.ExecuteAsync(async ct =>
            {
                await _documentLogService.LogDocumentOperationAsync(log, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);
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
            // Use a fresh cancellation token with timeout for completion logging
            // This ensures we log completion even if the original token is cancelled
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await _retryPipeline.ExecuteAsync(async ct =>
            {
                await _documentLogService.UpdateDocumentLogAsync(
                    logId,
                    DocumentLogStatus.Completed,
                    chunkCount,
                    embeddingCount,
                    tokenCount,
                    durationMs,
                    errorMessage: null,
                    ct).ConfigureAwait(false);
            }, timeoutCts.Token).ConfigureAwait(false);

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
            // IMPORTANT: Use CancellationToken.None for failure logging
            // We ALWAYS want to log failures, even if the original operation was cancelled.
            // Use a timeout to prevent hanging indefinitely.
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await _retryPipeline.ExecuteAsync(async ct =>
            {
                await _documentLogService.UpdateDocumentLogAsync(
                    logId,
                    DocumentLogStatus.Failed,
                    chunkCount: null,
                    embeddingCount: null,
                    tokenCount: null,
                    durationMs,
                    errorMessage,
                    ct).ConfigureAwait(false);
            }, timeoutCts.Token).ConfigureAwait(false);

            _logger.LogWarning("Document operation failed: {LogId} - {Error}", logId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log document operation failure: {LogId}", logId);
            // Don't throw - logging should not break the main operation
        }
    }
}

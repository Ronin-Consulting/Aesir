using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service implementation for managing document operation logs.
/// Uses Dapper for direct database access with JSONB column support.
/// </summary>
public class DocumentLogService : IDocumentLogService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<DocumentLogService> _logger;

    static DocumentLogService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, object>>());
    }

    public DocumentLogService(IDbContext dbContext, ILogger<DocumentLogService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogDocumentOperationAsync(AesirDocumentLog log, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Logging document operation: {Id} - {OperationType} for file {FileName}",
            log.Id, log.OperationType, log.FileName);

        const string sql = @"
            INSERT INTO aesir.aesir_log_document (
                id, chat_session_id, conversation_id,
                operation_type, file_name, file_path, file_size_bytes, content_type,
                chunk_count, embedding_count, token_count, processing_duration_ms,
                started_at, completed_at, status, error_message, metadata
            )
            VALUES (
                @Id, @ChatSessionId, @ConversationId,
                @OperationType, @FileName, @FilePath, @FileSizeBytes, @ContentType,
                @ChunkCount, @EmbeddingCount, @TokenCount, @ProcessingDurationMs,
                @StartedAt, @CompletedAt, @Status, @ErrorMessage, @Metadata::jsonb
            )
        ";

        await _dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    log.Id,
                    log.ChatSessionId,
                    log.ConversationId,
                    OperationType = log.OperationType.ToString(),
                    log.FileName,
                    log.FilePath,
                    log.FileSizeBytes,
                    log.ContentType,
                    log.ChunkCount,
                    log.EmbeddingCount,
                    log.TokenCount,
                    log.ProcessingDurationMs,
                    log.StartedAt,
                    log.CompletedAt,
                    Status = log.Status.ToString(),
                    log.ErrorMessage,
                    log.Metadata
                }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }, withTransaction: true).ConfigureAwait(false);

        _logger.LogInformation("Document operation logged: {Id} - {OperationType} for {FileName}, Status: {Status}",
            log.Id, log.OperationType, log.FileName, log.Status);
    }

    /// <inheritdoc />
    public async Task UpdateDocumentLogAsync(
        Guid id,
        DocumentLogStatus status,
        int? chunkCount = null,
        int? embeddingCount = null,
        int? tokenCount = null,
        long? processingDurationMs = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating document log: {Id} to status {Status}", id, status);

        const string sql = @"
            UPDATE aesir.aesir_log_document
            SET
                status = @Status,
                completed_at = @CompletedAt,
                chunk_count = COALESCE(@ChunkCount, chunk_count),
                embedding_count = COALESCE(@EmbeddingCount, embedding_count),
                token_count = COALESCE(@TokenCount, token_count),
                processing_duration_ms = COALESCE(@ProcessingDurationMs, processing_duration_ms),
                error_message = COALESCE(@ErrorMessage, error_message)
            WHERE id = @Id
        ";

        await _dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(
                new CommandDefinition(sql, new
                {
                    Id = id,
                    Status = status.ToString(),
                    CompletedAt = DateTimeOffset.UtcNow,
                    ChunkCount = chunkCount,
                    EmbeddingCount = embeddingCount,
                    TokenCount = tokenCount,
                    ProcessingDurationMs = processingDurationMs,
                    ErrorMessage = errorMessage
                }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }, withTransaction: true).ConfigureAwait(false);

        _logger.LogInformation("Document log updated: {Id} - Status: {Status}", id, status);
    }

    /// <inheritdoc />
    public async Task<AesirDocumentLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting document log by ID: {Id}", id);

        const string sql = @"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                operation_type as OperationType,
                file_name as FileName,
                file_path as FilePath,
                file_size_bytes as FileSizeBytes,
                content_type as ContentType,
                chunk_count as ChunkCount,
                embedding_count as EmbeddingCount,
                token_count as TokenCount,
                processing_duration_ms as ProcessingDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage,
                metadata::text as Metadata
            FROM aesir.aesir_log_document
            WHERE id = @Id
        ";

        var row = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleOrDefaultAsync<DocumentLogDbRow>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
                .ConfigureAwait(false))
            .ConfigureAwait(false);

        if (row == null)
        {
            _logger.LogDebug("Document log not found: {Id}", id);
            return null;
        }

        return MapToDocumentLog(row);
    }

    /// <inheritdoc />
    public async Task<PagedDocumentLogResponse> SearchAsync(
        DocumentLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching document logs: Page={Page}, PageSize={PageSize}",
            filter.Page, filter.PageSize);

        var (whereClause, parameters) = BuildWhereClause(filter);
        var sortOrder = filter.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        var offset = (filter.Page - 1) * filter.PageSize;

        // Count query
        var countSql = $@"
            SELECT COUNT(*)
            FROM aesir.aesir_log_document
            {whereClause}
        ";

        // Data query with pagination
        var dataSql = $@"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                operation_type as OperationType,
                file_name as FileName,
                file_size_bytes as FileSizeBytes,
                content_type as ContentType,
                chunk_count as ChunkCount,
                embedding_count as EmbeddingCount,
                token_count as TokenCount,
                processing_duration_ms as ProcessingDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_document
            {whereClause}
            ORDER BY started_at {sortOrder}
            LIMIT @PageSize OFFSET @Offset
        ";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var result = await _dbContext.UnitOfWorkAsync(async connection =>
        {
            var totalCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var rows = await connection.QueryAsync<DocumentLogDbRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var summaries = rows.Select(MapToSummary).ToList();

            return new PagedDocumentLogResponse
            {
                Items = summaries,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }).ConfigureAwait(false);

        _logger.LogDebug("Search returned {Count} of {Total} document logs",
            result.Items.Count(), result.TotalCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirDocumentLogSummary>> GetByConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting document logs for conversation: {ConversationId}", conversationId);

        const string sql = @"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                operation_type as OperationType,
                file_name as FileName,
                file_size_bytes as FileSizeBytes,
                content_type as ContentType,
                chunk_count as ChunkCount,
                embedding_count as EmbeddingCount,
                token_count as TokenCount,
                processing_duration_ms as ProcessingDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_document
            WHERE conversation_id = @ConversationId
            ORDER BY started_at DESC
        ";

        var rows = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<DocumentLogDbRow>(
                new CommandDefinition(sql, new { ConversationId = conversationId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false))
            .ConfigureAwait(false);

        return rows.Select(MapToSummary);
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(DocumentLogFilterRequest filter)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        // Time range
        if (filter.From.HasValue)
        {
            conditions.Add("started_at >= @From");
            parameters.Add("From", filter.From.Value.UtcDateTime);
        }

        if (filter.To.HasValue)
        {
            conditions.Add("started_at <= @To");
            parameters.Add("To", filter.To.Value.UtcDateTime);
        }

        // Chat session ID
        if (filter.ChatSessionId.HasValue)
        {
            conditions.Add("chat_session_id = @ChatSessionId");
            parameters.Add("ChatSessionId", filter.ChatSessionId.Value);
        }

        // Conversation ID
        if (filter.ConversationId.HasValue)
        {
            conditions.Add("conversation_id = @ConversationId");
            parameters.Add("ConversationId", filter.ConversationId.Value);
        }

        // Statuses (multiple values with OR using ANY)
        if (filter.Statuses is { Count: > 0 })
        {
            conditions.Add("status = ANY(@Statuses)");
            parameters.Add("Statuses", filter.Statuses.Select(s => s.ToString()).ToArray());
        }

        // Operation types (multiple values with OR using ANY)
        if (filter.OperationTypes is { Count: > 0 })
        {
            conditions.Add("operation_type = ANY(@OperationTypes)");
            parameters.Add("OperationTypes", filter.OperationTypes.Select(o => o.ToString()).ToArray());
        }

        // Search text (partial match on file name, case-insensitive)
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            conditions.Add("file_name ILIKE @SearchText");
            parameters.Add("SearchText", $"%{filter.SearchText}%");
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return (whereClause, parameters);
    }

    private static AesirDocumentLog MapToDocumentLog(DocumentLogDbRow row)
    {
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrEmpty(row.Metadata))
        {
            try
            {
                metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(row.Metadata);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new AesirDocumentLog
        {
            Id = row.Id,
            ChatSessionId = row.ChatSessionId,
            ConversationId = row.ConversationId,
            OperationType = Enum.TryParse<DocumentOperationType>(row.OperationType, out var opType)
                ? opType : DocumentOperationType.FileUploaded,
            FileName = row.FileName,
            FilePath = row.FilePath,
            FileSizeBytes = row.FileSizeBytes,
            ContentType = row.ContentType,
            ChunkCount = row.ChunkCount,
            EmbeddingCount = row.EmbeddingCount,
            TokenCount = row.TokenCount,
            ProcessingDurationMs = row.ProcessingDurationMs,
            StartedAt = row.StartedAt,
            CompletedAt = row.CompletedAt,
            Status = Enum.TryParse<DocumentLogStatus>(row.Status, out var status)
                ? status : DocumentLogStatus.Completed,
            ErrorMessage = row.ErrorMessage,
            Metadata = metadata
        };
    }

    private static AesirDocumentLogSummary MapToSummary(DocumentLogDbRow row)
    {
        return new AesirDocumentLogSummary
        {
            Id = row.Id,
            ChatSessionId = row.ChatSessionId,
            ConversationId = row.ConversationId,
            OperationType = Enum.TryParse<DocumentOperationType>(row.OperationType, out var opType)
                ? opType : DocumentOperationType.FileUploaded,
            FileName = row.FileName,
            FileSizeBytes = row.FileSizeBytes,
            ContentType = row.ContentType,
            ChunkCount = row.ChunkCount,
            EmbeddingCount = row.EmbeddingCount,
            TokenCount = row.TokenCount,
            ProcessingDurationMs = row.ProcessingDurationMs,
            StartedAt = row.StartedAt,
            CompletedAt = row.CompletedAt,
            Status = Enum.TryParse<DocumentLogStatus>(row.Status, out var status)
                ? status : DocumentLogStatus.Completed,
            ErrorMessage = row.ErrorMessage
        };
    }

    /// <summary>
    /// Internal DTO for database row mapping.
    /// </summary>
    private class DocumentLogDbRow
    {
        public Guid Id { get; set; }
        public Guid? ChatSessionId { get; set; }
        public Guid? ConversationId { get; set; }
        public string OperationType { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string? FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
        public int? ChunkCount { get; set; }
        public int? EmbeddingCount { get; set; }
        public int? TokenCount { get; set; }
        public long? ProcessingDurationMs { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public string? Metadata { get; set; }
    }
}

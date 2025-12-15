using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service implementation for unified timeline operations.
/// Combines inference and document logs into a single timeline view.
/// </summary>
public class UnifiedTimelineService : IUnifiedTimelineService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<UnifiedTimelineService> _logger;

    static UnifiedTimelineService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<AesirToolCallInfo>>());
    }

    public UnifiedTimelineService(IDbContext dbContext, ILogger<UnifiedTimelineService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<PagedUnifiedTimelineResponse> GetUnifiedTimelineAsync(
        UnifiedTimelineFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting unified timeline: Page={Page}, PageSize={PageSize}",
            filter.Page, filter.PageSize);

        var includeInference = filter.LogTypes == null || filter.LogTypes.Count == 0 ||
                               filter.LogTypes.Contains(TimelineItemType.Inference);
        var includeDocument = filter.LogTypes == null || filter.LogTypes.Count == 0 ||
                              filter.LogTypes.Contains(TimelineItemType.Document);

        var sortOrder = filter.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        var offset = (filter.Page - 1) * filter.PageSize;

        var (inferenceWhere, documentWhere, parameters) = BuildWhereClauses(filter);

        // Build the UNION query for combined results
        var unionParts = new List<string>();

        if (includeInference)
        {
            unionParts.Add($@"
                SELECT
                    'Inference' as ItemType,
                    id as Id,
                    chat_session_id as ChatSessionId,
                    conversation_id as ConversationId,
                    started_at as StartedAt,
                    completed_at as CompletedAt,
                    status as Status,
                    error_message as ErrorMessage,
                    user_query_truncated as UserQueryTruncated,
                    assistant_response as AssistantResponse,
                    tool_calls::text as ToolCallsJson,
                    tool_call_count as ToolCallCount,
                    total_duration_ms as TotalDurationMs,
                    NULL as OperationType,
                    NULL as FileName,
                    NULL::bigint as FileSizeBytes,
                    NULL as ContentType,
                    NULL::int as ChunkCount,
                    NULL::int as EmbeddingCount,
                    NULL::int as TokenCount,
                    NULL::bigint as ProcessingDurationMs
                FROM aesir.aesir_log_inference
                {inferenceWhere}
            ");
        }

        if (includeDocument)
        {
            unionParts.Add($@"
                SELECT
                    'Document' as ItemType,
                    id as Id,
                    chat_session_id as ChatSessionId,
                    conversation_id as ConversationId,
                    started_at as StartedAt,
                    completed_at as CompletedAt,
                    status as Status,
                    error_message as ErrorMessage,
                    NULL as UserQueryTruncated,
                    NULL as AssistantResponse,
                    NULL as ToolCallsJson,
                    NULL::int as ToolCallCount,
                    NULL::bigint as TotalDurationMs,
                    operation_type as OperationType,
                    file_name as FileName,
                    file_size_bytes as FileSizeBytes,
                    content_type as ContentType,
                    chunk_count as ChunkCount,
                    embedding_count as EmbeddingCount,
                    token_count as TokenCount,
                    processing_duration_ms as ProcessingDurationMs
                FROM aesir.aesir_log_document
                {documentWhere}
            ");
        }

        if (unionParts.Count == 0)
        {
            return new PagedUnifiedTimelineResponse
            {
                Items = Enumerable.Empty<UnifiedTimelineItem>(),
                TotalCount = 0,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        var unionQuery = string.Join(" UNION ALL ", unionParts);

        // Count query
        var countSql = $@"
            SELECT COUNT(*) FROM (
                {unionQuery}
            ) as unified
        ";

        // Data query with pagination
        var dataSql = $@"
            SELECT * FROM (
                {unionQuery}
            ) as unified
            ORDER BY StartedAt {sortOrder}
            LIMIT @PageSize OFFSET @Offset
        ";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var result = await _dbContext.UnitOfWorkAsync(async connection =>
        {
            var totalCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var rows = await connection.QueryAsync<UnifiedTimelineDbRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var items = rows.Select(MapToUnifiedTimelineItem).ToList();

            return new PagedUnifiedTimelineResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }).ConfigureAwait(false);

        _logger.LogDebug("Unified timeline returned {Count} of {Total} items",
            result.Items.Count(), result.TotalCount);

        return result;
    }

    private static (string InferenceWhere, string DocumentWhere, DynamicParameters Parameters)
        BuildWhereClauses(UnifiedTimelineFilterRequest filter)
    {
        var inferenceConditions = new List<string>();
        var documentConditions = new List<string>();
        var parameters = new DynamicParameters();

        // Time range - applies to both
        if (filter.From.HasValue)
        {
            inferenceConditions.Add("started_at >= @From");
            documentConditions.Add("started_at >= @From");
            parameters.Add("From", filter.From.Value.UtcDateTime);
        }

        if (filter.To.HasValue)
        {
            inferenceConditions.Add("started_at <= @To");
            documentConditions.Add("started_at <= @To");
            parameters.Add("To", filter.To.Value.UtcDateTime);
        }

        // Chat session ID - applies to both
        if (filter.ChatSessionId.HasValue)
        {
            inferenceConditions.Add("chat_session_id = @ChatSessionId");
            documentConditions.Add("chat_session_id = @ChatSessionId");
            parameters.Add("ChatSessionId", filter.ChatSessionId.Value);
        }

        // Conversation ID - applies to both
        if (filter.ConversationId.HasValue)
        {
            inferenceConditions.Add("conversation_id = @ConversationId");
            documentConditions.Add("conversation_id = @ConversationId");
            parameters.Add("ConversationId", filter.ConversationId.Value);
        }

        // Inference-specific: statuses
        if (filter.InferenceStatuses is { Count: > 0 })
        {
            inferenceConditions.Add("status = ANY(@InferenceStatuses)");
            parameters.Add("InferenceStatuses", filter.InferenceStatuses.Select(s => s.ToString()).ToArray());
        }

        // Inference-specific: has tool calls
        if (filter.HasToolCalls.HasValue)
        {
            if (filter.HasToolCalls.Value)
            {
                inferenceConditions.Add("tool_call_count > 0");
            }
            else
            {
                inferenceConditions.Add("tool_call_count = 0");
            }
        }

        // Document-specific: statuses
        if (filter.DocumentStatuses is { Count: > 0 })
        {
            documentConditions.Add("status = ANY(@DocumentStatuses)");
            parameters.Add("DocumentStatuses", filter.DocumentStatuses.Select(s => s.ToString()).ToArray());
        }

        // Document-specific: operation types
        if (filter.OperationTypes is { Count: > 0 })
        {
            documentConditions.Add("operation_type = ANY(@OperationTypes)");
            parameters.Add("OperationTypes", filter.OperationTypes.Select(o => o.ToString()).ToArray());
        }

        // Search text - applies differently to each
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            inferenceConditions.Add("(user_query_truncated ILIKE @SearchText OR user_query ILIKE @SearchText)");
            documentConditions.Add("file_name ILIKE @SearchText");
            parameters.Add("SearchText", $"%{filter.SearchText}%");
        }

        var inferenceWhere = inferenceConditions.Count > 0
            ? "WHERE " + string.Join(" AND ", inferenceConditions)
            : string.Empty;

        var documentWhere = documentConditions.Count > 0
            ? "WHERE " + string.Join(" AND ", documentConditions)
            : string.Empty;

        return (inferenceWhere, documentWhere, parameters);
    }

    private static UnifiedTimelineItem MapToUnifiedTimelineItem(UnifiedTimelineDbRow row)
    {
        var item = new UnifiedTimelineItem
        {
            StartedAt = row.StartedAt
        };

        if (row.ItemType == "Inference")
        {
            item.Type = TimelineItemType.Inference;

            List<ToolCallSummary>? toolCallSummaries = null;
            if (!string.IsNullOrEmpty(row.ToolCallsJson))
            {
                try
                {
                    var toolCalls = Newtonsoft.Json.JsonConvert.DeserializeObject<List<AesirToolCallInfo>>(row.ToolCallsJson);
                    toolCallSummaries = toolCalls?.Select(tc => new ToolCallSummary
                    {
                        FunctionName = tc.FunctionName,
                        PluginName = tc.PluginName,
                        ToolType = tc.ToolType,
                        Status = tc.Status,
                        DurationMs = tc.DurationMs
                    }).ToList();
                }
                catch
                {
                    // Ignore deserialization errors
                }
            }

            item.InferenceLog = new AesirInferenceLogSummary
            {
                Id = row.Id,
                ChatSessionId = row.ChatSessionId,
                ConversationId = row.ConversationId,
                UserQueryTruncated = row.UserQueryTruncated,
                AssistantResponse = row.AssistantResponse,
                ToolCallCount = row.ToolCallCount ?? 0,
                ToolCallSummaries = toolCallSummaries,
                TotalDurationMs = row.TotalDurationMs,
                StartedAt = row.StartedAt,
                CompletedAt = row.CompletedAt,
                Status = Enum.TryParse<InferenceStatus>(row.Status, out var infStatus)
                    ? infStatus : InferenceStatus.Completed,
                ErrorMessage = row.ErrorMessage
            };
        }
        else if (row.ItemType == "Document")
        {
            item.Type = TimelineItemType.Document;
            item.DocumentLog = new AesirDocumentLogSummary
            {
                Id = row.Id,
                ChatSessionId = row.ChatSessionId,
                ConversationId = row.ConversationId,
                OperationType = Enum.TryParse<DocumentOperationType>(row.OperationType, out var opType)
                    ? opType : DocumentOperationType.FileUploaded,
                FileName = row.FileName ?? string.Empty,
                FileSizeBytes = row.FileSizeBytes,
                ContentType = row.ContentType,
                ChunkCount = row.ChunkCount,
                EmbeddingCount = row.EmbeddingCount,
                TokenCount = row.TokenCount,
                ProcessingDurationMs = row.ProcessingDurationMs,
                StartedAt = row.StartedAt,
                CompletedAt = row.CompletedAt,
                Status = Enum.TryParse<DocumentLogStatus>(row.Status, out var docStatus)
                    ? docStatus : DocumentLogStatus.Completed,
                ErrorMessage = row.ErrorMessage
            };
        }

        return item;
    }

    /// <summary>
    /// Internal DTO for unified database row mapping.
    /// </summary>
    private class UnifiedTimelineDbRow
    {
        public string ItemType { get; set; } = null!;
        public Guid Id { get; set; }
        public Guid? ChatSessionId { get; set; }
        public Guid? ConversationId { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }

        // Inference-specific
        public string? UserQueryTruncated { get; set; }
        public string? AssistantResponse { get; set; }
        public string? ToolCallsJson { get; set; }
        public int? ToolCallCount { get; set; }
        public long? TotalDurationMs { get; set; }

        // Document-specific
        public string? OperationType { get; set; }
        public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentType { get; set; }
        public int? ChunkCount { get; set; }
        public int? EmbeddingCount { get; set; }
        public int? TokenCount { get; set; }
        public long? ProcessingDurationMs { get; set; }
    }
}

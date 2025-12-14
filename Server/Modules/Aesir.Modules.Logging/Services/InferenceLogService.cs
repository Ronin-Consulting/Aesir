using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service implementation for managing inference execution logs.
/// Uses Dapper for direct database access with JSONB column support.
/// </summary>
public class InferenceLogService : IInferenceLogService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<InferenceLogService> _logger;

    static InferenceLogService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<AesirToolCallInfo>>());
    }

    public InferenceLogService(IDbContext dbContext, ILogger<InferenceLogService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogInferenceAsync(AesirInferenceLog log, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Logging inference: {Id} with {ToolCallCount} tool calls",
            log.Id, log.ToolCallCount);

        const string sql = @"
            INSERT INTO aesir.aesir_log_inference (
                id, chat_session_id, conversation_id,
                user_query, user_query_truncated, assistant_response,
                tool_calls, tool_call_count, total_duration_ms,
                started_at, completed_at, status, error_message
            )
            VALUES (
                @Id, @ChatSessionId, @ConversationId,
                @UserQuery, @UserQueryTruncated, @AssistantResponse,
                @ToolCalls::jsonb, @ToolCallCount, @TotalDurationMs,
                @StartedAt, @CompletedAt, @Status, @ErrorMessage
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
                    log.UserQuery,
                    log.UserQueryTruncated,
                    log.AssistantResponse,
                    log.ToolCalls,
                    log.ToolCallCount,
                    log.TotalDurationMs,
                    log.StartedAt,
                    log.CompletedAt,
                    Status = log.Status.ToString(),
                    log.ErrorMessage
                }, cancellationToken: cancellationToken))
                .ConfigureAwait(false);
        }, withTransaction: true).ConfigureAwait(false);

        _logger.LogInformation("Inference logged: {Id} - Status: {Status}, ToolCalls: {ToolCallCount}",
            log.Id, log.Status, log.ToolCallCount);
    }

    /// <inheritdoc />
    public async Task<AesirInferenceLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting inference log by ID: {Id}", id);

        const string sql = @"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                user_query as UserQuery,
                user_query_truncated as UserQueryTruncated,
                assistant_response as AssistantResponse,
                tool_calls::jsonb as ToolCalls,
                tool_call_count as ToolCallCount,
                total_duration_ms as TotalDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_inference
            WHERE id = @Id
        ";

        var log = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleOrDefaultAsync<InferenceLogDbRow>(
                new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken))
                .ConfigureAwait(false))
            .ConfigureAwait(false);

        if (log == null)
        {
            _logger.LogDebug("Inference log not found: {Id}", id);
            return null;
        }

        return MapToInferenceLog(log);
    }

    /// <inheritdoc />
    public async Task<PagedInferenceLogResponse> SearchAsync(
        InferenceLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching inference logs: Page={Page}, PageSize={PageSize}",
            filter.Page, filter.PageSize);

        var (whereClause, parameters) = BuildWhereClause(filter);
        var sortOrder = filter.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        var offset = (filter.Page - 1) * filter.PageSize;

        // Count query
        var countSql = $@"
            SELECT COUNT(*)
            FROM aesir.aesir_log_inference
            {whereClause}
        ";

        // Data query with pagination - returns summary data
        var dataSql = $@"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                user_query_truncated as UserQueryTruncated,
                assistant_response as AssistantResponse,
                tool_calls::jsonb as ToolCalls,
                tool_call_count as ToolCallCount,
                total_duration_ms as TotalDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_inference
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

            var rows = await connection.QueryAsync<InferenceLogDbRow>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var summaries = rows.Select(MapToSummary).ToList();

            return new PagedInferenceLogResponse
            {
                Items = summaries,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }).ConfigureAwait(false);

        _logger.LogDebug("Search returned {Count} of {Total} inference logs",
            result.Items.Count(), result.TotalCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<AesirInferenceLog?> GetLatestByChatSessionAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting latest inference log for chat session: {ChatSessionId}", chatSessionId);

        const string sql = @"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                user_query as UserQuery,
                user_query_truncated as UserQueryTruncated,
                assistant_response as AssistantResponse,
                tool_calls::jsonb as ToolCalls,
                tool_call_count as ToolCallCount,
                total_duration_ms as TotalDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_inference
            WHERE chat_session_id = @ChatSessionId
            ORDER BY started_at DESC
            LIMIT 1
        ";

        var log = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleOrDefaultAsync<InferenceLogDbRow>(
                new CommandDefinition(sql, new { ChatSessionId = chatSessionId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false))
            .ConfigureAwait(false);

        return log != null ? MapToInferenceLog(log) : null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AesirInferenceLogSummary>> GetByChatSessionAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting inference logs for chat session: {ChatSessionId}", chatSessionId);

        const string sql = @"
            SELECT
                id as Id,
                chat_session_id as ChatSessionId,
                conversation_id as ConversationId,
                user_query_truncated as UserQueryTruncated,
                assistant_response as AssistantResponse,
                tool_calls::jsonb as ToolCalls,
                tool_call_count as ToolCallCount,
                total_duration_ms as TotalDurationMs,
                started_at as StartedAt,
                completed_at as CompletedAt,
                status as Status,
                error_message as ErrorMessage
            FROM aesir.aesir_log_inference
            WHERE chat_session_id = @ChatSessionId
            ORDER BY started_at DESC
        ";

        var rows = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<InferenceLogDbRow>(
                new CommandDefinition(sql, new { ChatSessionId = chatSessionId }, cancellationToken: cancellationToken))
                .ConfigureAwait(false))
            .ConfigureAwait(false);

        return rows.Select(MapToSummary);
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(InferenceLogFilterRequest filter)
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

        // Minimum tool call count
        if (filter.MinToolCallCount.HasValue)
        {
            conditions.Add("tool_call_count >= @MinToolCallCount");
            parameters.Add("MinToolCallCount", filter.MinToolCallCount.Value);
        }

        // Has tool calls filter
        if (filter.HasToolCalls.HasValue)
        {
            if (filter.HasToolCalls.Value)
            {
                conditions.Add("tool_call_count > 0");
            }
            else
            {
                conditions.Add("tool_call_count = 0");
            }
        }

        // Search text (partial match on user query, case-insensitive)
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            conditions.Add("(user_query_truncated ILIKE @SearchText OR user_query ILIKE @SearchText)");
            parameters.Add("SearchText", $"%{filter.SearchText}%");
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return (whereClause, parameters);
    }

    private static AesirInferenceLog MapToInferenceLog(InferenceLogDbRow row)
    {
        return new AesirInferenceLog
        {
            Id = row.Id,
            ChatSessionId = row.ChatSessionId,
            ConversationId = row.ConversationId,
            UserQuery = row.UserQuery ?? row.UserQueryTruncated ?? string.Empty,
            UserQueryTruncated = row.UserQueryTruncated,
            AssistantResponse = row.AssistantResponse,
            ToolCalls = row.ToolCalls ?? new List<AesirToolCallInfo>(),
            ToolCallCount = row.ToolCallCount,
            TotalDurationMs = row.TotalDurationMs,
            StartedAt = row.StartedAt,
            CompletedAt = row.CompletedAt,
            Status = Enum.TryParse<InferenceStatus>(row.Status, out var status) ? status : InferenceStatus.Completed,
            ErrorMessage = row.ErrorMessage
        };
    }

    private static AesirInferenceLogSummary MapToSummary(InferenceLogDbRow row)
    {
        var toolCallSummaries = row.ToolCalls?.Select(tc => new ToolCallSummary
        {
            FunctionName = tc.FunctionName,
            PluginName = tc.PluginName,
            ToolType = tc.ToolType,
            Status = tc.Status,
            DurationMs = tc.DurationMs
        }).ToList();

        return new AesirInferenceLogSummary
        {
            Id = row.Id,
            ChatSessionId = row.ChatSessionId,
            ConversationId = row.ConversationId,
            UserQueryTruncated = row.UserQueryTruncated,
            AssistantResponse = row.AssistantResponse,
            ToolCallCount = row.ToolCallCount,
            ToolCallSummaries = toolCallSummaries,
            TotalDurationMs = row.TotalDurationMs,
            StartedAt = row.StartedAt,
            CompletedAt = row.CompletedAt,
            Status = Enum.TryParse<InferenceStatus>(row.Status, out var status) ? status : InferenceStatus.Completed,
            ErrorMessage = row.ErrorMessage
        };
    }

    /// <summary>
    /// Internal DTO for database row mapping.
    /// </summary>
    private class InferenceLogDbRow
    {
        public Guid Id { get; set; }
        public Guid? ChatSessionId { get; set; }
        public Guid? ConversationId { get; set; }
        public string? UserQuery { get; set; }
        public string? UserQueryTruncated { get; set; }
        public string? AssistantResponse { get; set; }
        public List<AesirToolCallInfo>? ToolCalls { get; set; }
        public int ToolCallCount { get; set; }
        public long? TotalDurationMs { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; }
    }
}

using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Aesir.Modules.Logging.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service implementation for managing kernel execution logs.
/// Uses Dapper for direct database access with JSONB column support.
/// </summary>
public class KernelLogService : IKernelLogService
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<KernelLogService> _logger;

    static KernelLogService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<KernelLogDetails>());
    }

    public KernelLogService(IDbContext dbContext, ILogger<KernelLogService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogAsync(KernelLogLevel logLevel, string message, KernelLogDetails details)
    {
        _logger.LogDebug("Logging kernel execution: {Level} - {Message}", logLevel, message);

        const string sql = @"
            INSERT INTO aesir.aesir_log_kernel (id, level, message, created_at, details)
            VALUES (@Id, @Level, @Message, @Created, @Details::jsonb)
        ";

        await _dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = Guid.NewGuid(),
                Level = logLevel,
                Message = message,
                Created = DateTime.UtcNow,
                Details = details
            }).ConfigureAwait(false);
        }, withTransaction: true);

        _logger.LogInformation("Kernel execution logged: {Level} - {Message}", logLevel, message);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KernelLog>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        _logger.LogDebug("Getting kernel logs from {From} to {To}", from, to);

        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details,
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE created_at between @From and @To
            ORDER BY created_at DESC
        ";

        var logs = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<KernelLog>(sql, new { From = from.UtcDateTime, To = to.UtcDateTime })
                .ConfigureAwait(false));

        _logger.LogDebug("Retrieved {Count} kernel logs", logs.Count());

        return logs;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KernelLog>> GetLogsByChatSessionAsync(Guid chatSessionId)
    {
        _logger.LogDebug("Getting kernel logs for chat session: {ChatSessionId}", chatSessionId);

        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details,
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE details->>'ChatSessionId' = @ChatSessionId
            ORDER BY created_at DESC
        ";

        var logs = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<KernelLog>(sql, new { ChatSessionId = chatSessionId.ToString() })
                .ConfigureAwait(false));

        _logger.LogDebug("Retrieved {Count} kernel logs for chat session {ChatSessionId}", logs.Count(), chatSessionId);

        return logs;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<KernelLog>> GetLogsByConversationAsync(Guid conversationId)
    {
        _logger.LogDebug("Getting kernel logs for conversation: {ConversationId}", conversationId);

        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details,
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE details->>'ConversationId' = @ConversationId
            ORDER BY created_at DESC
        ";

        var logs = await _dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<KernelLog>(sql, new { ConversationId = conversationId.ToString() })
                .ConfigureAwait(false));

        _logger.LogDebug("Retrieved {Count} kernel logs for conversation {ConversationId}", logs.Count(), conversationId);

        return logs;
    }

    /// <inheritdoc />
    public async Task<PagedKernelLogResponse> SearchLogsAsync(
        KernelLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching kernel logs with filter: Page={Page}, PageSize={PageSize}",
            filter.Page, filter.PageSize);

        var (whereClause, parameters) = BuildWhereClause(filter);
        var sortOrder = filter.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        var offset = (filter.Page - 1) * filter.PageSize;

        // Count query
        var countSql = $@"
            SELECT COUNT(*)
            FROM aesir.aesir_log_kernel
            {whereClause}
        ";

        // Data query with pagination
        var dataSql = $@"
            SELECT
                id as Id,
                level as Level,
                created_at as CreatedAt,
                details::jsonb as Details,
                message as Message
            FROM aesir.aesir_log_kernel
            {whereClause}
            ORDER BY created_at {sortOrder}
            LIMIT @PageSize OFFSET @Offset
        ";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", offset);

        var result = await _dbContext.UnitOfWorkAsync(async connection =>
        {
            var totalCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            var items = await connection.QueryAsync<KernelLog>(
                new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            return new PagedKernelLogResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }).ConfigureAwait(false);

        _logger.LogDebug("Search returned {Count} of {Total} kernel logs",
            result.Items.Count(), result.TotalCount);

        return result;
    }

    private static (string WhereClause, DynamicParameters Parameters) BuildWhereClause(KernelLogFilterRequest filter)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        // Time range
        if (filter.From.HasValue)
        {
            conditions.Add("created_at >= @From");
            parameters.Add("From", filter.From.Value.UtcDateTime);
        }

        if (filter.To.HasValue)
        {
            conditions.Add("created_at <= @To");
            parameters.Add("To", filter.To.Value.UtcDateTime);
        }

        // Chat session ID (JSONB query - using PascalCase keys as stored by Newtonsoft.Json)
        if (filter.ChatSessionId.HasValue)
        {
            conditions.Add("details->>'ChatSessionId' = @ChatSessionId");
            parameters.Add("ChatSessionId", filter.ChatSessionId.Value.ToString());
        }

        // Conversation ID (JSONB query - using PascalCase keys as stored by Newtonsoft.Json)
        if (filter.ConversationId.HasValue)
        {
            conditions.Add("details->>'ConversationId' = @ConversationId");
            parameters.Add("ConversationId", filter.ConversationId.Value.ToString());
        }

        // Log levels (multiple values with OR using ANY)
        if (filter.Levels is { Count: > 0 })
        {
            conditions.Add("level = ANY(@Levels)");
            parameters.Add("Levels", filter.Levels.Select(l => l.ToString()).ToArray());
        }

        // Log types (JSONB query with multiple values using ANY - using PascalCase keys as stored by Newtonsoft.Json)
        // Note: Enums are stored as integers in JSONB (Newtonsoft.Json default), so we compare against integer values
        if (filter.Types is { Count: > 0 })
        {
            conditions.Add("details->>'Type' = ANY(@Types)");
            parameters.Add("Types", filter.Types.Select(t => ((int)t).ToString()).ToArray());
        }

        // Function name (JSONB partial match, case-insensitive - using PascalCase keys as stored by Newtonsoft.Json)
        if (!string.IsNullOrWhiteSpace(filter.FunctionName))
        {
            conditions.Add("details->>'FunctionName' ILIKE @FunctionName");
            parameters.Add("FunctionName", $"%{filter.FunctionName}%");
        }

        // Plugin name (JSONB partial match, case-insensitive - using PascalCase keys as stored by Newtonsoft.Json)
        if (!string.IsNullOrWhiteSpace(filter.PluginName))
        {
            conditions.Add("details->>'PluginName' ILIKE @PluginName");
            parameters.Add("PluginName", $"%{filter.PluginName}%");
        }

        // Message search (partial match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(filter.MessageSearch))
        {
            conditions.Add("message ILIKE @MessageSearch");
            parameters.Add("MessageSearch", $"%{filter.MessageSearch}%");
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        return (whereClause, parameters);
    }
}

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
}

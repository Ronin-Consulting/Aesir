using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class KernelLogService(ILogger<ChatHistoryService> logger, IDbContext dbContext): IKernelLogService
{
    static KernelLogService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirKernelLogDetails>());
    }
    
    public async Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_log_kernel (id, level, message, created_at, details)
            VALUES (@Id, @Level, @Message, @Created, @Details::jsonb)      
        ";

        await dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(sql, new { Id=Guid.NewGuid(), Level=logLevel, 
                Message=message, Created=DateTime.UtcNow, Details=details });
        }, withTransaction: true);
    }

    public async Task<IEnumerable<AesirKernelLog>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details, 
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE created_at between @From and @To
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirKernelLog>(sql, new { From=from.UtcDateTime, To=to.UtcDateTime }));
    }

    public async Task<IEnumerable<AesirKernelLog>> GetLogsByChatSessionAsync(Guid chatSessionId)
    {
        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details, 
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE details->>'ChatSessionId' =  @ChatSessionId
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirKernelLog>(sql, new { ChatSessionId=chatSessionId.ToString() }));
    }

    public async Task<IEnumerable<AesirKernelLog>> GetLogsByConversationAsync(Guid conversationId)
    {
        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details, 
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE details->>'ConversationId' =  @ConversationId
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirKernelLog>(sql, new { ConversationId=conversationId.ToString() }));
    }
}


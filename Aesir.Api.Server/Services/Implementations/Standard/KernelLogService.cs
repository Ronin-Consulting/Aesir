using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class KernelLogService(ILogger<ChatHistoryService> logger, IDbContext dbContext):IKernelLogService
{
    static KernelLogService()
    {
        // SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirKernelLogBase>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<AesirKernelLogDetailsBase>());
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

    public async Task<IEnumerable<AesirKernelLogBase>> GetLogsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        const string sql = @"
            SELECT id as Id, level as Level, created_at as CreatedAt, details::jsonb as Details, 
                message as Message
            FROM aesir.aesir_log_kernel
            WHERE created_at between @From and @To
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirKernelLogBase>(sql, new { From=from.UtcDateTime, To=to.UtcDateTime }));
    }

    public Task<IEnumerable<AesirKernelLogBase>> GetLogsAsync(Guid conversationId)
    {
        throw new NotImplementedException();
    }
}


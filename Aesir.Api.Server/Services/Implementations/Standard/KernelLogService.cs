using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class KernelLogService(ILogger<ChatHistoryService> logger, IDbContext dbContext):IKernelLogService
{
    public async Task LogAsync(KernelLogLevel logLevel, string message, AesirKernelLogDetails details)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_log_kernel (id, level, message, created_at, details)
            VALUES (@Id, @Level, @Message, @Created, @Details::jsonb)      
        ";

        await dbContext.UnitOfWorkAsync(async connection =>
        {
            await connection.ExecuteAsync(sql, new { Id=Guid.NewGuid(), Level=logLevel, 
                Message=message, Created=DateTime.UtcNow, Details=JsonConvert.SerializeObject(details) });
        }, withTransaction: true);
    }
}


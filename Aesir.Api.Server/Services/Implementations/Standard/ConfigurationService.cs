using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

public class ConfigurationService(ILogger<ConfigurationService> logger, IDbContext dbContext) : IConfigurationService
{
    public async Task<IEnumerable<AesirAgent>> GetAgentsAsync()
    {
        const string sql = @"
            SELECT id, name, chat_model as ChatModel, embedding_model as EmbeddingModel, vision_model as VisionModel, source, prompt
            FROM aesir.aesir_agent
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirAgent>(sql));
    }
    
    public async Task<AesirAgent> GetAgentAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name, chat_model as ChatModel, embedding_model as EmbeddingModel, vision_model as VisionModel, source, prompt
            FROM aesir.aesir_agent
            WHERE id = @Id::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirAgent>(sql, new { Id = id }));
    }
    public async Task<IEnumerable<AesirTool>> GetToolsAsync()
    {
        const string sql = @"
            SELECT id, name
            FROM aesir.aesir_tool
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirTool>(sql));
    }

    public async Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid agentId)
    {
        const string sql = @"
            SELECT t.id, t.name
            FROM aesir.aesir_tool t 
                INNER JOIN aesir.aesir_agent_tool at ON t.id = at.tool_id
            WHERE at.agent_id = @AgentId::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirTool>(sql, new { AgentId = agentId }));
    }
    
    public async Task<AesirTool> GetToolAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name
            FROM aesir.aesir_tool
            WHERE id = @Id::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirTool>(sql, new { Id = id }));
    }
}
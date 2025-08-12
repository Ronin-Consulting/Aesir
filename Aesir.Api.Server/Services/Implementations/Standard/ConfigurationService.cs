using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides operations for accessing and managing configuration data related to agents and tools.
/// </summary>
public class ConfigurationService(ILogger<ConfigurationService> logger, IDbContext dbContext) : IConfigurationService
{
    /// <summary>
    /// Retrieves a list of Aesir agents stored in the database asynchronously.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <c>AesirAgent</c> representing the agents retrieved from the database.
    /// </returns>
    public async Task<IEnumerable<AesirAgent>> GetAgentsAsync()
    {
        const string sql = @"
            SELECT id, name, chat_model as ChatModel, embedding_model as EmbeddingModel, vision_model as VisionModel, source, prompt
            FROM aesir.aesir_agent
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirAgent>(sql));
    }

    /// <summary>
    /// Retrieves an AesirAgent by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirAgent to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirAgent"/> object corresponding to the given identifier.
    /// If no agent is found, returns null.
    /// </returns>
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

    /// Asynchronously retrieves a collection of tools from the configuration database.
    /// This method queries and returns a list of all tools available in the database.
    /// It utilizes an SQL query to fetch `id` and `name` fields from the `aesir.aesir_tool` table.
    /// <returns>
    /// A task representing the asynchronous operation. The result contains an enumerable collection of `AesirTool` objects.
    /// </returns>
    public async Task<IEnumerable<AesirTool>> GetToolsAsync()
    {
        const string sql = @"
            SELECT id, name
            FROM aesir.aesir_tool
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirTool>(sql));
    }

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent whose tools are to be fetched.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tools used by the specified agent.</returns>
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

    /// <summary>
    /// Retrieves an Aesir tool by its unique identifier from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the Aesir tool to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirTool"/> object if found; otherwise, null.</returns>
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
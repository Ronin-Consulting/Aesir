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
    /// Handles the management of configuration by providing operations such as
    /// upserting, retrieving, deleting, and searching configurations in a database.
    /// </summary>
    /// <remarks>
    /// The service uses a database as the backend for storing and retrieving
    /// chat-related data such as chat sessions and messages. It leverages
    /// custom type handling for JSON data types using Dapper.
    /// </remarks>
    static ConfigurationService()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<IList<string?>>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<IDictionary<string, string?>>());
    }

    /// <summary>
    /// Asynchronously retrieves the Aesir general settings.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The task result contains the <see cref="AesirGeneralSettings"/>.
    /// </returns>
    public async Task<AesirGeneralSettings> GetGeneralSettingsAsync()
    {
        const string sql = @"
            SELECT rag_inf_eng_id as RagInferenceEngineId, 
                   rag_emb_model as RagEmbeddingModel, 
                   tts_model_path as TtsModelPath, 
                   stt_model_path as SttModelPath, 
                   vad_model_path as VadModelPath
            FROM aesir.aesir_general_settings
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirGeneralSettings>(sql));
    }

    /// <summary>
    /// Updates an existing AesirGeneralSetting in the database.
    /// </summary>
    /// <param name="generalSettings">The general setting with updated values.</param>
    public async Task UpdateGeneralSettingsAsync(AesirGeneralSettings generalSettings)
    {
        const string sql = @"
            UPDATE aesir.aesir_general_settings
            SET rag_inf_eng_id = @RagInferenceEngineId, 
                rag_emb_model = @RagEmbeddingModel, 
                tts_model_path = @TtsModelPath, 
                stt_model_path = @SttModelPath, 
                vad_model_path = @VadModelPath
        ";

        var rows =  await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, generalSettings));
        
        if (rows == 0)
            throw new Exception("No rows updated");
        if (rows > 1)
            throw new Exception("Multiple rows updated");
    }

    /// <summary>
    /// Asynchronously retrieves a collection of Aesir inference engines.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The result contains an enumerable collection of <c>AesirInferenceEngine</c> objects.
    /// </returns>
    public async Task<IEnumerable<AesirInferenceEngine>> GetInferenceEnginesAsync()
    {
        const string sql = @"
            SELECT id, name, description, type, configuration as Configuration
            FROM aesir.aesir_inference_engine
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirInferenceEngine>(sql));
    }

    /// <summary>
    /// Retrieves an AesirInferenceEngine by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirInferenceEngine to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirInferenceEngine"/> corresponding to the given identifier.
    /// If no agent is found, returns null.
    /// </returns>
    public async Task<AesirInferenceEngine> GetInferenceEngineAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name, description, type, configuration as Configuration
            FROM aesir.aesir_inference_engine
            WHERE id = @Id::uuid
        ";
        
        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirInferenceEngine>(sql, new { Id = id }));
    }

    /// <summary>
    /// Inserts a new AesirInferenceEngine into the database.
    /// </summary>
    /// <param name="InferenceEngine">The inference engine to insert.</param>
    public async Task CreateInferenceEngineAsync(AesirInferenceEngine inferenceEngine)
    {   
        const string sql = @"
            INSERT INTO aesir.aesir_inference_engine 
            (name, description, type, configuration)
            VALUES 
            (@Name, @Description, @Type, @Configuration::jsonb)
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, inferenceEngine));

        if (rows == 0)
            throw new Exception("No rows created");
    }

    /// <summary>
    /// Updates an existing AesirInferenceEngine in the database.
    /// </summary>
    /// <param name="agent">The inference engine with updated values.</param>
    public async Task UpdateInferenceEngineAsync(AesirInferenceEngine inferenceEngine)
    {
        const string sql = @"
            UPDATE aesir.aesir_inference_engine
            SET name = @Name,
                description = @Description,
                location = @Type,
                configuration = @Configuration::jsonb
            WHERE id = @Id
        ";

        var rows =  await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, inferenceEngine));
        
        if (rows == 0)
            throw new Exception("No rows updated");
        if (rows > 1)
            throw new Exception("Multiple rows updated");
    }

    /// <summary>
    /// Delete an existing AesirInferenceEngine from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirInferenceEngine to delete.</param>
    public async Task DeleteInferenceEngineAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM aesir.aesir_infernece_engine
            WHERE id = @Id::uuid
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");
        if (rows > 1)
            throw new Exception("Multiple rows deleted");
    }
    
    /// <summary>
    /// Retrieves a list of Aesir agents stored in the database asynchronously.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <c>AesirAgent</c> representing the agents retrieved from the database.
    /// </returns>
    public async Task<IEnumerable<AesirAgent>> GetAgentsAsync()
    {
        const string sql = @"
            SELECT id, name, description, chat_inference_engine_id as ChatInferenceEngineId, chat_model as ChatModel, vision_inference_engine_id as VisionInferenceEngineId, vision_model as VisionModel, prompt
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
            SELECT id, name, description, chat_inference_engine_id as ChatInferenceEngineId, chat_model as ChatModel, vision_inference_engine_id as VisionInferenceEngineId, vision_model as VisionModel, prompt
            FROM aesir.aesir_agent
            WHERE id = @Id::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirAgent>(sql, new { Id = id }));
    }
    
    /// <summary>
    /// Inserts a new AesirAgent into the database.
    /// </summary>
    /// <param name="agent">The agent to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateAgentAsync(AesirAgent agent)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_agent 
            (name, description, chat_inference_engine_id, chat_model, vision_inference_engine_id, vision_model, prompt)
            VALUES 
            (@Name, @Description, @ChatInferenceEngineId, @ChatModel, @VisionInferenceEngineId, @VisionModel, @Prompt)
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, agent));

        if (rows == 0)
            throw new Exception("No rows created");
    }

    /// <summary>
    /// Updates an existing AesirAgent in the database.
    /// </summary>
    /// <param name="agent">The agent with updated values.</param>
    public async Task UpdateAgentAsync(AesirAgent agent)
    {
        const string sql = @"
            UPDATE aesir.aesir_agent
            SET name = @Name,
                description = @Description,
                chat_inference_engine_id = @ChatInferenceEngineId,
                chat_model = @ChatModel,
                vision_inference_engine_id = @VisionInferenceEngineId
                vision_model = @VisionModel,
                prompt = @Prompt
            WHERE id = @Id
        ";

        var rows =  await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, agent));
        
        if (rows == 0)
            throw new Exception("No rows updated");
        if (rows > 1)
            throw new Exception("Multiple rows updated");
    }

    /// <summary>
    /// Delete an existing AesirAgent from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirAgent to delete.</param>
    public async Task DeleteAgentAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM aesir.aesir_agent
            WHERE id = @Id::uuid
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");
        if (rows > 1)
            throw new Exception("Multiple rows deleted");
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
            SELECT id, name, type, description, mcp_server_id AS McpServerId, mcp_server_tool_name AS McpServerTool
            FROM aesir.aesir_tool
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirTool>(sql));
    }

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent whose tools are to be fetched.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tools used by the specified agent.</returns>
    public async Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid id)
    {
        const string sql = @"
            SELECT t.id, t.name, t.type, t.description, mcp_server_id AS McpServerId, mcp_server_tool_name AS McpServerTool
            FROM aesir.aesir_tool t 
                INNER JOIN aesir.aesir_agent_tool at ON t.id = at.tool_id
            WHERE at.agent_id = @AgentId::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirTool>(sql, new { AgentId = id }));
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
            SELECT id, name, type, description, mcp_server_id AS McpServerId, mcp_server_tool_name AS McpServerTool
            FROM aesir.aesir_tool
            WHERE id = @Id::uuid
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirTool>(sql, new { Id = id }));
    }
    
    /// <summary>
    /// Inserts a new AesirTool into the database.
    /// </summary>
    /// <param name="tool">The tool to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateToolAsync(AesirTool tool)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_tool 
            (name, description, type, mcp_server_id, mcp_server_tool_name)
            VALUES 
            (@Name, @Description, @Type, @McpServerId, @McpServerTool)
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, tool));

        if (rows == 0)
            throw new Exception("No rows created");
    }
    
    /// <summary>
    /// Updates an existing AesirTool in the database.
    /// </summary>
    /// <param name="tool">The agent with updated values.</param>
    public async Task UpdateToolAsync(AesirTool tool)
    {
        const string sql = @"
            UPDATE aesir.aesir_tool
            SET name = @Name,
                description = @Description,
                type = @Type,
                mcp_server_id = @McpServerId,
                mcp_server_tool_name = @McpServerTool
            WHERE id = @Id
        ";

        var rows =  await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, tool));
        
        if (rows == 0)
            throw new Exception("No rows updated");
        if (rows > 1)
            throw new Exception("Multiple rows updated");
    }

    /// <summary>
    /// Delete an existing AesirTool from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirTool to delete.</param>
    public async Task DeleteToolAsync(Guid id)
    {
        const string sql = @"
            DELETE FROM aesir.aesir_tool
            WHERE id = @Id::uuid
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");
        if (rows > 1)
            throw new Exception("Multiple rows deleted");
    }
    
    /// <summary>
    /// Retrieves a list of Aesir MCP Servers stored in the database asynchronously.
    /// </summary>
    /// <returns>
    /// An enumerable collection of <c>AesirMcpServer</c> representing the agents retrieved from the database.
    /// </returns>
    public async Task<IEnumerable<AesirMcpServer>> GetMcpServersAsync()
    {
        const string sql = @"
            SELECT id, name, description, location, command, arguments, environment_variables as EnvironmentVariables, url, http_headers as HttpHeaders
            FROM aesir.aesir_mcp_server
        ";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<AesirMcpServer>(sql));
    }

    /// <summary>
    /// Retrieves an AesirMcpServer by its unique identifier asynchronously.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirMcpServer to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirMcpServer"/> object corresponding to the given identifier.
    /// If no MCP Server is found, returns null.
    /// </returns>
    public async Task<AesirMcpServer> GetMcpServerAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name, description, location, command, arguments, environment_variables as EnvironmentVariables, url, http_headers as HttpHeaders
            FROM aesir.aesir_mcp_server
            WHERE id = @Id::uuid
        ";
        
        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<AesirMcpServer>(sql, new { Id = id }));
    }
    
    /// <summary>
    /// Inserts a new AesirMcpServer into the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateMcpServerAsync(AesirMcpServer mcpServer)
    {
        const string sql = @"
            INSERT INTO aesir.aesir_mcp_server 
            (name, description, location, command, arguments, environment_variables, url, http_headers)
            VALUES 
            (@Name, @Description, @Location, @Command, @Arguments::jsonb, @EnvironmentVariables::jsonb, @Url, @HttpHeaders::jsonb)
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, mcpServer));

        if (rows == 0)
            throw new Exception("No rows created");
    }
    
    /// <summary>
    /// Updates an existing AesirMcpServer in the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server with updated values.</param>
    public async Task UpdateMcpServerAsync(AesirMcpServer mcpServer)
    {
        const string sql = @"
            UPDATE aesir.aesir_mcp_server 
            SET name = @Name,
                description = @Description,
                location = @Location,
                command = @Command,
                arguments = @Arguments::jsonb,
                environment_variables = @EnvironmentVariables::jsonb,
                url = @Url,
                http_headers = @HttpHeaders::jsonb
            WHERE id = @Id
        ";

        var rows =  await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, mcpServer));
        
        if (rows == 0)
            throw new Exception("No rows updated");
        if (rows > 1)
            throw new Exception("Multiple rows updated");
    }

    /// <summary>
    /// Delete an existing AesirMcpServer from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the AesirMcpServer to delete.</param>
    public async Task DeleteMcpServerAsync(Guid id)
    {
        const string sql = @"
        DELETE FROM aesir.aesir_mcp_server
        WHERE id = @Id::uuid
    ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");
        if (rows > 1)
            throw new Exception("Multiple rows deleted");
    }
}
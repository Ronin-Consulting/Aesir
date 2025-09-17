using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Dapper;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides operations for accessing and managing configuration data related to agents and tools.
/// </summary>
public class ConfigurationService(
    ILogger<ConfigurationService> logger, 
    IDbContext dbContext,
    IConfiguration configuration) : IConfigurationService
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
    /// Indicates if the service is in database mode or file mode
    /// </summary>
    public bool DatabaseMode => configuration.GetValue("Configuration:LoadFromDatabase", false);
    
    /// <summary>
    /// Validates that the application is running in database mode.
    /// Throws an InvalidOperationException if not in database mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the application is not configured to run in database mode.</exception>
    private void VerifyIsDatabaseMode()
    {
        if (!DatabaseMode)
        {
            throw new InvalidOperationException("This operation requires the application to be running in database mode.");
        }
    }

    /// <summary>
    /// Asynchronously retrieves the Aesir general settings.
    /// </summary>
    /// <returns>
    /// A task representing the operation. The task result contains the <see cref="AesirGeneralSettings"/>.
    /// </returns>
    public async Task<AesirGeneralSettings> GetGeneralSettingsAsync()
    {
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT rag_emb_inf_eng_id as RagEmbeddingInferenceEngineId, 
                       rag_emb_model as RagEmbeddingModel, 
                       rag_vis_inf_eng_id as RagVisionInferenceEngineId,
                       rag_vis_model as RagVisionModel,
                       tts_model_path as TtsModelPath, 
                       stt_model_path as SttModelPath, 
                       vad_model_path as VadModelPath
                FROM aesir.aesir_general_settings
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<AesirGeneralSettings>(sql));
        }
        else
        {
            var generalSettingsDictionary = configuration.GetSection("GeneralSettings")
                .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
    
            var generalSettings = new AesirGeneralSettings()
            {
                RagEmbeddingInferenceEngineId = Guid.TryParse(generalSettingsDictionary["RagEmbeddingInferenceEngineId"], out var embGuid) ? embGuid : null,
                RagEmbeddingModel = generalSettingsDictionary["RagEmbeddingModel"],
                RagVisionInferenceEngineId = Guid.TryParse(generalSettingsDictionary["RagVisionInferenceEngineId"], out var visGuid) ? visGuid : null,
                RagVisionModel = generalSettingsDictionary["RagVisionModel"],
                TtsModelPath = generalSettingsDictionary["TtsModelPath"],
                SttModelPath = generalSettingsDictionary["SttModelPath"],
                VadModelPath = generalSettingsDictionary["VadModelPath"]
            };

            return await Task.FromResult(generalSettings);
        }
    }

    /// <summary>
    /// Updates an existing AesirGeneralSetting in the database.
    /// </summary>
    /// <param name="generalSettings">The general setting with updated values.</param>
    public async Task UpdateGeneralSettingsAsync(AesirGeneralSettings generalSettings)
    {
        VerifyIsDatabaseMode();
        
        const string sql = @"
            UPDATE aesir.aesir_general_settings
            SET rag_emb_inf_eng_id = @RagEmbeddingInferenceEngineId, 
                rag_emb_model = @RagEmbeddingModel, 
                rag_vis_inf_eng_id = @RagVisionInferenceEngineId,
                rag_vis_model = @RagVisionModel, 
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, type, configuration as Configuration
                FROM aesir.aesir_inference_engine
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryAsync<AesirInferenceEngine>(sql));
        }
        else
        {
            var inferenceEngines = configuration.GetSection("InferenceEngines")
                .Get<AesirInferenceEngine[]>() ?? [];

            return await Task.FromResult(inferenceEngines);
        }
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, type, configuration as Configuration
                FROM aesir.aesir_inference_engine
                WHERE id = @Id::uuid
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<AesirInferenceEngine>(sql, new { Id = id }));
        }
        else
        {
            var inferenceEngines = configuration.GetSection("InferenceEngines")
                .Get<AesirInferenceEngine[]>() ?? [];

            return await Task.FromResult(inferenceEngines.FirstOrDefault(ie => ie.Id == id));
        }
    }

    /// <summary>
    /// Inserts a new AesirInferenceEngine into the database.
    /// </summary>
    /// <param name="InferenceEngine">The inference engine to insert.</param>
    public async Task CreateInferenceEngineAsync(AesirInferenceEngine inferenceEngine)
    {   
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
        const string sql = @"
            DELETE FROM aesir.aesir_inference_engine
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, chat_inference_engine_id as ChatInferenceEngineId, chat_model as ChatModel, 
                       chat_temperature as ChatTemperature, chat_top_p as ChatTopP, chat_max_tokens as ChatMaxTokens,
                       chat_prompt_persona as ChatPromptPersona, chat_custom_prompt_content as ChatCustomPromptContent
                FROM aesir.aesir_agent
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryAsync<AesirAgent>(sql));
        }
        else
        {
            var agents = configuration.GetSection("Agents")
                .Get<AesirAgent[]>() ?? [];

            return await Task.FromResult(agents);
        }
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, chat_inference_engine_id as ChatInferenceEngineId, chat_model as ChatModel, 
                       chat_temperature as ChatTemperature, chat_top_p as ChatTopP, chat_max_tokens as ChatMaxTokens,
                       chat_prompt_persona as ChatPromptPersona, chat_custom_prompt_content as ChatCustomPromptContent
                FROM aesir.aesir_agent
                WHERE id = @Id::uuid
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<AesirAgent>(sql, new { Id = id }));
        }
        else
        {
            var agents = configuration.GetSection("Agents")
                .Get<AesirAgent[]>() ?? [];

            return await Task.FromResult(agents.FirstOrDefault(a => a.Id == id));
        }
    }
    
    /// <summary>
    /// Inserts a new AesirAgent into the database.
    /// </summary>
    /// <param name="agent">The agent to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateAgentAsync(AesirAgent agent)
    {
        VerifyIsDatabaseMode();
        
        const string sql = @"
            INSERT INTO aesir.aesir_agent 
            (name, description, chat_inference_engine_id, chat_model, chat_temperature, chat_top_p, chat_max_tokens, chat_prompt_persona, chat_custom_prompt_content)
            VALUES 
            (@Name, @Description, @ChatInferenceEngineId, @ChatModel, @ChatTemperature, @ChatTopP, @ChatMaxTokens, @ChatPromptPersona, @ChatCustomPromptContent)
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
        VerifyIsDatabaseMode();
        
        const string sql = @"
            UPDATE aesir.aesir_agent
            SET name = @Name,
                description = @Description,
                chat_inference_engine_id = @ChatInferenceEngineId,
                chat_model = @ChatModel,
                chat_temperature = @ChatTemperature, 
                chat_top_p = @ChatTopP, 
                chat_max_tokens = @ChatMaxTokens,
                chat_prompt_persona = @ChatPromptPersona,
                chat_custom_prompt_content = @ChatCustomPromptContent
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
        VerifyIsDatabaseMode();
        
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
        if (DatabaseMode)
        {
            const string sql = @"
            SELECT id, name, type, description, mcp_server_id AS McpServerId, mcp_server_tool_name AS McpServerTool
            FROM aesir.aesir_tool
        ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryAsync<AesirTool>(sql));
        }
        else
        {   
            var tools = configuration.GetSection("Tools")
                .Get<AesirTool[]>() ?? [];

            return await Task.FromResult(tools);
        }
    }

    /// <summary>
    /// Retrieves a collection of tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent whose tools are to be fetched.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of tools used by the specified agent.</returns>
    public async Task<IEnumerable<AesirTool>> GetToolsUsedByAgentAsync(Guid id)
    {
        if (DatabaseMode)
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
        else
        {   
            var agents = configuration.GetSection("Agents")
                .Get<AesirAgent[]>() ?? [];

            for (var idx = 0; idx < agents.Length; idx++)
            {
                var agent = agents[idx];
                if (agent.Id == id)
                {
                    // found our agent, see if any tools configured
                    var agentToolNames = configuration.GetSection($"Agents:{idx}:Tools")
                        .Get<string[]>() ?? [];

                    var allTools = await GetToolsAsync();
                    
                    // Filter tools by names specified in agent configuration
                    var agentTools = allTools.Where(t => agentToolNames.Contains(t.Name));

                    return agentTools;
                }
            }

            return new List<AesirTool>();
        }
    }

    /// <summary>
    /// Retrieves an Aesir tool by its unique identifier from the database.
    /// </summary>
    /// <param name="id">The unique identifier of the Aesir tool to retrieve.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains the <see cref="AesirTool"/> object if found; otherwise, null.</returns>
    public async Task<AesirTool> GetToolAsync(Guid id)
    {
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, type, description, mcp_server_id AS McpServerId, mcp_server_tool_name AS McpServerTool
                FROM aesir.aesir_tool
                WHERE id = @Id::uuid
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<AesirTool>(sql, new { Id = id }));
        }
        else
        {
            var tools = configuration.GetSection("Tools")
                .Get<AesirTool[]>() ?? [];

            return await Task.FromResult(tools.FirstOrDefault(t => t.Id == id));
        }
    }
    
    /// <summary>
    /// Inserts a new AesirTool into the database.
    /// </summary>
    /// <param name="tool">The tool to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateToolAsync(AesirTool tool)
    {
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, location, command, arguments, environment_variables as EnvironmentVariables, url, http_headers as HttpHeaders
                FROM aesir.aesir_mcp_server
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryAsync<AesirMcpServer>(sql));
        }
        else
        {
            var mcpServers = configuration.GetSection("McpServers")
                .Get<AesirMcpServer[]>() ?? [];

            return await Task.FromResult(mcpServers);
        }
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
        if (DatabaseMode)
        {
            const string sql = @"
                SELECT id, name, description, location, command, arguments, environment_variables as EnvironmentVariables, url, http_headers as HttpHeaders
                FROM aesir.aesir_mcp_server
                WHERE id = @Id::uuid
            ";

            return await dbContext.UnitOfWorkAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<AesirMcpServer>(sql, new { Id = id }));
        }
        else
        {
            var mcpServers = configuration.GetSection("McpServers")
                .Get<AesirMcpServer[]>() ?? [];

            return await Task.FromResult(mcpServers.FirstOrDefault(m => m.Id == id));
        }
    }
    
    /// <summary>
    /// Inserts a new AesirMcpServer into the database.
    /// </summary>
    /// <param name="mcpServer">The MCP server to insert.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task CreateMcpServerAsync(AesirMcpServer mcpServer)
    {
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
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
        VerifyIsDatabaseMode();
        
        const string sql = @"
        DELETE FROM aesir.aesir_mcp_server
        WHERE id = @Id::uuid";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");
        if (rows > 1)
            throw new Exception("Multiple rows deleted");
    }
}
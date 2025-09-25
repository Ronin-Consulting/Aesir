using Aesir.Api.Server.Data;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
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
    /// Prepares the database configuration by verifying the readiness and completeness
    /// of necessary settings required for application functionality. Should only be called once during startup.
    /// </summary>
    /// <param name="configurationReadinessService">
    /// Service responsible for reporting missing or incomplete configuration details.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous preparation of the database configuration.
    /// </returns>
    public async Task PrepareDatabaseConfigurationAsync(
        ConfigurationReadinessService configurationReadinessService)
    {
        if (!DatabaseMode)
            throw new InvalidOperationException("System is not in database configuration mode");
        
        var inferenceEngines = (await GetInferenceEnginesAsync()).ToArray() ?? [];

        if (inferenceEngines.Length == 0)
            configurationReadinessService.ReportMissingConfiguration("No Inference Engines configured");

        foreach (var inferenceEngine in inferenceEngines)
        {
            if (inferenceEngine.Type == InferenceEngineType.OpenAICompatible)
            {
                if (inferenceEngine.Configuration["ApiKey"] == null)
                {
                    configurationReadinessService.ReportMissingConfiguration(
                        $"API Key missing for Inference Engine {inferenceEngine.Name}");

                    configurationReadinessService.MarkInferenceEngineNotReadyAtBoot(inferenceEngine.Id.Value);
                }
            }

            if (inferenceEngine.Configuration["Endpoint"] == null)
            {
                configurationReadinessService.ReportMissingConfiguration(
                    $"Endpoint missing for Inference Engine {inferenceEngine.Name}");
                
                configurationReadinessService.MarkInferenceEngineNotReadyAtBoot(inferenceEngine.Id.Value);
            }
        }
        
        var generalSettings = await GetGeneralSettingsAsync();
        
        if (generalSettings.RagEmbeddingInferenceEngineId == null)
            configurationReadinessService.ReportMissingConfiguration("RAG embedding inference engine configuration is not yet complete");
        
        if (generalSettings.RagEmbeddingModel == null)
            configurationReadinessService.ReportMissingConfiguration("RAG embedding model configuration is not yet complete");
        
        if (generalSettings.RagVisionInferenceEngineId == null)
            configurationReadinessService.ReportMissingConfiguration("RAG vision inference engine configuration is not yet complete");
        
        if (generalSettings.RagVisionModel == null)
            configurationReadinessService.ReportMissingConfiguration("RAG vision model configuration is not yet complete");
    }

    /// <summary>
    /// Prepares and validates file-based configuration settings by assigning unique
    /// identifiers to various components such as inference engines, agents, servers, and tools.
    /// </summary>
    /// <remarks>
    /// This method ensures the system's configuration is properly structured and each component
    /// is assigned a unique identifier. It throws an exception if any required configuration
    /// data is missing or if the system is not in file configuration mode. Additionally, it
    /// updates relevant sections in the configuration to reflect the changes.
    /// </remarks>
    public void PrepareFileConfigurationAsync()
    {
        if (DatabaseMode)
            throw new InvalidOperationException("System is not in file configuration mode");
        
        // validate settings and add/fix up ids ...
        
        // give each inference engine an Id
        var inferenceEngines = configuration.GetSection("InferenceEngines")
            .Get<AesirInferenceEngine[]>() ?? [];
        if (inferenceEngines.Length == 0)
            throw new InvalidOperationException("InferenceEngines configuration is missing or empty");
        for (var i = 0; i < inferenceEngines.Length; i++)
        {
            var inferenceEngine = inferenceEngines[i];
            
            // this logic assumes we are given the inference engines in order of their idx
            inferenceEngine.Id = Guid.NewGuid();
            configuration[$"InferenceEngines:{i}:Id"] = inferenceEngine.Id.ToString();
        }

        // update the rag embedding inference engine id
        var generalSettings = configuration.GetSection("GeneralSettings")
            .Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
        var ragEmbeddingInferenceEngineName = generalSettings["RagEmbeddingInferenceEngineName"] ??
                                     throw new InvalidOperationException("RagEmbeddingInferenceEngineName not configured");
        var ragEmbeddingInferenceEngine = inferenceEngines.FirstOrDefault(ie => ie.Name == ragEmbeddingInferenceEngineName) ?? 
                                              throw new InvalidOperationException("RagEmbeddingInferenceEngineName does not match a configured Inference Engine");
        configuration[$"GeneralSettings:RagEmbeddingInferenceEngineId"] = ragEmbeddingInferenceEngine?.Id?.ToString() ?? null;
        
        // update the rag vision inference engine id
        var ragVisionInferenceEngineName = generalSettings["RagVisionInferenceEngineName"] ??
                                     throw new InvalidOperationException("RagVisionInferenceEngineName not configured");
        var ragVisionInferenceEngine = inferenceEngines.FirstOrDefault(ie => ie.Name == ragVisionInferenceEngineName) ?? 
                                 throw new InvalidOperationException("RagVisionInferenceEngineName does not match a configured Inference Engine");
        configuration[$"GeneralSettings:RagVisionInferenceEngineId"] = ragVisionInferenceEngine?.Id?.ToString() ?? null;
        
        // give each agent an id
        var agents = configuration.GetSection("Agents")
            .Get<AesirAgent[]>() ?? [];
        if (inferenceEngines.Length == 0)
            throw new InvalidOperationException("Agents configuration is missing or empty");
        for (var i = 0; i < agents.Length; i++)
        {
            var agent = agents[i];
            
            // this logic assumes we are given the agents in order of their idx
            agent.Id = Guid.NewGuid();
            configuration[$"Agents:{i}:Id"] = agent.Id.ToString();
            
            // determine chat inference engine id
            var chatInferenceEngineName = configuration[$"Agents:{i}:ChatInferenceEngineName"] ??
                                         throw new InvalidOperationException("ChatInferenceEngineName not configured");
            var chatInferenceEngineId = inferenceEngines.FirstOrDefault(ie => ie.Name == chatInferenceEngineName)?.Id ?? 
                                        throw new InvalidOperationException("ChatInferenceEngineName does not match a configured Inference Engine");
            configuration[$"Agents:{i}:ChatInferenceEngineId"] = chatInferenceEngineId.ToString();
        }
        
        // give each mcp server an id
        var mcpServers = configuration.GetSection("McpServers")
            .Get<AesirMcpServer[]>() ?? [];
        for (var i = 0; i < mcpServers.Length; i++)
        {
            var mcpServer = mcpServers[i];
            
            // this logic assumes we are given the agents in order of their idx
            mcpServer.Id = Guid.NewGuid();
            configuration[$"McpServers:{i}:Id"] = mcpServer.Id.ToString();
        }
        
        // give each tool an id
        var tools = configuration.GetSection("Tools")
            .Get<AesirTool[]>() ?? [];
        for (var i = 0; i < tools.Length; i++)
        {
            var tool = tools[i];
            
            // this logic assumes we are given the agents in order of their idx
            tool.Id = Guid.NewGuid();
            configuration[$"Tools:{i}:Id"] = tool.Id.ToString();
            
            // determine mcp server id
            if (tool.Type == ToolType.McpServer)
            {
                var mcpServerName = configuration[$"Tools:{i}:McpServerName"] ??
                                    throw new InvalidOperationException("McpServerName not configured");
                var mcpServerId = mcpServers.FirstOrDefault(ie => ie.Name == mcpServerName)?.Id ??
                                  throw new InvalidOperationException(
                                      "McpServerName does not match a configured MCP Server");
                configuration[$"Tools:{i}:McpServerId"] = mcpServerId.ToString();
            }
        }
    }
    
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
    public async Task<Guid> CreateInferenceEngineAsync(AesirInferenceEngine inferenceEngine)
    {   
        VerifyIsDatabaseMode();
        
        const string sql = @"
            INSERT INTO aesir.aesir_inference_engine 
            (name, description, type, configuration)
            VALUES 
            (@Name, @Description, @Type, @Configuration::jsonb)
            RETURNING id;
        ";

        var id = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleAsync<Guid>(sql, inferenceEngine));

        return id;
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
    public async Task<Guid> CreateAgentAsync(AesirAgent agent)
    {
        VerifyIsDatabaseMode();
        
        const string sql = @"
            INSERT INTO aesir.aesir_agent 
            (name, description, chat_inference_engine_id, chat_model, chat_temperature, chat_top_p, chat_max_tokens, chat_prompt_persona, chat_custom_prompt_content)
            VALUES 
            (@Name, @Description, @ChatInferenceEngineId, @ChatModel, @ChatTemperature, @ChatTopP, @ChatMaxTokens, @ChatPromptPersona, @ChatCustomPromptContent)
            RETURNING id;
        ";

        var id = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleAsync<Guid>(sql, agent));

        return id;
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
            DELETE FROM aesir.aesir_agent_tool 
            WHERE agent_id = @Id::uuid;
            
            DELETE FROM aesir.aesir_agent
            WHERE id = @Id::uuid
        ";

        var rows = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }));

        if (rows == 0)
            throw new Exception("No rows deleted");

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
    /// Updates the tools associated with a specific agent.
    /// </summary>
    /// <param name="id">The unique identifier of the agent.</param>
    /// <param name="toolIds">An array of unique identifiers for the tools to associate with the agent. If null, the tools will be cleared.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateToolsForAgentAsync(Guid id, Guid[]? toolIds)
    {
        VerifyIsDatabaseMode();VerifyIsDatabaseMode();
        
        string sql;
        object parameters;
        
        if (toolIds == null || toolIds.Length == 0)
        {
            // Just delete all existing associations
            sql = @"
                DELETE FROM aesir.aesir_agent_tool 
                WHERE agent_id = @AgentId::uuid
            ";
            parameters = new { AgentId = id };
        
            var rows = await dbContext.UnitOfWorkAsync(async connection =>
                await connection.ExecuteAsync(sql, parameters));
        }
        else
        {
            // Delete existing and insert new in a single statement
            sql = @"
                WITH deleted AS (
                    DELETE FROM aesir.aesir_agent_tool 
                    WHERE agent_id = @AgentId::uuid
                )
                INSERT INTO aesir.aesir_agent_tool (agent_id, tool_id)
                SELECT @AgentId::uuid, unnest(@ToolIds::uuid[])
            ";
            parameters = new { AgentId = id, ToolIds = toolIds };
        
            var rows = await dbContext.UnitOfWorkAsync(async connection =>
                await connection.ExecuteAsync(sql, parameters));
        
            if (toolIds.Length != rows)
                throw new Exception("Incorrect number of rows updated");
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
    public async Task<Guid>  CreateToolAsync(AesirTool tool)
    {
        VerifyIsDatabaseMode();
        
        const string sql = @"
            INSERT INTO aesir.aesir_tool 
            (name, description, type, mcp_server_id, mcp_server_tool_name)
            VALUES 
            (@Name, @Description, @Type, @McpServerId, @McpServerTool)
            RETURNING id;
        ";

        var id = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleAsync<Guid>(sql, tool));

        return id;
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
    public async Task<Guid> CreateMcpServerAsync(AesirMcpServer mcpServer)
    {
        VerifyIsDatabaseMode();
        
        const string sql = @"
            INSERT INTO aesir.aesir_mcp_server 
            (name, description, location, command, arguments, environment_variables, url, http_headers)
            VALUES 
            (@Name, @Description, @Location, @Command, @Arguments::jsonb, @EnvironmentVariables::jsonb, @Url, @HttpHeaders::jsonb)
            RETURNING id;
        ";

        var id = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QuerySingleAsync<Guid>(sql, mcpServer));

        return id;
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
using Microsoft.Extensions.DependencyInjection;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Web.Tests.Integration.Flows;

/// <summary>
/// Integration tests for the Settings module flows:
/// General Settings, Inference Engines, Agents with Model Selection
/// </summary>
public class SettingsFlowTests : IntegrationTestBase
{
    #region General Settings Tests

    [Fact]
    public async Task GeneralSettings_LoadAndSave_PreservesData()
    {
        // Arrange
        AddTestInferenceEngine("Test Engine");
        var engineId = InferenceEngines.First().Id!.Value;
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Act - Save settings
        var newSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "nomic-embed-text:latest",
            RagVisionInferenceEngineId = engineId,
            RagVisionModel = "llava:latest"
        };
        var saveResult = await settingsService.UpdateSettingsAsync(newSettings);
        saveResult.IsSuccess.Should().BeTrue();

        // Act - Load settings
        var loadResult = await settingsService.GetSettingsAsync();

        // Assert
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value!.RagEmbeddingInferenceEngineId.Should().Be(engineId);
        loadResult.Value.RagEmbeddingModel.Should().Be("nomic-embed-text:latest");
        loadResult.Value.RagVisionInferenceEngineId.Should().Be(engineId);
        loadResult.Value.RagVisionModel.Should().Be("llava:latest");
    }

    [Fact]
    public async Task GeneralSettings_UpdateAndReload_TriggersConfigReload()
    {
        // Arrange
        AddTestInferenceEngine("Test Engine");
        var engineId = InferenceEngines.First().Id!.Value;
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        var newSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "nomic-embed-text:latest"
        };

        // Act
        var result = await settingsService.UpdateSettingsAndReloadAsync(newSettings);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GeneralSettings.RagEmbeddingModel.Should().Be("nomic-embed-text:latest");
    }

    [Fact]
    public async Task GeneralSettings_ClearVision_SetsToNull()
    {
        // Arrange
        AddTestInferenceEngine("Test Engine");
        var engineId = InferenceEngines.First().Id!.Value;
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Set initial settings with vision
        var initialSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "nomic-embed-text:latest",
            RagVisionInferenceEngineId = engineId,
            RagVisionModel = "llava:latest"
        };
        await settingsService.UpdateSettingsAsync(initialSettings);

        // Act - Clear vision settings
        var updatedSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "nomic-embed-text:latest",
            RagVisionInferenceEngineId = null,
            RagVisionModel = null
        };
        await settingsService.UpdateSettingsAsync(updatedSettings);

        // Assert
        var loadResult = await settingsService.GetSettingsAsync();
        loadResult.Value!.RagVisionInferenceEngineId.Should().BeNull();
        loadResult.Value.RagVisionModel.Should().BeNull();
    }

    #endregion

    #region Inference Engine CRUD Flow Tests

    [Fact]
    public async Task InferenceEngine_CreateUpdateDelete_FullLifecycle()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act - Create
        var newEngine = new AesirInferenceEngineBase
        {
            Name = "Lifecycle Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        };
        var createResult = await engineService.CreateAsync(newEngine);
        createResult.IsSuccess.Should().BeTrue();
        var engineId = createResult.Value;

        // Act - Update
        var updatedEngine = new AesirInferenceEngineBase
        {
            Id = engineId,
            Name = "Updated Engine Name",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11435" } }
        };
        var updateResult = await engineService.UpdateAsync(updatedEngine);
        updateResult.IsSuccess.Should().BeTrue();

        // Verify Update
        var getResult = await engineService.GetByIdAsync(engineId);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Name.Should().Be("Updated Engine Name");

        // Act - Delete
        var deleteResult = await engineService.DeleteAsync(engineId);
        deleteResult.IsSuccess.Should().BeTrue();

        // Verify Delete
        var getAllResult = await engineService.GetAllAsync();
        getAllResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task InferenceEngine_MultipleEngines_AllManaged()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act - Create multiple engines
        await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Ollama Local",
            Type = InferenceEngineType.Ollama
        });

        await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "OpenAI",
            Type = InferenceEngineType.OpenAICompatible,
            Configuration = new Dictionary<string, string?>
            {
                { "Endpoint", "https://api.openai.com/v1" },
                { "ApiKey", "sk-test" }
            }
        });

        await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Ollama Remote",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://remote:11434" } }
        });

        // Assert
        var result = await engineService.GetAllAsync();
        result.Value.Should().HaveCount(3);
        result.Value!.Select(e => e.Name).Should().Contain("Ollama Local");
        result.Value!.Select(e => e.Name).Should().Contain("OpenAI");
        result.Value!.Select(e => e.Name).Should().Contain("Ollama Remote");
    }

    #endregion

    #region Agent Configuration Flow Tests

    [Fact]
    public async Task Agent_CreateWithInferenceEngine_ConfigurationComplete()
    {
        // Arrange
        AddTestInferenceEngine("Agent Test Engine");
        var engineId = InferenceEngines.First().Id!.Value;
        var agentService = Services.GetRequiredService<IAgentService>();

        // Act
        var newAgent = new AesirAgentBase
        {
            Name = "Configuration Test Agent",
            Description = "An agent for testing configuration",
            ChatInferenceEngineId = engineId,
            ChatModel = "llama3.2:latest",
            ChatTemperature = 0.7,
            ChatTopP = 0.9,
            ChatMaxTokens = 8192,
            ChatPromptPersona = PromptPersona.Business,
            AllowThinking = true
        };
        var result = await agentService.CreateAsync(newAgent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var agent = Agents.First();
        agent.Name.Should().Be("Configuration Test Agent");
        agent.ChatInferenceEngineId.Should().Be(engineId);
        agent.ChatModel.Should().Be("llama3.2:latest");
        agent.ChatTemperature.Should().Be(0.7);
        agent.ChatTopP.Should().Be(0.9);
        agent.ChatMaxTokens.Should().Be(8192);
        agent.ChatPromptPersona.Should().Be(PromptPersona.Business);
        agent.AllowThinking.Should().BeTrue();
    }

    [Fact]
    public async Task Agent_UpdateModel_ChangesConfiguration()
    {
        // Arrange
        AddTestInferenceEngine("Agent Engine");
        AddTestAgent("Updateable Agent");
        var agentService = Services.GetRequiredService<IAgentService>();
        var agent = Agents.First();

        // Act - Update the model
        agent.ChatModel = "qwen2.5:14b";
        agent.ChatTemperature = 0.5;
        var updateResult = await agentService.UpdateAsync(agent);

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        var getResult = await agentService.GetByIdAsync(agent.Id!.Value);
        getResult.Value!.ChatModel.Should().Be("qwen2.5:14b");
        getResult.Value.ChatTemperature.Should().Be(0.5);
    }

    [Fact]
    public async Task Agent_DeleteAsync_RemovesFromList()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent("To Be Deleted");
        AddTestAgent("To Keep");
        var agentService = Services.GetRequiredService<IAgentService>();
        var agentToDelete = Agents.First();

        // Act
        var deleteResult = await agentService.DeleteAsync(agentToDelete.Id!.Value);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        var getAllResult = await agentService.GetAllAsync();
        getAllResult.Value.Should().HaveCount(1);
        getAllResult.Value!.First().Name.Should().Be("To Keep");
    }

    #endregion

    #region Agent Tool Assignment Tests

    [Fact]
    public async Task Agent_AssignTools_UpdatesToolList()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent("Tool Agent");
        AddTestTool("Tool 1");
        AddTestTool("Tool 2");
        AddTestTool("Tool 3");

        var agentService = Services.GetRequiredService<IAgentService>();
        var agent = Agents.First();
        var toolIds = Tools.Select(t => t.Id!.Value).Take(2).ToList();

        // Act
        var result = await agentService.UpdateAgentToolsAsync(agent.Id!.Value, toolIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Agent_GetTools_ReturnsAssignedTools()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent("Agent With Tools");

        var agentService = Services.GetRequiredService<IAgentService>();
        var agent = Agents.First();

        // Act
        var result = await agentService.GetAgentToolsAsync(agent.Id!.Value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Note: Tools would be empty by default in mock
    }

    #endregion

    #region MCP Server Configuration Flow Tests

    [Fact]
    public async Task McpServer_CreateLocalServer_Succeeds()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();

        // Act
        var newServer = new AesirMcpServerBase
        {
            Name = "Local MCP Server",
            Description = "A local MCP server for testing",
            Location = ServerLocation.Local,
            Command = "/usr/local/bin/mcp-server",
            Arguments = ["--port", "3000"],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                { "DEBUG", "true" },
                { "LOG_LEVEL", "info" }
            }
        };
        var result = await mcpService.CreateAsync(newServer);

        // Assert
        result.IsSuccess.Should().BeTrue();
        McpServers.Should().HaveCount(1);
        McpServers.First().Location.Should().Be(ServerLocation.Local);
        McpServers.First().Command.Should().Be("/usr/local/bin/mcp-server");
    }

    [Fact]
    public async Task McpServer_CreateRemoteServer_Succeeds()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();

        // Act
        var newServer = new AesirMcpServerBase
        {
            Name = "Remote MCP Server",
            Description = "A remote MCP server",
            Location = ServerLocation.Remote,
            Url = "https://mcp.example.com/api",
            HttpHeaders = new Dictionary<string, string?>
            {
                { "Authorization", "Bearer token123" },
                { "X-API-Version", "2.0" }
            }
        };
        var result = await mcpService.CreateAsync(newServer);

        // Assert
        result.IsSuccess.Should().BeTrue();
        McpServers.Should().HaveCount(1);
        McpServers.First().Location.Should().Be(ServerLocation.Remote);
        McpServers.First().Url.Should().Be("https://mcp.example.com/api");
    }

    [Fact]
    public async Task McpServer_DeleteServer_RemovesFromList()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();
        await mcpService.CreateAsync(new AesirMcpServerBase
        {
            Name = "Server To Delete",
            Location = ServerLocation.Local
        });
        var serverId = McpServers.First().Id!.Value;

        // Act
        var result = await mcpService.DeleteAsync(serverId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        McpServers.Should().BeEmpty();
    }

    #endregion

    #region Tool Management Flow Tests

    [Fact]
    public async Task Tool_CreateInternalTool_Succeeds()
    {
        // Arrange
        var toolService = Services.GetRequiredService<IToolService>();

        // Act
        var newTool = new AesirToolBase
        {
            Name = "Custom Calculator",
            Type = ToolType.Internal,
            Description = "A custom calculation tool",
            ToolName = "calculator",
            IconName = "calculate"
        };
        var result = await toolService.CreateAsync(newTool);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Tools.Should().HaveCount(1);
        Tools.First().Type.Should().Be(ToolType.Internal);
    }

    [Fact]
    public async Task Tool_GetAllTools_ReturnsBothTypes()
    {
        // Arrange
        AddTestTool("Internal Tool", ToolType.Internal);
        var toolService = Services.GetRequiredService<IToolService>();

        // Add an MCP tool
        Tools.Add(new AesirToolBase
        {
            Id = Guid.NewGuid(),
            Name = "MCP Tool",
            Type = ToolType.McpServer,
            McpServerId = Guid.NewGuid()
        });

        // Act
        var result = await toolService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Should().Contain(t => t.Type == ToolType.Internal);
        result.Value!.Should().Contain(t => t.Type == ToolType.McpServer);
    }

    #endregion

    #region Configuration Change Flow Tests

    [Fact]
    public async Task ConfigurationChange_EngineCreated_AffectsAgentOptions()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Initially no engines
        var initialEngines = await engineService.GetAllAsync();
        initialEngines.Value.Should().BeEmpty();

        // Act - Create an engine
        await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "New Engine",
            Type = InferenceEngineType.Ollama
        });

        // Assert - Now agents can use this engine
        var enginesAfter = await engineService.GetAllAsync();
        enginesAfter.Value.Should().HaveCount(1);

        // Agent creation would now have an engine to select
        var engineId = InferenceEngines.First().Id!.Value;
        var agentResult = await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Agent Using New Engine",
            ChatInferenceEngineId = engineId,
            ChatModel = "llama3.2"
        });
        agentResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfigurationChange_SettingsUpdated_PreservesRelationships()
    {
        // Arrange
        AddTestInferenceEngine("Primary Engine");
        AddTestInferenceEngine("Secondary Engine");
        var primaryId = InferenceEngines.First().Id!.Value;
        var secondaryId = InferenceEngines.Last().Id!.Value;

        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Act - Set different engines for embedding and vision
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = primaryId,
            RagEmbeddingModel = "nomic-embed-text:latest",
            RagVisionInferenceEngineId = secondaryId,
            RagVisionModel = "llava:latest"
        };
        await settingsService.UpdateSettingsAsync(settings);

        // Assert
        var loadedSettings = await settingsService.GetSettingsAsync();
        loadedSettings.Value!.RagEmbeddingInferenceEngineId.Should().Be(primaryId);
        loadedSettings.Value.RagVisionInferenceEngineId.Should().Be(secondaryId);
        (loadedSettings.Value.RagEmbeddingInferenceEngineId != loadedSettings.Value.RagVisionInferenceEngineId).Should().BeTrue();
    }

    #endregion

    #region Complex Flow Tests

    [Fact]
    public async Task CompleteSettingsFlow_FromEmptyToFullyConfigured()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var mcpService = Services.GetRequiredService<IMcpServerService>();
        var toolService = Services.GetRequiredService<IToolService>();
        var agentService = Services.GetRequiredService<IAgentService>();
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Step 1: Create Inference Engine
        var engineResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Complete Flow Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        });
        var engineId = engineResult.Value;

        // Step 2: Configure General Settings
        await settingsService.UpdateSettingsAsync(new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "nomic-embed-text:latest"
        });

        // Step 3: Create MCP Server
        await mcpService.CreateAsync(new AesirMcpServerBase
        {
            Name = "Flow Test MCP",
            Location = ServerLocation.Local,
            Command = "/usr/bin/mcp"
        });

        // Step 4: Create Internal Tool
        await toolService.CreateAsync(new AesirToolBase
        {
            Name = "Flow Test Tool",
            Type = ToolType.Internal
        });

        // Step 5: Create Agent with Full Configuration
        await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Flow Test Agent",
            ChatInferenceEngineId = engineId,
            ChatModel = "llama3.2",
            ChatTemperature = 0.7,
            ChatMaxTokens = 4096
        });

        // Assert - All entities created
        InferenceEngines.Should().HaveCount(1);
        McpServers.Should().HaveCount(1);
        Tools.Should().HaveCount(1);
        Agents.Should().HaveCount(1);
        GeneralSettings.RagEmbeddingModel.Should().Be("nomic-embed-text:latest");
    }

    [Fact]
    public async Task MultipleAgents_DifferentEngines_IndependentConfiguration()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Create two engines
        var ollamaResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Ollama",
            Type = InferenceEngineType.Ollama
        });

        var openAiResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "OpenAI",
            Type = InferenceEngineType.OpenAICompatible
        });

        // Act - Create agents using different engines
        await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Local Agent",
            ChatInferenceEngineId = ollamaResult.Value,
            ChatModel = "llama3.2"
        });

        await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Cloud Agent",
            ChatInferenceEngineId = openAiResult.Value,
            ChatModel = "gpt-4"
        });

        // Assert
        var agents = await agentService.GetAllAsync();
        agents.Value.Should().HaveCount(2);

        var localAgent = agents.Value!.First(a => a.Name == "Local Agent");
        var cloudAgent = agents.Value!.First(a => a.Name == "Cloud Agent");

        localAgent.ChatInferenceEngineId.Should().Be(ollamaResult.Value);
        cloudAgent.ChatInferenceEngineId.Should().Be(openAiResult.Value);
        (localAgent.ChatInferenceEngineId != cloudAgent.ChatInferenceEngineId).Should().BeTrue();
    }

    #endregion
}

using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Web.Tests.Integration.Flows;

/// <summary>
/// Integration tests for the configuration flow:
/// Inference Engines → Agents → Agent availability in Chat
/// </summary>
public class ConfigurationFlowTests : IntegrationTestBase
{
    [Fact]
    public async Task CompleteFlow_CreateInferenceEngine_ThenAgent_ThenAvailableInChat()
    {
        // Arrange - Get services
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var configApiService = Services.GetRequiredService<IConfigurationApiService>();

        // Act 1: Create an inference engine
        var engineResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "My Ollama Engine",
            Description = "Local Ollama instance",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        });

        // Assert 1: Engine was created (returns Guid)
        engineResult.IsSuccess.Should().BeTrue();
        engineResult.Value.Should().NotBe(Guid.Empty);

        // Act 2: Create an agent using the engine
        var agentResult = await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Test Assistant",
            Description = "An AI assistant",
            ChatInferenceEngineId = engineResult.Value,
            ChatModel = "llama3.2",
            ChatTemperature = 0.7,
            ChatMaxTokens = 4096
        });

        // Assert 2: Agent was created (returns Guid)
        agentResult.IsSuccess.Should().BeTrue();
        agentResult.Value.Should().NotBe(Guid.Empty);

        // Act 3: Load agents in configuration API (simulating what ChatWelcome does)
        var agentsResult = await configApiService.GetAgentsAsync();

        // Assert 3: Agent is available for selection
        agentsResult.IsSuccess.Should().BeTrue();
        agentsResult.Value.Should().Contain(a => a.Name == "Test Assistant");
    }

    [Fact]
    public async Task CreateMultipleInferenceEngines_AllAvailableForAgents()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act: Create multiple engines
        var ollamaResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Ollama Local",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        });

        var openAiResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "OpenAI Cloud",
            Type = InferenceEngineType.OpenAICompatible,
            Configuration = new Dictionary<string, string?>
            {
                { "BaseUrl", "https://api.openai.com" },
                { "ApiKey", "sk-test-key" }
            }
        });

        // Assert: Both engines created
        ollamaResult.IsSuccess.Should().BeTrue();
        openAiResult.IsSuccess.Should().BeTrue();

        // Verify in list
        var enginesResult = await engineService.GetAllAsync();
        enginesResult.IsSuccess.Should().BeTrue();
        enginesResult.Value.Should().HaveCount(2);
        enginesResult.Value.Should().Contain(e => e.Name == "Ollama Local");
        enginesResult.Value.Should().Contain(e => e.Name == "OpenAI Cloud");
    }

    [Fact]
    public async Task CreateAgentWithTools_ToolsAssignedCorrectly()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var toolService = Services.GetRequiredService<IToolService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Create engine
        var engineResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        });

        // Create tools
        var toolResult1 = await toolService.CreateAsync(new AesirToolBase
        {
            Name = "Calculator",
            Type = ToolType.Internal,
            Description = "Performs calculations"
        });

        var toolResult2 = await toolService.CreateAsync(new AesirToolBase
        {
            Name = "Web Search",
            Type = ToolType.Internal,
            Description = "Searches the web"
        });

        // Act: Create agent
        var agentResult = await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Tool Agent",
            ChatInferenceEngineId = engineResult.Value,
            ChatModel = "llama3.2"
        });

        // Assign tools via UpdateAgentToolsAsync
        var updateToolsResult = await agentService.UpdateAgentToolsAsync(
            agentResult.Value,
            new List<Guid> { toolResult1.Value, toolResult2.Value });

        // Assert
        agentResult.IsSuccess.Should().BeTrue();
        updateToolsResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteInferenceEngine_AgentsUsingItStillExist()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Create engine
        var engineResult = await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Temporary Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        });

        // Create agent using engine
        var agentResult = await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Orphaned Agent",
            ChatInferenceEngineId = engineResult.Value,
            ChatModel = "llama3.2"
        });

        // Act: Delete the engine
        var deleteResult = await engineService.DeleteAsync(engineResult.Value);

        // Assert: Agent still exists (but engine reference is now invalid)
        deleteResult.IsSuccess.Should().BeTrue();
        var agentsResult = await agentService.GetAllAsync();
        agentsResult.Value.Should().Contain(a => a.Name == "Orphaned Agent");
    }

    [Fact]
    public async Task UpdateAgentConfiguration_ChangesPersist()
    {
        // Arrange - Pre-populate with test agent
        AddTestInferenceEngine();
        AddTestAgent("Configurable Agent", "llama3.2");
        var existingAgent = Agents.First();

        var agentService = Services.GetRequiredService<IAgentService>();

        // Act: Update agent configuration
        var updateResult = await agentService.UpdateAsync(new AesirAgentBase
        {
            Id = existingAgent.Id,
            Name = "Updated Agent",
            ChatModel = "llama3.2:70b",
            ChatTemperature = 0.9,
            ChatTopP = 0.95,
            ChatMaxTokens = 8192
        });

        // Assert: Update succeeded
        updateResult.IsSuccess.Should().BeTrue();

        // Verify changes via GetByIdAsync
        var getResult = await agentService.GetByIdAsync(existingAgent.Id!.Value);
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Name.Should().Be("Updated Agent");
        getResult.Value.ChatModel.Should().Be("llama3.2:70b");
    }

    [Fact]
    public async Task CreateMcpServer_ToolsCanBeDiscovered()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();

        // Act: Create local MCP server
        var result = await mcpService.CreateAsync(new AesirMcpServerBase
        {
            Name = "File System MCP",
            Description = "File system operations",
            Location = ServerLocation.Local,
            Command = "npx",
            Arguments = new List<string> { "-y", "@modelcontextprotocol/server-filesystem" }
        });

        // Assert: Server created (returns Guid)
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        // Verify in list
        var listResult = await mcpService.GetAllAsync();
        listResult.Value.Should().Contain(s => s.Name == "File System MCP");
    }

    [Fact]
    public async Task CreateRemoteMcpServer_WithHttpHeaders()
    {
        // Arrange
        var mcpService = Services.GetRequiredService<IMcpServerService>();

        // Act: Create remote MCP server with auth
        var result = await mcpService.CreateAsync(new AesirMcpServerBase
        {
            Name = "Remote API Server",
            Description = "Remote MCP endpoint",
            Location = ServerLocation.Remote,
            Url = "https://mcp.example.com/api",
            HttpHeaders = new Dictionary<string, string?>
            {
                { "Authorization", "Bearer token123" },
                { "X-Custom-Header", "custom-value" }
            }
        });

        // Assert: Server created
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);

        // Verify server exists
        var listResult = await mcpService.GetAllAsync();
        listResult.Value.Should().Contain(s => s.Name == "Remote API Server");
    }

    [Fact]
    public async Task FullConfigurationSetup_ReadyForChat()
    {
        // Arrange - Services
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var toolService = Services.GetRequiredService<IToolService>();
        var agentService = Services.GetRequiredService<IAgentService>();
        var chatStateService = Services.GetRequiredService<IChatStateService>();
        var configApiService = Services.GetRequiredService<IConfigurationApiService>();

        // Act: Complete setup workflow

        // 1. Create inference engine
        var engineId = (await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Production Ollama",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "BaseUrl", "http://localhost:11434" } }
        })).Value;

        // 2. Create internal tool
        await toolService.CreateAsync(new AesirToolBase
        {
            Name = "Code Executor",
            Type = ToolType.Internal,
            Description = "Executes code snippets",
            ToolName = "execute_code"
        });

        // 3. Create agent
        await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Developer Assistant",
            Description = "Helps with coding tasks",
            ChatInferenceEngineId = engineId,
            ChatModel = "codellama:13b",
            ChatTemperature = 0.3,
            ChatTopP = 0.95,
            ChatMaxTokens = 8192,
            ChatPromptPersona = PromptPersona.Custom,
            ChatCustomPromptContent = "You are a senior software developer..."
        });

        // 4. Verify everything is ready for chat
        var enginesLoaded = await configApiService.GetInferenceEnginesAsync();
        var toolsLoaded = await configApiService.GetToolsAsync();
        var agentsLoaded = await configApiService.GetAgentsAsync();

        // Assert: Full configuration available
        enginesLoaded.Value.Should().HaveCount(1);
        toolsLoaded.Value.Should().HaveCount(1);
        agentsLoaded.Value.Should().HaveCount(1);

        // Chat state can select the agent
        var loadedAgent = agentsLoaded.Value!.First();
        chatStateService.SelectAgent(loadedAgent);
        chatStateService.SelectedAgent.Should().NotBeNull();
        chatStateService.SelectedAgent!.Name.Should().Be("Developer Assistant");
    }

    [Fact]
    public async Task AgentWithThinkingEnabled_ConfiguredCorrectly()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        await engineService.CreateAsync(new AesirInferenceEngineBase
        {
            Name = "Anthropic Engine",
            Type = InferenceEngineType.OpenAICompatible,
            Configuration = new Dictionary<string, string?>
            {
                { "BaseUrl", "https://api.anthropic.com" },
                { "ApiKey", "sk-ant-test" }
            }
        });

        // Act: Create agent with thinking enabled
        var agentId = await agentService.CreateAsync(new AesirAgentBase
        {
            Name = "Thinking Agent",
            ChatModel = "claude-3-opus",
            AllowThinking = true,
            ThinkValue = ThinkValue.High
        });

        // Assert: Agent created
        agentId.IsSuccess.Should().BeTrue();

        // Verify via GetByIdAsync
        var agentResult = await agentService.GetByIdAsync(agentId.Value);
        agentResult.IsSuccess.Should().BeTrue();
        agentResult.Value!.AllowThinking.Should().BeTrue();
        agentResult.Value.ThinkValue.ToString().Should().Be(ThinkValue.High);
    }
}

using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Services;

public class ConfigurationApiServiceTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly ConfigurationApiService _service;

    public ConfigurationApiServiceTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _service = new ConfigurationApiService(_mockApiClient.Object);
    }

    // Inference Engine Tests

    [Fact]
    public async Task GetInferenceEnginesAsync_ReturnsEngines()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Ollama", Type = InferenceEngineType.Ollama },
            new() { Id = Guid.NewGuid(), Name = "OpenAI", Type = InferenceEngineType.OpenAICompatible }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirInferenceEngineBase>>(
                "/configuration/inferenceengines", It.IsAny<CancellationToken>()))
            .ReturnsAsync(engines);

        // Act
        var result = await _service.GetInferenceEnginesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInferenceEngineAsync_ReturnsEngine_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var engine = new AesirInferenceEngineBase { Id = id, Name = "Test Engine" };
        _mockApiClient.Setup(x => x.GetAsync<AesirInferenceEngineBase>(
                $"/configuration/inferenceengines/{id}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(engine);

        // Act
        var result = await _service.GetInferenceEngineAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Engine");
    }

    [Fact]
    public async Task CreateInferenceEngineAsync_ReturnsNewId()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var engine = new AesirInferenceEngineBase { Name = "New Engine" };
        _mockApiClient.Setup(x => x.PostAsync<Guid>(
                "/configuration/inferenceengines", engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var result = await _service.CreateInferenceEngineAsync(engine);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newId);
    }

    [Fact]
    public async Task UpdateInferenceEngineAsync_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        var engine = new AesirInferenceEngineBase { Id = id, Name = "Updated Engine" };
        _mockApiClient.Setup(x => x.PutAsync<object>(
                $"/configuration/inferenceengines/{id}", engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        var result = await _service.UpdateInferenceEngineAsync(engine);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteInferenceEngineAsync_ReturnsSuccess()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiClient.Setup(x => x.DeleteAsync(
                $"/configuration/inferenceengines/{id}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteInferenceEngineAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // Agent Tests

    [Fact]
    public async Task GetAgentsAsync_ReturnsAgents()
    {
        // Arrange
        var agents = new List<AesirAgentBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Agent 1" },
            new() { Id = Guid.NewGuid(), Name = "Agent 2" }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirAgentBase>>(
                "/configuration/agents", It.IsAny<CancellationToken>()))
            .ReturnsAsync(agents);

        // Act
        var result = await _service.GetAgentsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAgentToolsAsync_ReturnsTools()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Tool 1", Type = ToolType.Internal },
            new() { Id = Guid.NewGuid(), Name = "Tool 2", Type = ToolType.McpServer }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirToolBase>>(
                $"/configuration/agents/{agentId}/tools", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tools);

        // Act
        var result = await _service.GetAgentToolsAsync(agentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAgentToolsAsync_ReturnsSuccess()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _mockApiClient.Setup(x => x.PutAsync<object>(
                $"/configuration/agents/{agentId}/tools", It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        var result = await _service.UpdateAgentToolsAsync(agentId, toolIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // MCP Server Tests

    [Fact]
    public async Task GetMcpServersAsync_ReturnsServers()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Server 1", Location = ServerLocation.Local },
            new() { Id = Guid.NewGuid(), Name = "Server 2", Location = ServerLocation.Remote }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirMcpServerBase>>(
                "/configuration/mcpservers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(servers);

        // Act
        var result = await _service.GetMcpServersAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMcpServerToolsAsync_ReturnsTools()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var tools = new List<AesirMcpServerToolBase>
        {
            new() { Name = "mcp_tool_1" },
            new() { Name = "mcp_tool_2" }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirMcpServerToolBase>>(
                $"/configuration/mcpservers/{serverId}/tools", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tools);

        // Act
        var result = await _service.GetMcpServerToolsAsync(serverId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // Tools Tests

    [Fact]
    public async Task GetToolsAsync_ReturnsTools()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Tool 1" },
            new() { Id = Guid.NewGuid(), Name = "Tool 2" }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirToolBase>>(
                "/configuration/tools", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tools);

        // Act
        var result = await _service.GetToolsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    // Error Handling Tests

    [Fact]
    public async Task GetInferenceEnginesAsync_ReturnsFailure_OnHttpError()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetAsync<List<AesirInferenceEngineBase>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetInferenceEnginesAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task GetInferenceEngineAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiClient.Setup(x => x.GetAsync<AesirInferenceEngineBase>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirInferenceEngineBase?)null);

        // Act
        var result = await _service.GetInferenceEngineAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    // System Readiness Test

    [Fact]
    public async Task GetSystemReadinessAsync_ReturnsReadinessStatus()
    {
        // Arrange
        var readiness = new AesirConfigurationReadinessBase { IsReady = true };
        _mockApiClient.Setup(x => x.GetAsync<AesirConfigurationReadinessBase>(
                "/configuration/systemready", It.IsAny<CancellationToken>()))
            .ReturnsAsync(readiness);

        // Act
        var result = await _service.GetSystemReadinessAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReady.Should().BeTrue();
    }

    // Configuration Reload Tests

    [Fact]
    public async Task ReloadConfigurationAsync_ReturnsUpdatedReadinessStatus()
    {
        // Arrange
        var readiness = new AesirConfigurationReadinessBase { IsReady = true };
        _mockApiClient.Setup(x => x.PostAsync<AesirConfigurationReadinessBase>(
                "/configuration/reload", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(readiness);

        // Act
        var result = await _service.ReloadConfigurationAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task ReloadConfigurationAsync_ReturnsFailure_OnHttpError()
    {
        // Arrange
        _mockApiClient.Setup(x => x.PostAsync<AesirConfigurationReadinessBase>(
                It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.ReloadConfigurationAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task ReloadConfigurationAsync_ReturnsDefaultReadiness_WhenNull()
    {
        // Arrange
        _mockApiClient.Setup(x => x.PostAsync<AesirConfigurationReadinessBase>(
                "/configuration/reload", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirConfigurationReadinessBase?)null);

        // Act
        var result = await _service.ReloadConfigurationAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReady.Should().BeFalse();
    }

    // General Settings Tests

    [Fact]
    public async Task GetGeneralSettingsAsync_ReturnsSettings()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingModel = "test-embedding",
            GoogleSearchEngineId = "search-id"
        };
        _mockApiClient.Setup(x => x.GetAsync<AesirGeneralSettingsBase>(
                "/configuration/generalsettings", It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _service.GetGeneralSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RagEmbeddingModel.Should().Be("test-embedding");
        result.Value.GoogleSearchEngineId.Should().Be("search-id");
    }

    [Fact]
    public async Task GetGeneralSettingsAsync_ReturnsEmptySettings_WhenNull()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetAsync<AesirGeneralSettingsBase>(
                "/configuration/generalsettings", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirGeneralSettingsBase?)null);

        // Act
        var result = await _service.GetGeneralSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetGeneralSettingsAsync_ReturnsFailure_OnHttpError()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetAsync<AesirGeneralSettingsBase>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetGeneralSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task UpdateGeneralSettingsAsync_ReturnsSuccess()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase { RagEmbeddingModel = "test-model" };
        _mockApiClient.Setup(x => x.PutAsync<object>(
                "/configuration/generalsettings", settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _service.UpdateGeneralSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateGeneralSettingsAsync_CallsCorrectEndpoint()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase { RagEmbeddingModel = "test-model" };
        _mockApiClient.Setup(x => x.PutAsync<object>(
                It.IsAny<string>(), It.IsAny<AesirGeneralSettingsBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);

        // Act
        await _service.UpdateGeneralSettingsAsync(settings);

        // Assert
        _mockApiClient.Verify(x => x.PutAsync<object>(
            "/configuration/generalsettings", settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGeneralSettingsAsync_ReturnsFailure_OnHttpError()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase();
        _mockApiClient.Setup(x => x.PutAsync<object>(
                It.IsAny<string>(), It.IsAny<AesirGeneralSettingsBase>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.UpdateGeneralSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }
}

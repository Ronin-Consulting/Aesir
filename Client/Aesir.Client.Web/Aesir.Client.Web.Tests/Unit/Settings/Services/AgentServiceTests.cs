using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Web.Tests.Unit.Settings.Services;

public class AgentServiceTests
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly AgentService _service;

    public AgentServiceTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _service = new AgentService(_mockApiService.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAgents_WhenApiSucceeds()
    {
        // Arrange
        var agents = new List<AesirAgentBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Agent 1", ChatModel = "gpt-4" },
            new() { Id = Guid.NewGuid(), Name = "Agent 2", ChatModel = "claude-3" }
        };
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Failure("Network error"));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAgent_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var agent = new AesirAgentBase { Id = id, Name = "Test Agent", ChatModel = "gpt-4" };
        _mockApiService.Setup(x => x.GetAgentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirAgentBase>.Success(agent));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Agent");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetAgentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirAgentBase>.Failure("Agent not found"));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateAsync_ReturnsNewId_WhenSuccessful()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var agent = new AesirAgentBase { Name = "New Agent", ChatModel = "gpt-4" };
        _mockApiService.Setup(x => x.CreateAgentAsync(agent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(newId));

        // Act
        var result = await _service.CreateAsync(agent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var agent = new AesirAgentBase { Name = "New Agent" };
        _mockApiService.Setup(x => x.CreateAgentAsync(agent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Failure("Validation error"));

        // Act
        var result = await _service.CreateAsync(agent);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Updated Agent" };
        _mockApiService.Setup(x => x.UpdateAgentAsync(agent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateAsync(agent);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Updated Agent" };
        _mockApiService.Setup(x => x.UpdateAgentAsync(agent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateAsync(agent);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Update failed");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteAgentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteAgentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Delete failed"));

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Delete failed");
    }

    [Fact]
    public async Task GetAgentToolsAsync_ReturnsTools_WhenSuccessful()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Tool 1" },
            new() { Id = Guid.NewGuid(), Name = "Tool 2" }
        };
        _mockApiService.Setup(x => x.GetAgentToolsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));

        // Act
        var result = await _service.GetAgentToolsAsync(agentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAgentToolsAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetAgentToolsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Failure("Agent not found"));

        // Act
        var result = await _service.GetAgentToolsAsync(agentId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateAgentToolsAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _mockApiService.Setup(x => x.UpdateAgentToolsAsync(agentId, toolIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateAgentToolsAsync(agentId, toolIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAgentToolsAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var toolIds = new List<Guid> { Guid.NewGuid() };
        _mockApiService.Setup(x => x.UpdateAgentToolsAsync(agentId, toolIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Tool assignment failed"));

        // Act
        var result = await _service.UpdateAgentToolsAsync(agentId, toolIds);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Tool assignment failed");
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var agents = new List<AesirAgentBase>();
        _mockApiService.Setup(x => x.GetAgentsAsync(cts.Token))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));

        // Act
        await _service.GetAllAsync(cts.Token);

        // Assert
        _mockApiService.Verify(x => x.GetAgentsAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAllParameters_PassesCorrectly()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var engineId = Guid.NewGuid();
        var agent = new AesirAgentBase
        {
            Name = "Full Agent",
            Description = "A fully configured agent",
            ChatInferenceEngineId = engineId,
            ChatModel = "gpt-4-turbo",
            ChatTemperature = 0.7,
            ChatTopP = 0.9,
            ChatMaxTokens = 4096,
            ChatPromptPersona = PromptPersona.Business,
            AllowThinking = true,
            ThinkValue = "high"
        };
        _mockApiService.Setup(x => x.CreateAgentAsync(agent, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(newId));

        // Act
        var result = await _service.CreateAsync(agent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockApiService.Verify(x => x.CreateAgentAsync(
            It.Is<AesirAgentBase>(a =>
                a.Name == "Full Agent" &&
                a.ChatModel == "gpt-4-turbo" &&
                a.ChatTemperature == 0.7 &&
                a.ChatPromptPersona == PromptPersona.Business),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

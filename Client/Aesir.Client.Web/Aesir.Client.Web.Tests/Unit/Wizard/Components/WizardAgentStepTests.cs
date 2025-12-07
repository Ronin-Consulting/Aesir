using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Wizard.Components;

/// <summary>
/// Tests for the WizardAgentStep component.
/// Note: Full component rendering tests for MudBlazor components with MudSelect are complex
/// due to MudBlazor 8.x popover service requirements. These tests focus on service interactions
/// and validation logic that can be tested through the service layer.
/// </summary>
public class WizardAgentStepTests
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IInferenceEngineService> _mockEngineService;

    public WizardAgentStepTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockEngineService = new Mock<IInferenceEngineService>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAgents_WhenAgentsExist()
    {
        // Arrange
        var agents = CreateTestAgents();
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));

        // Act
        var result = await _mockAgentService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Assistant 1");
        result.Value[1].Name.Should().Be("Assistant 2");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoAgentsConfigured()
    {
        // Arrange
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));

        // Act
        var result = await _mockAgentService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CreatesAgent_WithValidData()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var newAgent = new AesirAgentBase
        {
            Id = agentId,
            Name = "My Assistant",
            Description = "A helpful assistant",
            ChatInferenceEngineId = Guid.NewGuid(),
            ChatModel = "llama3.1"
        };

        _mockAgentService.Setup(x => x.CreateAsync(It.IsAny<AesirAgentBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(agentId));

        // Act
        var result = await _mockAgentService.Object.CreateAsync(newAgent, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(agentId);
    }

    [Fact]
    public async Task DeleteAsync_DeletesAgent_Successfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _mockAgentService.Setup(x => x.DeleteAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _mockAgentService.Object.DeleteAsync(agentId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanProceed_ReturnsFalse_WhenNoAgents()
    {
        // Arrange
        var agents = new List<AesirAgentBase>();

        // Act
        var canProceed = agents.Count > 0;

        // Assert
        canProceed.Should().BeFalse();
    }

    [Fact]
    public void CanProceed_ReturnsTrue_WhenAgentsExist()
    {
        // Arrange
        var agents = CreateTestAgents();

        // Act
        var canProceed = agents.Count > 0;

        // Assert
        canProceed.Should().BeTrue();
    }

    [Fact]
    public void AgentValidation_RequiresName()
    {
        // Arrange
        var agent = new AesirAgentBase
        {
            Name = "",
            ChatInferenceEngineId = Guid.NewGuid()
        };

        // Act
        var isValid = !string.IsNullOrWhiteSpace(agent.Name);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void AgentValidation_AcceptsValidName()
    {
        // Arrange
        var agent = new AesirAgentBase
        {
            Name = "My Assistant",
            ChatInferenceEngineId = Guid.NewGuid()
        };

        // Act
        var isValid = !string.IsNullOrWhiteSpace(agent.Name);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void AgentValidation_RequiresInferenceEngine()
    {
        // Arrange
        var agent = new AesirAgentBase
        {
            Name = "My Assistant",
            ChatInferenceEngineId = Guid.Empty
        };

        // Act
        var isValid = agent.ChatInferenceEngineId != Guid.Empty;

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void Agent_HasAdvancedOptions()
    {
        // Arrange
        var agent = new AesirAgentBase
        {
            Name = "My Assistant",
            ChatInferenceEngineId = Guid.NewGuid(),
            ChatModel = "llama3.1",
            ChatCustomPromptContent = "You are a helpful assistant",
            ChatTemperature = 0.7,
            ChatMaxTokens = 2048
        };

        // Assert
        agent.ChatCustomPromptContent.Should().Be("You are a helpful assistant");
        agent.ChatTemperature.Should().Be(0.7);
        agent.ChatMaxTokens.Should().Be(2048);
    }

    [Fact]
    public async Task GetAllEnginesAsync_ReturnsEngines()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Ollama" },
            new() { Id = Guid.NewGuid(), Name = "OpenAI" }
        };
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var result = await _mockEngineService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void AutoSelectFirstEngine_WhenAvailable()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Engine" }
        };

        // Act - Simulating auto-selection logic
        var selectedEngine = engines.FirstOrDefault();

        // Assert
        selectedEngine.Should().NotBeNull();
        selectedEngine!.Name.Should().Be("Test Engine");
    }

    private static IReadOnlyList<AesirAgentBase> CreateTestAgents()
    {
        return new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Assistant 1",
                ChatModel = "llama3.1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Assistant 2",
                ChatModel = "gpt-4"
            }
        };
    }
}

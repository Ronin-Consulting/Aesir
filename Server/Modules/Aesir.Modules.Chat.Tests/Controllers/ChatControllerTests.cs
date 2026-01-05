using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Chat.Controllers;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Chat.Tests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly Mock<ILogger<ChatController>> _loggerMock;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _configurationServiceMock = new Mock<IConfigurationService>();
        _loggerMock = new Mock<ILogger<ChatController>>();
        _controller = new ChatController(_serviceProviderMock.Object, _configurationServiceMock.Object, _loggerMock.Object);
    }

    #region ApplyInferenceEngineMasterSwitchAsync Tests

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenThinkingNotRequested_ReturnsOriginalValues()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid());

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: false, requestThinkValue: null);

        // Assert
        enableThinking.Should().BeFalse();
        thinkValue.Should().BeNull();
        _configurationServiceMock.Verify(x => x.GetInferenceEngineAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenThinkingNull_ReturnsOriginalValues()
    {
        // Arrange
        var agent = CreateAgent(Guid.NewGuid());

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: null, requestThinkValue: null);

        // Assert
        enableThinking.Should().BeNull();
        thinkValue.Should().BeNull();
        _configurationServiceMock.Verify(x => x.GetInferenceEngineAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenNonOllamaEngine_ReturnsOriginalValues()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = CreateInferenceEngine(engineId, InferenceEngineType.OpenAICompatible);

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        var requestThinkValue = new ThinkValue("high");

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: requestThinkValue);

        // Assert
        enableThinking.Should().BeTrue();
        thinkValue.Should().Be(requestThinkValue);
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenOllamaEngineWithThinkingEnabled_ReturnsOriginalValues()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = CreateInferenceEngine(engineId, InferenceEngineType.Ollama, enableThinking: true);

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        var requestThinkValue = new ThinkValue("medium");

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: requestThinkValue);

        // Assert
        enableThinking.Should().BeTrue();
        thinkValue.Should().Be(requestThinkValue);
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenOllamaEngineWithThinkingDisabled_DisablesThinking()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = CreateInferenceEngine(engineId, InferenceEngineType.Ollama, enableThinking: false);

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        var requestThinkValue = new ThinkValue("high");

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: requestThinkValue);

        // Assert
        enableThinking.Should().BeFalse();
        thinkValue.Should().BeNull();
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenOllamaEngineWithNullConfiguration_DisablesThinking()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = new AesirInferenceEngine
        {
            Id = engineId,
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = null
        };

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: new ThinkValue(true));

        // Assert
        enableThinking.Should().BeFalse();
        thinkValue.Should().BeNull();
    }

    [Fact]
    public async Task ApplyInferenceEngineMasterSwitch_WhenOllamaEngineWithMissingKey_DisablesThinking()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = new AesirInferenceEngine
        {
            Id = engineId,
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?>
            {
                ["SomeOtherKey"] = "value"
            }
        };

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: new ThinkValue("low"));

        // Assert
        enableThinking.Should().BeFalse();
        thinkValue.Should().BeNull();
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    public async Task ApplyInferenceEngineMasterSwitch_ParsesEnableChatModelThinkingCaseInsensitive(
        string configValue, bool expectedEnabled)
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = new AesirInferenceEngine
        {
            Id = engineId,
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?>
            {
                ["enable_chat_model_thinking"] = configValue
            }
        };

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        var requestThinkValue = new ThinkValue("high");

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: requestThinkValue);

        // Assert
        if (expectedEnabled)
        {
            enableThinking.Should().BeTrue();
            thinkValue.Should().Be(requestThinkValue);
        }
        else
        {
            enableThinking.Should().BeFalse();
            thinkValue.Should().BeNull();
        }
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("yes")]
    [InlineData("no")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ApplyInferenceEngineMasterSwitch_InvalidConfigValue_DisablesThinking(string? configValue)
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agent = CreateAgent(engineId);
        var engine = new AesirInferenceEngine
        {
            Id = engineId,
            Name = "Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?>
            {
                ["enable_chat_model_thinking"] = configValue
            }
        };

        _configurationServiceMock
            .Setup(x => x.GetInferenceEngineAsync(engineId))
            .ReturnsAsync(engine);

        // Act
        var (enableThinking, thinkValue) = await _controller.ApplyInferenceEngineMasterSwitchAsync(
            agent, requestEnableThinking: true, requestThinkValue: new ThinkValue(true));

        // Assert
        enableThinking.Should().BeFalse();
        thinkValue.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static AesirAgent CreateAgent(Guid inferenceEngineId)
    {
        return new AesirAgent
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatInferenceEngineId = inferenceEngineId,
            ChatModel = "test-model"
        };
    }

    private static AesirInferenceEngine CreateInferenceEngine(
        Guid id,
        InferenceEngineType type,
        bool? enableThinking = null)
    {
        var config = new Dictionary<string, string?>();

        if (enableThinking.HasValue)
        {
            config["enable_chat_model_thinking"] = enableThinking.Value.ToString().ToLower();
        }

        return new AesirInferenceEngine
        {
            Id = id,
            Name = "Test Engine",
            Type = type,
            Configuration = config
        };
    }

    #endregion
}

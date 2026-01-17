using Aesir.Common.Models;
using Aesir.Infrastructure.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ChatRequestBuilderTests
{
    private readonly Mock<IConfigurationService> _configurationServiceMock;
    private readonly Mock<ILogger<ChatRequestBuilder>> _loggerMock;
    private readonly ChatRequestBuilder _builder;

    public ChatRequestBuilderTests()
    {
        _configurationServiceMock = new Mock<IConfigurationService>();
        _loggerMock = new Mock<ILogger<ChatRequestBuilder>>();

        _builder = new ChatRequestBuilder(
            _configurationServiceMock.Object,
            _loggerMock.Object);
    }

    #region ShouldPersistChatSession Tests

    [Fact]
    public async Task BuildAsync_SetsShouldPersistChatSession_ToFalse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions();

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.ShouldPersistChatSession.Should().BeFalse(
            "Research agent requests should not persist chat sessions");
    }

    [Fact]
    public async Task BuildAsync_WithTools_StillSetsShouldPersistChatSession_ToFalse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions() with { IncludeTools = true };

        SetupConfigurationService(agent.BaseAgentId, includeTools: true);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.ShouldPersistChatSession.Should().BeFalse(
            "Research agent requests with tools should not persist chat sessions");
    }

    [Fact]
    public async Task BuildAsync_WithCustomPersona_StillSetsShouldPersistChatSession_ToFalse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions() with
        {
            UseCustomPersona = true,
            CustomPersonaContent = "Custom persona content"
        };

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.ShouldPersistChatSession.Should().BeFalse(
            "Research agent requests with custom persona should not persist chat sessions");
    }

    [Fact]
    public async Task BuildAsync_WhenConfigurationFails_StillSetsShouldPersistChatSession_ToFalse()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions();

        // Setup configuration service to throw
        _configurationServiceMock
            .Setup(c => c.GetAgentAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new Exception("Configuration error"));

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.ShouldPersistChatSession.Should().BeFalse(
            "Research agent requests should not persist chat sessions even when configuration fails");
    }

    #endregion

    #region Other BuildAsync Tests

    [Fact]
    public async Task BuildAsync_SetsCorrectConversation()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions();
        var systemPrompt = "You are a research assistant.";
        var userPrompt = "Research this topic.";

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            systemPrompt,
            userPrompt,
            options);

        // Assert
        result.Conversation.Should().NotBeNull();
        result.Conversation.Messages.Should().HaveCount(2);
        result.Conversation.Messages[0].Role.Should().Be("system");
        result.Conversation.Messages[0].Content.Should().Be(systemPrompt);
        result.Conversation.Messages[1].Role.Should().Be("user");
        result.Conversation.Messages[1].Content.Should().Be(userPrompt);
    }

    [Fact]
    public async Task BuildAsync_SetsModelFromAgent()
    {
        // Arrange
        var agent = CreateTestAgent();
        agent.Model = "gpt-4-turbo";
        var options = CreateTestOptions();

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.Model.Should().Be("gpt-4-turbo");
    }

    [Fact]
    public async Task BuildAsync_UsesDefaultModel_WhenAgentModelIsNull()
    {
        // Arrange
        var agent = CreateTestAgent();
        agent.Model = null;
        var options = CreateTestOptions();

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.Model.Should().Be("gpt-4"); // Default model
    }

    [Fact]
    public async Task BuildAsync_SetsUserAndTitle()
    {
        // Arrange
        var agent = CreateTestAgent();
        var options = CreateTestOptions() with
        {
            User = "test-user-123",
            Title = "Research Session Title"
        };

        SetupConfigurationService(agent.BaseAgentId);

        // Act
        var result = await _builder.BuildAsync(
            agent,
            "System prompt",
            "User prompt",
            options);

        // Assert
        result.User.Should().Be("test-user-123");
        result.Title.Should().Be("Research Session Title");
    }

    #endregion

    #region Helper Methods

    private static ResearchAgent CreateTestAgent()
    {
        return new ResearchAgent
        {
            TeamMemberId = Guid.NewGuid(),
            Role = ResearchRole.DeepDiver,
            RoleName = "Deep Diver",
            BaseAgentId = Guid.NewGuid(),
            InferenceEngineId = Guid.NewGuid(),
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 4096,
            Persona = "You are a thorough researcher."
        };
    }

    private static ChatRequestOptions CreateTestOptions()
    {
        return new ChatRequestOptions
        {
            User = "test-user",
            Title = "Test Title",
            IncludeTools = false,
            UseCustomPersona = false
        };
    }

    private void SetupConfigurationService(Guid baseAgentId, bool includeTools = false)
    {
        var baseAgent = new AesirAgent
        {
            Id = baseAgentId,
            Name = "Test Agent",
            AllowThinking = true,
            ThinkValue = ThinkValue.Medium
        };

        _configurationServiceMock
            .Setup(c => c.GetAgentAsync(baseAgentId))
            .ReturnsAsync(baseAgent);

        if (includeTools)
        {
            _configurationServiceMock
                .Setup(c => c.GetToolsUsedByAgentAsync(baseAgentId))
                .ReturnsAsync(new List<AesirTool>
                {
                    new() { ToolName = "WebSearch" }
                });

            _configurationServiceMock
                .Setup(c => c.GetMcpServersAsync())
                .ReturnsAsync(new List<AesirMcpServer>());
        }
        else
        {
            _configurationServiceMock
                .Setup(c => c.GetToolsUsedByAgentAsync(baseAgentId))
                .ReturnsAsync(new List<AesirTool>());
        }
    }

    #endregion
}

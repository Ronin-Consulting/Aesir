using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ChairmanPlanningServiceChatSessionIdTests
{
    private readonly Mock<ILogger<ChairmanPlanningService>> _loggerMock;
    private readonly Mock<IChatServiceResolver> _chatServiceResolverMock;
    private readonly Mock<IChatRequestBuilder> _chatRequestBuilderMock;
    private readonly Mock<IResearchProgressBroadcaster> _progressBroadcasterMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly ChairmanPlanningService _service;

    public ChairmanPlanningServiceChatSessionIdTests()
    {
        _loggerMock = new Mock<ILogger<ChairmanPlanningService>>();
        _chatServiceResolverMock = new Mock<IChatServiceResolver>();
        _chatRequestBuilderMock = new Mock<IChatRequestBuilder>();
        _progressBroadcasterMock = new Mock<IResearchProgressBroadcaster>();
        _chatServiceMock = new Mock<IChatService>();

        _service = new ChairmanPlanningService(
            _loggerMock.Object,
            _chatServiceResolverMock.Object,
            _chatRequestBuilderMock.Object,
            _progressBroadcasterMock.Object);
    }

    [Fact]
    public async Task CreateUnifiedPlanAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Test research query",
            ChatSessionId = chatSessionId
        };
        var chairman = CreateChairmanAgent();
        var teamAgents = new List<ResearchAgent> { CreateTeamAgent(ResearchRole.DeepDiver, "Deep Diver") };

        ChatRequestOptions? capturedOptions = null;

        _chatRequestBuilderMock
            .Setup(b => b.BuildAsync(
                It.IsAny<ResearchAgent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatRequestOptions>()))
            .Callback<ResearchAgent, string, string, ChatRequestOptions>(
                (_, _, _, opts) => capturedOptions = opts)
            .ReturnsAsync(CreateValidRequest());

        _chatServiceResolverMock
            .Setup(r => r.GetRequiredChatService(It.IsAny<Guid?>(), It.IsAny<string>()))
            .Returns(_chatServiceMock.Object);

        _chatServiceMock
            .Setup(s => s.ChatCompletionsAsync(It.IsAny<AesirChatRequestBase>()))
            .ReturnsAsync(CreateValidResult("### Deep Diver\nResearch sub-plan content"));

        // Act
        await _service.CreateUnifiedPlanAsync(
            session, chairman, teamAgents, "Test refined query");

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ChatSessionId.Should().Be(chatSessionId,
            "ChatSessionId from ResearchSession should be propagated to ChatRequestOptions for observability");
    }

    #region Helper Methods

    private static ResearchAgent CreateChairmanAgent()
    {
        return new ResearchAgent
        {
            TeamMemberId = Guid.NewGuid(),
            Role = ResearchRole.Chairman,
            RoleName = "Chairman",
            BaseAgentId = Guid.NewGuid(),
            InferenceEngineId = Guid.NewGuid(),
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 4096,
            Persona = "You are a research chairman."
        };
    }

    private static ResearchAgent CreateTeamAgent(ResearchRole role, string roleName)
    {
        return new ResearchAgent
        {
            TeamMemberId = Guid.NewGuid(),
            Role = role,
            RoleName = roleName,
            BaseAgentId = Guid.NewGuid(),
            InferenceEngineId = Guid.NewGuid(),
            Model = "gpt-4",
            Temperature = 0.7,
            MaxTokens = 4096,
            Persona = $"You are a {roleName}."
        };
    }

    private static AesirChatRequestBase CreateValidRequest()
    {
        return new AesirChatRequestBase
        {
            Model = "gpt-4",
            User = "test",
            Title = "Test",
            Conversation = new AesirConversation
            {
                Messages = [
                    new AesirChatMessage { Role = "system", Content = "System" },
                    new AesirChatMessage { Role = "user", Content = "User" }
                ]
            }
        };
    }

    private static AesirChatResult CreateValidResult(string content)
    {
        return new AesirChatResult
        {
            AesirConversation = new AesirConversation
            {
                Messages = [
                    new AesirChatMessage { Role = "assistant", Content = content }
                ]
            }
        };
    }

    #endregion
}

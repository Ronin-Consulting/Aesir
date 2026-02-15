using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ClarificationServiceChatSessionIdTests
{
    private readonly Mock<ILogger<ClarificationService>> _loggerMock;
    private readonly Mock<IChatServiceResolver> _chatServiceResolverMock;
    private readonly Mock<IChatRequestBuilder> _chatRequestBuilderMock;
    private readonly Mock<IResearchProgressBroadcaster> _progressBroadcasterMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly ClarificationService _service;

    public ClarificationServiceChatSessionIdTests()
    {
        _loggerMock = new Mock<ILogger<ClarificationService>>();
        _chatServiceResolverMock = new Mock<IChatServiceResolver>();
        _chatRequestBuilderMock = new Mock<IChatRequestBuilder>();
        _progressBroadcasterMock = new Mock<IResearchProgressBroadcaster>();
        _chatServiceMock = new Mock<IChatService>();

        _service = new ClarificationService(
            _loggerMock.Object,
            _chatServiceResolverMock.Object,
            _chatRequestBuilderMock.Object,
            _progressBroadcasterMock.Object);
    }

    [Fact]
    public async Task GenerateClarificationQuestionsAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var chairman = CreateChairmanAgent();

        ChatRequestOptions? capturedOptions = null;

        SetupChatRequestBuilder(opts => capturedOptions = opts);
        SetupChatServiceWithResponse("```json\n[\"Question 1?\"]\n```");

        _chatServiceResolverMock
            .Setup(r => r.GetChatService(It.IsAny<Guid?>()))
            .Returns(_chatServiceMock.Object);

        // Act
        await _service.GenerateClarificationQuestionsAsync(
            sessionId, "Test query", chairman, chatSessionId);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ChatSessionId.Should().Be(chatSessionId,
            "ChatSessionId should be propagated through to ChatRequestOptions for observability");
    }

    [Fact]
    public async Task RefineQueryAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var chairman = CreateChairmanAgent();
        var questions = new List<string> { "What scope?" };
        var answers = new Dictionary<string, string> { { "What scope?", "Global" } };

        ChatRequestOptions? capturedOptions = null;

        SetupChatRequestBuilder(opts => capturedOptions = opts);
        SetupChatServiceWithResponse("Refined query about global scope research");

        _chatServiceResolverMock
            .Setup(r => r.GetChatService(It.IsAny<Guid?>()))
            .Returns(_chatServiceMock.Object);

        // Act
        await _service.RefineQueryAsync(
            sessionId, "Test query", questions, answers, chairman, chatSessionId);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ChatSessionId.Should().Be(chatSessionId,
            "ChatSessionId should be propagated through to ChatRequestOptions for observability");
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
            Temperature = 0.3,
            MaxTokens = 4096,
            Persona = "You are a research chairman.",
            ClarificationPrompt = "Analyze the query for clarification needs."
        };
    }

    private void SetupChatRequestBuilder(Action<ChatRequestOptions> captureCallback)
    {
        _chatRequestBuilderMock
            .Setup(b => b.BuildAsync(
                It.IsAny<ResearchAgent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ChatRequestOptions>()))
            .Callback<ResearchAgent, string, string, ChatRequestOptions>(
                (_, _, _, opts) => captureCallback(opts))
            .ReturnsAsync(CreateValidRequest());
    }

    private void SetupChatServiceWithResponse(string content)
    {
        _chatServiceMock
            .Setup(s => s.ChatCompletionsAsync(It.IsAny<AesirChatRequestBase>()))
            .ReturnsAsync(CreateValidResult(content));
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

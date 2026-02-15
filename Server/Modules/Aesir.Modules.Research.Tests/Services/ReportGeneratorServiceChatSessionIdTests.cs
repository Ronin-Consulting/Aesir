using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ReportGeneratorServiceChatSessionIdTests
{
    private readonly Mock<ILogger<ReportGeneratorService>> _loggerMock;
    private readonly Mock<IChatServiceResolver> _chatServiceResolverMock;
    private readonly Mock<IChatRequestBuilder> _chatRequestBuilderMock;
    private readonly Mock<IConfidenceCalculator> _confidenceCalculatorMock;
    private readonly Mock<IScoringCalculator> _scoringCalculatorMock;
    private readonly Mock<IResearchProgressBroadcaster> _progressBroadcasterMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly ReportGeneratorService _service;

    public ReportGeneratorServiceChatSessionIdTests()
    {
        _loggerMock = new Mock<ILogger<ReportGeneratorService>>();
        _chatServiceResolverMock = new Mock<IChatServiceResolver>();
        _chatRequestBuilderMock = new Mock<IChatRequestBuilder>();
        _confidenceCalculatorMock = new Mock<IConfidenceCalculator>();
        _scoringCalculatorMock = new Mock<IScoringCalculator>();
        _progressBroadcasterMock = new Mock<IResearchProgressBroadcaster>();
        _chatServiceMock = new Mock<IChatService>();

        _service = new ReportGeneratorService(
            _loggerMock.Object,
            _chatServiceResolverMock.Object,
            _chatRequestBuilderMock.Object,
            _confidenceCalculatorMock.Object,
            _scoringCalculatorMock.Object,
            _progressBroadcasterMock.Object);
    }

    [Fact]
    public async Task GenerateReportAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Test research query",
            ChatSessionId = chatSessionId,
            Submissions = new List<ResearchSubmission>
            {
                new()
                {
                    Id = submissionId,
                    SessionId = Guid.NewGuid(),
                    Role = ResearchRole.DeepDiver,
                    Content = "Deep diver findings",
                    Status = SubmissionStatus.Completed,
                    Sources = new List<ResearchSource>(),
                    ToolCalls = new List<ResearchToolCall>(),
                    CreatedAt = DateTime.UtcNow
                }
            },
            PeerReviews = new List<PeerReview>()
        };

        var chairman = CreateChairmanAgent();
        var submissionScores = new List<SubmissionScore>
        {
            new()
            {
                SubmissionId = submissionId,
                AverageScore = 7.5,
                MedianScore = 7.5,
                MinScore = 7.0,
                MaxScore = 8.0,
                ReviewCount = 1,
                Confidence = ConfidenceLevel.Medium
            }
        };

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
            .Setup(r => r.GetChatService(It.IsAny<Guid?>()))
            .Returns(_chatServiceMock.Object);

        _chatServiceMock
            .Setup(s => s.ChatCompletionsAsync(It.IsAny<AesirChatRequestBase>()))
            .ReturnsAsync(CreateValidResult("# Research Report\n\nExecutive summary of the findings."));

        _confidenceCalculatorMock
            .Setup(c => c.ScoreToConfidence(It.IsAny<double>()))
            .Returns(ConfidenceLevel.Medium);

        // Act
        await _service.GenerateReportAsync(
            session, submissionScores, chairman);

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
            Persona = "You are a research chairman.",
            SynthesisPrompt = "Synthesize the research findings."
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

using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Execution;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class PeerReviewServiceChatSessionIdTests
{
    private readonly Mock<ILogger<PeerReviewService>> _loggerMock;
    private readonly Mock<IChatServiceResolver> _chatServiceResolverMock;
    private readonly Mock<IChatRequestBuilder> _chatRequestBuilderMock;
    private readonly Mock<IScoringCalculator> _scoringCalculatorMock;
    private readonly Mock<IResearchProgressBroadcaster> _progressBroadcasterMock;
    private readonly Mock<IPhaseExecutionStrategyFactory> _strategyFactoryMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly PeerReviewService _service;

    public PeerReviewServiceChatSessionIdTests()
    {
        _loggerMock = new Mock<ILogger<PeerReviewService>>();
        _chatServiceResolverMock = new Mock<IChatServiceResolver>();
        _chatRequestBuilderMock = new Mock<IChatRequestBuilder>();
        _scoringCalculatorMock = new Mock<IScoringCalculator>();
        _progressBroadcasterMock = new Mock<IResearchProgressBroadcaster>();
        _strategyFactoryMock = new Mock<IPhaseExecutionStrategyFactory>();
        _chatServiceMock = new Mock<IChatService>();

        _service = new PeerReviewService(
            _loggerMock.Object,
            _chatServiceResolverMock.Object,
            _chatRequestBuilderMock.Object,
            _scoringCalculatorMock.Object,
            _progressBroadcasterMock.Object,
            _strategyFactoryMock.Object);
    }

    [Fact]
    public async Task ConductPeerReviewsAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Test research query",
            ChatSessionId = chatSessionId
        };

        var reviewer = CreateTeamAgent(ResearchRole.Synthesizer, "Synthesizer");
        var agents = new List<ResearchAgent> { reviewer };

        var submissionId = Guid.NewGuid();
        var anonymizedSubmissions = new Dictionary<string, AnonymizedSubmission>
        {
            ["ANON-001"] = new AnonymizedSubmission
            {
                AnonymizedId = "ANON-001",
                OriginalSubmissionId = submissionId,
                OriginalRole = ResearchRole.DeepDiver,
                AnonymizedContent = "Research content from Deep Diver"
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
            .ReturnsAsync(CreateValidResult("""
                ```json
                {"depth": 7, "accuracy": 8, "source_quality": 6, "novelty": 7, "coherence": 8, "weighted_average": 7.2}
                ```
                ### Key Strengths
                Good analysis.
                ### Areas for Improvement
                More sources needed.
                """));

        _scoringCalculatorMock
            .Setup(s => s.CalculateWeightedAverage(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>()))
            .Returns(7.2);

        // Mock the strategy to invoke the callback directly
        var strategyMock = new Mock<IPhaseExecutionStrategy<(ResearchAgent Reviewer, AnonymizedSubmission Submission), PeerReview>>();
        strategyMock
            .Setup(s => s.ExecutePhaseWithAgentTrackingAsync(
                It.IsAny<ResearchSession>(),
                It.IsAny<IReadOnlyList<(ResearchAgent Reviewer, AnonymizedSubmission Submission)>>(),
                It.IsAny<Func<ResearchSession, (ResearchAgent Reviewer, AnonymizedSubmission Submission), CancellationToken, Task<PeerReview>>>(),
                It.IsAny<Func<(ResearchAgent Reviewer, AnonymizedSubmission Submission), ActiveAgentInfo>>(),
                It.IsAny<CancellationToken>()))
            .Returns<ResearchSession,
                IReadOnlyList<(ResearchAgent Reviewer, AnonymizedSubmission Submission)>,
                Func<ResearchSession, (ResearchAgent Reviewer, AnonymizedSubmission Submission), CancellationToken, Task<PeerReview>>,
                Func<(ResearchAgent Reviewer, AnonymizedSubmission Submission), ActiveAgentInfo>,
                CancellationToken>(
                async (sess, inputs, executeFunc, _, ct) =>
                {
                    var results = new List<PeerReview>();
                    foreach (var input in inputs)
                    {
                        var result = await executeFunc(sess, input, ct);
                        results.Add(result);
                    }
                    return results;
                });

        _strategyFactoryMock
            .Setup(f => f.CreatePeerReviewStrategy())
            .Returns(strategyMock.Object);

        // Act
        await _service.ConductPeerReviewsAsync(
            session, agents, anonymizedSubmissions);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.ChatSessionId.Should().Be(chatSessionId,
            "ChatSessionId from ResearchSession should be propagated to ChatRequestOptions for observability");
    }

    #region Helper Methods

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

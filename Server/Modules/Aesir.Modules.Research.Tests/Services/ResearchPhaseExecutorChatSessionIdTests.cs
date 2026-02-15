using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Execution;
using Aesir.Modules.Research.Hubs;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Tests.Services;

public class ResearchPhaseExecutorChatSessionIdTests
{
    private readonly Mock<ILogger<ResearchPhaseExecutor>> _loggerMock;
    private readonly Mock<IChatServiceResolver> _chatServiceResolverMock;
    private readonly Mock<IChatRequestBuilder> _chatRequestBuilderMock;
    private readonly Mock<IHubContext<ResearchHub>> _hubContextMock;
    private readonly Mock<IAnonymizationService> _anonymizationServiceMock;
    private readonly Mock<IPeerReviewService> _peerReviewServiceMock;
    private readonly Mock<IReportGeneratorService> _reportGeneratorServiceMock;
    private readonly Mock<IScoringCalculator> _scoringCalculatorMock;
    private readonly Mock<IResearchProgressBroadcaster> _progressBroadcasterMock;
    private readonly Mock<IChairmanPlanningService> _chairmanPlanningServiceMock;
    private readonly Mock<IPhaseExecutionStrategyFactory> _strategyFactoryMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly ResearchPhaseExecutor _service;

    public ResearchPhaseExecutorChatSessionIdTests()
    {
        _loggerMock = new Mock<ILogger<ResearchPhaseExecutor>>();
        _chatServiceResolverMock = new Mock<IChatServiceResolver>();
        _chatRequestBuilderMock = new Mock<IChatRequestBuilder>();
        _hubContextMock = new Mock<IHubContext<ResearchHub>>();
        _anonymizationServiceMock = new Mock<IAnonymizationService>();
        _peerReviewServiceMock = new Mock<IPeerReviewService>();
        _reportGeneratorServiceMock = new Mock<IReportGeneratorService>();
        _scoringCalculatorMock = new Mock<IScoringCalculator>();
        _progressBroadcasterMock = new Mock<IResearchProgressBroadcaster>();
        _chairmanPlanningServiceMock = new Mock<IChairmanPlanningService>();
        _strategyFactoryMock = new Mock<IPhaseExecutionStrategyFactory>();
        _chatServiceMock = new Mock<IChatService>();

        _service = new ResearchPhaseExecutor(
            _loggerMock.Object,
            _chatServiceResolverMock.Object,
            _chatRequestBuilderMock.Object,
            _hubContextMock.Object,
            _anonymizationServiceMock.Object,
            _peerReviewServiceMock.Object,
            _reportGeneratorServiceMock.Object,
            _scoringCalculatorMock.Object,
            _progressBroadcasterMock.Object,
            _chairmanPlanningServiceMock.Object,
            _strategyFactoryMock.Object);
    }

    [Fact]
    public async Task ExecuteResearchPhaseAsync_PropagatesChatSessionId_ToChatRequestOptions()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            Query = "Test research query",
            ChatSessionId = chatSessionId
        };
        var agent = CreateTeamAgent(ResearchRole.DeepDiver, "Deep Diver");
        var agents = new List<ResearchAgent> { agent };
        var agentPlans = new Dictionary<Guid, string>
        {
            { agent.TeamMemberId, "Research plan for Deep Diver" }
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
            .ReturnsAsync(CreateValidResult("Research findings content"));

        // Mock the strategy to invoke the callback directly with the input
        var strategyMock = new Mock<IPhaseExecutionStrategy<(ResearchAgent Agent, string Plan), ResearchSubmission>>();
        strategyMock
            .Setup(s => s.ExecutePhaseWithAgentTrackingAsync(
                It.IsAny<ResearchSession>(),
                It.IsAny<IReadOnlyList<(ResearchAgent Agent, string Plan)>>(),
                It.IsAny<Func<ResearchSession, (ResearchAgent Agent, string Plan), CancellationToken, Task<ResearchSubmission>>>(),
                It.IsAny<Func<(ResearchAgent Agent, string Plan), ActiveAgentInfo>>(),
                It.IsAny<CancellationToken>()))
            .Returns<ResearchSession,
                IReadOnlyList<(ResearchAgent Agent, string Plan)>,
                Func<ResearchSession, (ResearchAgent Agent, string Plan), CancellationToken, Task<ResearchSubmission>>,
                Func<(ResearchAgent Agent, string Plan), ActiveAgentInfo>,
                CancellationToken>(
                async (sess, inputs, executeFunc, _, ct) =>
                {
                    var results = new List<ResearchSubmission>();
                    foreach (var input in inputs)
                    {
                        var result = await executeFunc(sess, input, ct);
                        results.Add(result);
                    }
                    return results;
                });

        _strategyFactoryMock
            .Setup(f => f.CreateResearchStrategy())
            .Returns(strategyMock.Object);

        // Act
        await _service.ExecuteResearchPhaseAsync(
            session, agents, "Test refined query", agentPlans);

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
            Persona = $"You are a {roleName}.",
            ResearchPrompt = "Research: {{QUERY}}\n{{REFINED_CONTEXT}}"
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

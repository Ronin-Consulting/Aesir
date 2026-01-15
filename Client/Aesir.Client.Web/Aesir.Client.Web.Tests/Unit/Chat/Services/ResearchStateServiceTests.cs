using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Modules.Research.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class ResearchStateServiceTests
{
    private readonly Mock<IResearchSessionApiService> _sessionApi;
    private readonly Mock<IResearchSignalRService> _signalRService;
    private readonly Mock<IChatHistoryService> _chatHistoryService;
    private readonly ResearchStateService _service;

    public ResearchStateServiceTests()
    {
        _sessionApi = new Mock<IResearchSessionApiService>();
        _signalRService = new Mock<IResearchSignalRService>();
        _chatHistoryService = new Mock<IChatHistoryService>();

        _signalRService.SetupGet(s => s.IsConnected).Returns(false);

        _service = new ResearchStateService(
            _sessionApi.Object,
            _signalRService.Object,
            _chatHistoryService.Object,
            null);
    }

    #region RestoreSessionForConversationAsync Tests

    [Fact]
    public async Task RestoreSessionForConversationAsync_NoSessions_ReturnsFalse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase { Sessions = [] }));

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeFalse();
        _service.ActiveSession.Should().BeNull();
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_InProgressSession_RestoresAndSubscribes()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new ResearchSessionBase
        {
            Id = sessionId,
            ConversationId = conversationId,
            Status = ResearchStatusBase.Researching,
            CurrentPhase = ResearchPhaseBase.Research,
            Query = "Test query"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = [session]
            }));

        _signalRService.SetupGet(s => s.IsConnected).Returns(true);

        var sessionChangedRaised = false;
        _service.OnSessionChanged += () => sessionChangedRaised = true;

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeTrue();
        _service.ActiveSession.Should().NotBeNull();
        _service.ActiveSession!.Id.Should().Be(sessionId);
        _service.IsResearchInProgress.Should().BeTrue();
        sessionChangedRaised.Should().BeTrue();
        _signalRService.Verify(s => s.SubscribeToSessionAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_CompletedSession_RestoresWithoutSubscription()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new ResearchSessionBase
        {
            Id = sessionId,
            ConversationId = conversationId,
            Status = ResearchStatusBase.Completed,
            CurrentPhase = ResearchPhaseBase.Synthesis,
            Query = "Completed research"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = [session]
            }));

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeTrue();
        _service.ActiveSession.Should().NotBeNull();
        _service.ActiveSession!.Id.Should().Be(sessionId);
        _service.IsResearchInProgress.Should().BeFalse();
        _service.CurrentProgressPercent.Should().Be(100);
        _signalRService.Verify(s => s.SubscribeToSessionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_PrioritizesInProgressOverCompleted()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var completedSessionId = Guid.NewGuid();
        var inProgressSessionId = Guid.NewGuid();

        var completedSession = new ResearchSessionBase
        {
            Id = completedSessionId,
            ConversationId = conversationId,
            Status = ResearchStatusBase.Completed,
            Query = "Completed"
        };

        var inProgressSession = new ResearchSessionBase
        {
            Id = inProgressSessionId,
            ConversationId = conversationId,
            Status = ResearchStatusBase.Researching,
            Query = "In progress"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                // Completed first in list, but in-progress should be prioritized
                Sessions = [completedSession, inProgressSession]
            }));

        _signalRService.SetupGet(s => s.IsConnected).Returns(true);

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeTrue();
        _service.ActiveSession!.Id.Should().Be(inProgressSessionId);
        _service.IsResearchInProgress.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_ApiError_ReturnsFalse()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Failure("API error"));

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeFalse();
        _service.ActiveSession.Should().BeNull();
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_ClearsSessionWhenNoResearch()
    {
        // Arrange - First set an active session
        var originalSessionId = Guid.NewGuid();
        _service.HandleProgressUpdate(new ResearchProgressBase
        {
            SessionId = originalSessionId,
            Status = ResearchStatusBase.Researching,
            Phase = ResearchPhaseBase.Research,
            Message = "Test"
        });

        // Need to set ActiveSession directly for test - using reflection or alternative approach
        // Instead, we'll verify the behavior through the ClearSession call pattern

        var newConversationId = Guid.NewGuid();
        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(newConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = []
            }));

        // Act
        var result = await _service.RestoreSessionForConversationAsync(newConversationId);

        // Assert
        result.Should().BeFalse();
        _service.ActiveSession.Should().BeNull();
        _service.CurrentProgressMessage.Should().BeNull();
        _service.CurrentProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task RestoreSessionForConversationAsync_ConnectsSignalRIfNotConnected()
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = new ResearchSessionBase
        {
            Id = sessionId,
            ConversationId = conversationId,
            Status = ResearchStatusBase.Planning,
            Query = "Test"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = [session]
            }));

        // SignalR not initially connected, but connect succeeds
        _signalRService.SetupSequence(s => s.IsConnected)
            .Returns(false)  // First check
            .Returns(true);  // After connect
        _signalRService.Setup(s => s.ConnectAsync()).ReturnsAsync(true);

        // Act
        var result = await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        result.Should().BeTrue();
        _signalRService.Verify(s => s.ConnectAsync(), Times.Once);
    }

    #endregion

    #region IsResearchInProgress Tests

    [Fact]
    public void IsResearchInProgress_NoActiveSession_ReturnsFalse()
    {
        _service.IsResearchInProgress.Should().BeFalse();
    }

    [Theory]
    [InlineData(ResearchStatusBase.Created)]
    [InlineData(ResearchStatusBase.AwaitingClarification)]
    [InlineData(ResearchStatusBase.Planning)]
    [InlineData(ResearchStatusBase.Researching)]
    [InlineData(ResearchStatusBase.Anonymizing)]
    [InlineData(ResearchStatusBase.PeerReviewing)]
    [InlineData(ResearchStatusBase.Synthesizing)]
    public async Task IsResearchInProgress_ActiveStatuses_ReturnsTrue(ResearchStatusBase status)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var session = new ResearchSessionBase
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Status = status,
            Query = "Test"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = [session]
            }));

        _signalRService.SetupGet(s => s.IsConnected).Returns(true);

        // Act
        await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        _service.IsResearchInProgress.Should().BeTrue();
    }

    [Theory]
    [InlineData(ResearchStatusBase.Completed)]
    [InlineData(ResearchStatusBase.Failed)]
    [InlineData(ResearchStatusBase.Cancelled)]
    public async Task IsResearchInProgress_TerminalStatuses_ReturnsFalse(ResearchStatusBase status)
    {
        // Arrange
        var conversationId = Guid.NewGuid();
        var session = new ResearchSessionBase
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Status = status,
            Query = "Test"
        };

        _sessionApi
            .Setup(x => x.GetSessionsByConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchSessionListBase>.Success(new ResearchSessionListBase
            {
                Sessions = [session]
            }));

        // Act
        await _service.RestoreSessionForConversationAsync(conversationId);

        // Assert
        _service.IsResearchInProgress.Should().BeFalse();
    }

    #endregion
}

using Aesir.Client.Web.Infrastructure.Services;

namespace Aesir.Client.Web.Tests.Unit.Infrastructure.Services;

public class ChatSessionNotifierTests
{
    private readonly ChatSessionNotifier _sut;

    public ChatSessionNotifierTests()
    {
        _sut = new ChatSessionNotifier();
    }

    [Fact]
    public void NotifySessionCreated_RaisesOnSessionCreated_WithCorrectSessionId()
    {
        // Arrange
        var expectedSessionId = Guid.NewGuid();
        Guid? receivedSessionId = null;
        _sut.OnSessionCreated += id => receivedSessionId = id;

        // Act
        _sut.NotifySessionCreated(expectedSessionId);

        // Assert
        receivedSessionId.Should().Be(expectedSessionId);
    }

    [Fact]
    public void NotifySessionCreated_DoesNotThrow_WhenNoSubscribers()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        var act = () => _sut.NotifySessionCreated(sessionId);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void NotifySessionCreated_NotifiesMultipleSubscribers()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var callCount = 0;
        _sut.OnSessionCreated += _ => callCount++;
        _sut.OnSessionCreated += _ => callCount++;

        // Act
        _sut.NotifySessionCreated(sessionId);

        // Assert
        callCount.Should().Be(2);
    }
}

using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class ChatStateServiceTests
{
    private readonly ChatStateService _service;
    private readonly Mock<IJSRuntime> _jsRuntime;

    public ChatStateServiceTests()
    {
        _jsRuntime = new Mock<IJSRuntime>();
        _service = new ChatStateService(_jsRuntime.Object);
    }

    [Fact]
    public void SelectedAgent_IsNull_Initially()
    {
        // Assert
        _service.SelectedAgent.Should().BeNull();
        _service.SelectedAgentId.Should().BeNull();
    }

    [Fact]
    public void CurrentSessionId_IsNull_Initially()
    {
        // Assert
        _service.CurrentSessionId.Should().BeNull();
        _service.HasActiveChat.Should().BeFalse();
    }

    [Fact]
    public void SelectAgent_SetsSelectedAgent()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };

        // Act
        _service.SelectAgent(agent);

        // Assert
        _service.SelectedAgent.Should().Be(agent);
        _service.SelectedAgentId.Should().Be(agent.Id);
    }

    [Fact]
    public void SelectAgent_RaisesOnAgentChangedEvent()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        var eventRaised = false;
        _service.OnAgentChanged += () => eventRaised = true;

        // Act
        _service.SelectAgent(agent);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void SelectAgent_DoesNotRaiseEvent_WhenSameAgentSelected()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _service.SelectAgent(agent);
        var eventRaised = false;
        _service.OnAgentChanged += () => eventRaised = true;

        // Act
        _service.SelectAgent(agent);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SelectAgent_CanSetToNull()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _service.SelectAgent(agent);

        // Act
        _service.SelectAgent(null);

        // Assert
        _service.SelectedAgent.Should().BeNull();
        _service.SelectedAgentId.Should().BeNull();
    }

    [Fact]
    public void SetCurrentSession_SetsSessionId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        _service.SetCurrentSession(sessionId);

        // Assert
        _service.CurrentSessionId.Should().Be(sessionId);
        _service.HasActiveChat.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentSession_RaisesOnConversationChangedEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var eventRaised = false;
        _service.OnConversationChanged += () => eventRaised = true;

        // Act
        _service.SetCurrentSession(sessionId);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentSession_DoesNotRaiseEvent_WhenSameSessionSet()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        var eventRaised = false;
        _service.OnConversationChanged += () => eventRaised = true;

        // Act
        _service.SetCurrentSession(sessionId);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void StartNewChat_ClearsSessionId()
    {
        // Arrange
        _service.SetCurrentSession(Guid.NewGuid());

        // Act
        _service.StartNewChat();

        // Assert
        _service.CurrentSessionId.Should().BeNull();
        _service.HasActiveChat.Should().BeFalse();
    }

    [Fact]
    public void StartNewChat_RaisesOnNewChatEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.OnNewChat += () => eventRaised = true;

        // Act
        _service.StartNewChat();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void NotifyConversationChanged_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.OnConversationChanged += () => eventRaised = true;

        // Act
        _service.NotifyConversationChanged();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void MultipleAgentChanges_RaiseCorrectEvents()
    {
        // Arrange
        var agent1 = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Agent 1" };
        var agent2 = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Agent 2" };
        var eventCount = 0;
        _service.OnAgentChanged += () => eventCount++;

        // Act
        _service.SelectAgent(agent1);
        _service.SelectAgent(agent2);
        _service.SelectAgent(null);

        // Assert
        eventCount.Should().Be(3);
    }

    [Fact]
    public void HasActiveChat_ReturnsFalse_WhenSessionIdNull()
    {
        // Arrange
        _service.SetCurrentSession(null);

        // Assert
        _service.HasActiveChat.Should().BeFalse();
    }

    [Fact]
    public void HasActiveChat_ReturnsTrue_WhenSessionIdSet()
    {
        // Arrange
        _service.SetCurrentSession(Guid.NewGuid());

        // Assert
        _service.HasActiveChat.Should().BeTrue();
    }

    [Fact]
    public void CurrentSessionTitle_IsNull_Initially()
    {
        // Assert
        _service.CurrentSessionTitle.Should().BeNull();
    }

    [Fact]
    public void SetCurrentSession_SetsSessionTitle()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var title = "Test Chat Title";

        // Act
        _service.SetCurrentSession(sessionId, title);

        // Assert
        _service.CurrentSessionId.Should().Be(sessionId);
        _service.CurrentSessionTitle.Should().Be(title);
    }

    [Fact]
    public void SetCurrentSessionTitle_UpdatesTitle()
    {
        // Arrange
        _service.SetCurrentSession(Guid.NewGuid(), "Initial Title");

        // Act
        _service.SetCurrentSessionTitle("Updated Title");

        // Assert
        _service.CurrentSessionTitle.Should().Be("Updated Title");
    }

    [Fact]
    public void SetCurrentSessionTitle_RaisesOnConversationChangedEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.OnConversationChanged += () => eventRaised = true;

        // Act
        _service.SetCurrentSessionTitle("New Title");

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentSessionTitle_DoesNotRaiseEvent_WhenSameTitleSet()
    {
        // Arrange
        _service.SetCurrentSessionTitle("Same Title");
        var eventRaised = false;
        _service.OnConversationChanged += () => eventRaised = true;

        // Act
        _service.SetCurrentSessionTitle("Same Title");

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void StartNewChat_ClearsSessionTitle()
    {
        // Arrange
        _service.SetCurrentSession(Guid.NewGuid(), "Test Title");

        // Act
        _service.StartNewChat();

        // Assert
        _service.CurrentSessionTitle.Should().BeNull();
    }
}

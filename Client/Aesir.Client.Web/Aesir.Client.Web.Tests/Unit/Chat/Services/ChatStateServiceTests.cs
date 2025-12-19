using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class ChatStateServiceTests
{
    private readonly ChatStateService _service;
    private readonly Mock<IJSRuntime> _jsRuntime;
    private readonly Mock<IChatPreferencesService> _preferencesService;

    public ChatStateServiceTests()
    {
        _jsRuntime = new Mock<IJSRuntime>();
        _preferencesService = new Mock<IChatPreferencesService>();

        // Setup default returns for preferences service
        _preferencesService
            .Setup(x => x.GetDisabledToolIdsCached(It.IsAny<Guid?>()))
            .Returns(new HashSet<Guid>());
        _preferencesService
            .Setup(x => x.GetThinkLevelCached(It.IsAny<Guid?>()))
            .Returns((ThinkValue?)null);
        _preferencesService
            .Setup(x => x.SetToolEnabledAsync(It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.SetThinkLevelAsync(It.IsAny<Guid?>(), It.IsAny<ThinkValue?>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.ClearToolTogglesAsync(It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.ClearThinkLevelAsync(It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.RestoreToolTogglesFromStorageAsync(It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.RestoreThinkLevelFromStorageAsync(It.IsAny<Guid?>()))
            .Returns(Task.CompletedTask);
        _preferencesService
            .Setup(x => x.GetDisabledToolIdsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new HashSet<Guid>());
        _preferencesService
            .Setup(x => x.GetThinkLevelAsync(It.IsAny<Guid?>()))
            .ReturnsAsync((ThinkValue?)null);

        _service = new ChatStateService(_jsRuntime.Object, _preferencesService.Object, null);
    }

    #region Agent Selection Tests

    [Fact]
    public void SelectedAgent_IsNull_Initially()
    {
        // Assert
        _service.SelectedAgent.Should().BeNull();
        _service.SelectedAgentId.Should().BeNull();
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

    #endregion

    #region Session Management Tests

    [Fact]
    public void CurrentSessionId_IsNull_Initially()
    {
        // Assert
        _service.CurrentSessionId.Should().BeNull();
        _service.HasActiveChat.Should().BeFalse();
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

    #endregion

    #region Tool Toggle Delegation Tests

    [Fact]
    public void DisabledToolIds_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var disabledIds = new HashSet<Guid> { Guid.NewGuid() };
        _service.SetCurrentSession(sessionId);
        _preferencesService.Setup(x => x.GetDisabledToolIdsCached(sessionId)).Returns(disabledIds);

        // Act
        var result = _service.DisabledToolIds;

        // Assert
        result.Should().BeSameAs(disabledIds);
        _preferencesService.Verify(x => x.GetDisabledToolIdsCached(sessionId), Times.Once);
    }

    [Fact]
    public async Task SetToolEnabledAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.SetToolEnabledAsync(toolId, false);

        // Assert
        _preferencesService.Verify(x => x.SetToolEnabledAsync(sessionId, toolId, false), Times.Once);
    }

    [Fact]
    public void IsToolEnabled_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        _preferencesService.Setup(x => x.IsToolEnabled(sessionId, toolId)).Returns(false);

        // Act
        var result = _service.IsToolEnabled(toolId);

        // Assert
        result.Should().BeFalse();
        _preferencesService.Verify(x => x.IsToolEnabled(sessionId, toolId), Times.Once);
    }

    [Fact]
    public async Task ClearToolTogglesAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        _preferencesService.Verify(x => x.ClearToolTogglesAsync(sessionId), Times.Once);
    }

    [Fact]
    public void GetDisabledToolIdsForSession_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var disabledIds = new HashSet<Guid> { Guid.NewGuid() };
        _preferencesService.Setup(x => x.GetDisabledToolIdsCached(sessionId)).Returns(disabledIds);

        // Act
        var result = _service.GetDisabledToolIdsForSession(sessionId);

        // Assert
        result.Should().BeSameAs(disabledIds);
    }

    [Fact]
    public void GetDisabledToolIdsForSession_ReturnsEmpty_ForEmptyGuid()
    {
        // Act
        var result = _service.GetDisabledToolIdsForSession(Guid.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        await _service.RestoreToolTogglesForSessionAsync(sessionId);

        // Assert
        _preferencesService.Verify(x => x.RestoreToolTogglesFromStorageAsync(sessionId), Times.Once);
    }

    [Fact]
    public void OnToolTogglesChanged_ForwardedFromPreferencesService()
    {
        // Arrange
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        _preferencesService.Raise(x => x.OnToolTogglesChanged += null);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Think Level Delegation Tests

    [Fact]
    public void SelectedThinkLevel_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var thinkLevel = new ThinkValue(ThinkValue.High);
        _service.SetCurrentSession(sessionId);
        _preferencesService.Setup(x => x.GetThinkLevelCached(sessionId)).Returns(thinkLevel);

        // Act
        var result = _service.SelectedThinkLevel;

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ThinkValue.High);
        _preferencesService.Verify(x => x.GetThinkLevelCached(sessionId), Times.Once);
    }

    [Fact]
    public async Task SetThinkLevelAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.Medium);
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.SetThinkLevelAsync(level);

        // Assert
        _preferencesService.Verify(x => x.SetThinkLevelAsync(sessionId, level), Times.Once);
    }

    [Fact]
    public async Task ClearThinkLevelAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.ClearThinkLevelAsync();

        // Assert
        _preferencesService.Verify(x => x.ClearThinkLevelAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task GetThinkLevelForSessionAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.Low);
        _preferencesService.Setup(x => x.GetThinkLevelAsync(sessionId)).ReturnsAsync(level);

        // Act
        var result = await _service.GetThinkLevelForSessionAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ThinkValue.Low);
    }

    [Fact]
    public async Task GetThinkLevelForSessionAsync_ReturnsNull_ForEmptyGuid()
    {
        // Act
        var result = await _service.GetThinkLevelForSessionAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreThinkLevelForSessionAsync_DelegatesToPreferencesService()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        // Act
        await _service.RestoreThinkLevelForSessionAsync(sessionId);

        // Assert
        _preferencesService.Verify(x => x.RestoreThinkLevelFromStorageAsync(sessionId), Times.Once);
    }

    [Fact]
    public void OnThinkLevelChanged_ForwardedFromPreferencesService()
    {
        // Arrange
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        _preferencesService.Raise(x => x.OnThinkLevelChanged += null);

        // Assert
        eventRaised.Should().BeTrue();
    }

    #endregion

    #region Document Deletion Tests

    [Fact]
    public void NotifyDocumentDeleted_RaisesOnDocumentDeletedEvent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        Guid? receivedDocumentId = null;
        _service.OnDocumentDeleted += id => receivedDocumentId = id;

        // Act
        _service.NotifyDocumentDeleted(documentId);

        // Assert
        receivedDocumentId.Should().Be(documentId);
    }

    [Fact]
    public void NotifyDocumentDeleted_DoesNotThrow_WhenNoSubscribers()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var action = () => _service.NotifyDocumentDeleted(documentId);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void NotifyDocumentDeleted_SupportsMultipleSubscribers()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var subscriber1Called = false;
        var subscriber2Called = false;
        _service.OnDocumentDeleted += _ => subscriber1Called = true;
        _service.OnDocumentDeleted += _ => subscriber2Called = true;

        // Act
        _service.NotifyDocumentDeleted(documentId);

        // Assert
        subscriber1Called.Should().BeTrue();
        subscriber2Called.Should().BeTrue();
    }

    [Fact]
    public void OnDocumentDeleted_CanBeUnsubscribed()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var eventRaised = false;
        void Handler(Guid id) => eventRaised = true;
        _service.OnDocumentDeleted += Handler;
        _service.OnDocumentDeleted -= Handler;

        // Act
        _service.NotifyDocumentDeleted(documentId);

        // Assert
        eventRaised.Should().BeFalse();
    }

    #endregion
}

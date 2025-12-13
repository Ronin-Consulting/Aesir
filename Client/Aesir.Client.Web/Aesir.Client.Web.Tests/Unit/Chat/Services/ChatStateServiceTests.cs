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

    #region ThinkLevel Tests

    [Fact]
    public void SelectedThinkLevel_IsNull_Initially()
    {
        // Assert
        _service.SelectedThinkLevel.Should().BeNull();
    }

    [Fact]
    public async Task SetThinkLevelAsync_SetsLevel_Low()
    {
        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.Low));

        // Assert
        _service.SelectedThinkLevel.Should().NotBeNull();
        _service.SelectedThinkLevel!.Value.ToString().Should().Be(ThinkValue.Low);
    }

    [Fact]
    public async Task SetThinkLevelAsync_SetsLevel_Medium()
    {
        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.Medium));

        // Assert
        _service.SelectedThinkLevel.Should().NotBeNull();
        _service.SelectedThinkLevel!.Value.ToString().Should().Be(ThinkValue.Medium);
    }

    [Fact]
    public async Task SetThinkLevelAsync_SetsLevel_High()
    {
        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Assert
        _service.SelectedThinkLevel.Should().NotBeNull();
        _service.SelectedThinkLevel!.Value.ToString().Should().Be(ThinkValue.High);
    }

    [Fact]
    public async Task SetThinkLevelAsync_SetsLevel_True()
    {
        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(true));

        // Assert
        _service.SelectedThinkLevel.Should().NotBeNull();
        _service.SelectedThinkLevel!.Value.ToString().Should().BeOneOf("True", "true");
    }

    [Fact]
    public async Task SetThinkLevelAsync_SetsLevel_False()
    {
        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(false));

        // Assert
        _service.SelectedThinkLevel.Should().NotBeNull();
        _service.SelectedThinkLevel!.Value.ToString().Should().BeOneOf("False", "false");
    }

    [Fact]
    public async Task SetThinkLevelAsync_CanSetToNull()
    {
        // Arrange
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Act
        await _service.SetThinkLevelAsync(null);

        // Assert
        _service.SelectedThinkLevel.Should().BeNull();
    }

    [Fact]
    public async Task SetThinkLevelAsync_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.Medium));

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SetThinkLevelAsync_DoesNotRaiseEvent_WhenSameLevelSet()
    {
        // Arrange
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task SetThinkLevelAsync_PersistsToLocalStorage_WithSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Assert
        _jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem",
            It.Is<object[]>(args => args.Length == 2 &&
                args[0].ToString()!.Contains(sessionId.ToString()) &&
                args[1].ToString() == ThinkValue.High)),
            Times.Once);
    }

    [Fact]
    public async Task SetThinkLevelAsync_CachesInMemory_WithSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.Medium));

        // Act - Get from cache (should not call localStorage)
        var level = await _service.GetThinkLevelForSessionAsync(sessionId);

        // Assert
        level.Should().NotBeNull();
        level!.Value.ToString().Should().Be(ThinkValue.Medium);
        // Should only have one localStorage call (the setItem from SetThinkLevelAsync)
        _jsRuntime.Verify(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()), Times.Never);
    }

    [Fact]
    public async Task ClearThinkLevelAsync_ClearsLevel()
    {
        // Arrange
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Act
        await _service.ClearThinkLevelAsync();

        // Assert
        _service.SelectedThinkLevel.Should().BeNull();
    }

    [Fact]
    public async Task ClearThinkLevelAsync_RaisesEvent()
    {
        // Arrange
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _service.ClearThinkLevelAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ClearThinkLevelAsync_DoesNotRaiseEvent_WhenAlreadyNull()
    {
        // Arrange - default is null
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _service.ClearThinkLevelAsync();

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ClearThinkLevelAsync_RemovesFromLocalStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Act
        await _service.ClearThinkLevelAsync();

        // Assert
        _jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem",
            It.Is<object[]>(args => args.Length == 1 && args[0].ToString()!.Contains(sessionId.ToString()))),
            Times.Once);
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
    public async Task GetThinkLevelForSessionAsync_LoadsFromStorage_WhenNotCached()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(ThinkValue.Medium);

        // Act
        var level = await _service.GetThinkLevelForSessionAsync(sessionId);

        // Assert
        level.Should().NotBeNull();
        level!.Value.ToString().Should().Be(ThinkValue.Medium);
    }

    [Fact]
    public async Task RestoreThinkLevelForSessionAsync_RaisesEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var eventRaised = false;
        _service.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _service.RestoreThinkLevelForSessionAsync(sessionId);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreThinkLevelForSessionAsync_ResetsToNull_ForEmptyGuid()
    {
        // Arrange
        await _service.SetThinkLevelAsync(new ThinkValue(ThinkValue.High));

        // Act
        await _service.RestoreThinkLevelForSessionAsync(Guid.Empty);

        // Assert
        _service.SelectedThinkLevel.Should().BeNull();
    }

    #endregion

    #region ToolToggle Tests

    [Fact]
    public void DisabledToolIds_IsEmpty_Initially()
    {
        // Assert
        _service.DisabledToolIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SetToolEnabledAsync_AddsToDisabled_WhenFalse()
    {
        // Arrange
        var toolId = Guid.NewGuid();

        // Act
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert
        _service.DisabledToolIds.Should().Contain(toolId);
    }

    [Fact]
    public async Task SetToolEnabledAsync_RemovesFromDisabled_WhenTrue()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act
        await _service.SetToolEnabledAsync(toolId, enabled: true);

        // Assert
        _service.DisabledToolIds.Should().NotContain(toolId);
    }

    [Fact]
    public async Task SetToolEnabledAsync_RaisesEvent_WhenChanged()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SetToolEnabledAsync_DoesNotRaiseEvent_WhenNoChange()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act - disable again (no change)
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task SetToolEnabledAsync_PersistsToSession_WithActiveSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert - check that session storage was updated
        var storedIds = _service.GetDisabledToolIdsForSession(sessionId);
        storedIds.Should().Contain(toolId);
    }

    [Fact]
    public async Task SetToolEnabledAsync_PersistsToLocalStorage_WithSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);

        // Act
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert
        _jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.setItem",
            It.Is<object[]>(args => args.Length == 2 &&
                args[0].ToString()!.Contains(sessionId.ToString()) &&
                args[1].ToString()!.Contains(toolId.ToString()))),
            Times.Once);
    }

    [Fact]
    public async Task SetToolEnabledAsync_CachesInMemory_WithSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act - Get from cache (should not call localStorage.getItem)
        var storedIds = _service.GetDisabledToolIdsForSession(sessionId);

        // Assert
        storedIds.Should().Contain(toolId);
        // GetDisabledToolIdsForSession only reads from memory, not localStorage
        _jsRuntime.Verify(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()), Times.Never);
    }

    [Fact]
    public void IsToolEnabled_ReturnsTrue_WhenNotDisabled()
    {
        // Arrange
        var toolId = Guid.NewGuid();

        // Assert
        _service.IsToolEnabled(toolId).Should().BeTrue();
    }

    [Fact]
    public async Task IsToolEnabled_ReturnsFalse_WhenDisabled()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Assert
        _service.IsToolEnabled(toolId).Should().BeFalse();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_ClearsAllDisabled()
    {
        // Arrange
        var toolId1 = Guid.NewGuid();
        var toolId2 = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId1, enabled: false);
        await _service.SetToolEnabledAsync(toolId2, enabled: false);

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        _service.DisabledToolIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_RaisesEvent_WhenHadDisabled()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_DoesNotRaiseEvent_WhenAlreadyEmpty()
    {
        // Arrange - default is empty
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_RemovesSessionStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        var storedIds = _service.GetDisabledToolIdsForSession(sessionId);
        storedIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_RemovesFromLocalStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act
        await _service.ClearToolTogglesAsync();

        // Assert
        _jsRuntime.Verify(js => js.InvokeAsync<object>(
            "localStorage.removeItem",
            It.Is<object[]>(args => args.Length == 1 && args[0].ToString()!.Contains(sessionId.ToString()))),
            Times.Once);
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
    public void GetDisabledToolIdsForSession_ReturnsEmpty_ForUnknownSession()
    {
        // Act
        var result = _service.GetDisabledToolIdsForSession(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisabledToolIdsForSession_ReturnsDisabledIds_ForKnownSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act
        var result = _service.GetDisabledToolIdsForSession(sessionId);

        // Assert
        result.Should().Contain(toolId);
    }

    [Fact]
    public void RestoreToolTogglesForSession_ResetsToEmpty_ForEmptyGuid()
    {
        // Arrange - use sync version (in-memory only)
        _service.RestoreToolTogglesForSession(Guid.Empty);

        // Assert
        _service.DisabledToolIds.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreToolTogglesForSession_RestoresFromSession()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // Set up session 1 with a disabled tool
        _service.SetCurrentSession(sessionId1);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Switch to session 2 (this will clear current disabled tools when restored)
        _service.SetCurrentSession(sessionId2);
        _service.RestoreToolTogglesForSession(sessionId2);

        // Verify session 2 has no disabled tools
        _service.DisabledToolIds.Should().BeEmpty();

        // Act - restore session 1
        _service.RestoreToolTogglesForSession(sessionId1);

        // Assert - session 1's disabled tool should be restored
        _service.DisabledToolIds.Should().Contain(toolId);
    }

    [Fact]
    public async Task RestoreToolTogglesForSession_ClearsCurrentIfNoSessionData()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);
        var unknownSessionId = Guid.NewGuid();

        // Act
        _service.RestoreToolTogglesForSession(unknownSessionId);

        // Assert
        _service.DisabledToolIds.Should().BeEmpty();
    }

    [Fact]
    public void RestoreToolTogglesForSession_RaisesEvent()
    {
        // Arrange
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        _service.RestoreToolTogglesForSession(Guid.NewGuid());

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_ResetsToEmpty_ForEmptyGuid()
    {
        // Arrange
        var toolId = Guid.NewGuid();
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Act
        await _service.RestoreToolTogglesForSessionAsync(Guid.Empty);

        // Assert
        _service.DisabledToolIds.Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_RaisesEvent()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var eventRaised = false;
        _service.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        await _service.RestoreToolTogglesForSessionAsync(sessionId);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_LoadsFromCache_WhenCached()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        _service.SetCurrentSession(sessionId);
        await _service.SetToolEnabledAsync(toolId, enabled: false);

        // Clear current disabled tools (simulate switching sessions)
        _service.SetCurrentSession(Guid.NewGuid());
        _service.RestoreToolTogglesForSession(Guid.NewGuid());
        _service.DisabledToolIds.Should().BeEmpty();

        // Act - restore from original session
        await _service.RestoreToolTogglesForSessionAsync(sessionId);

        // Assert
        _service.DisabledToolIds.Should().Contain(toolId);
        // Should not call localStorage.getItem because it's cached
        _jsRuntime.Verify(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()), Times.Never);
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_LoadsFromStorage_WhenNotCached()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var json = System.Text.Json.JsonSerializer.Serialize(new HashSet<Guid> { toolId });
        _jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(json);

        // Act
        await _service.RestoreToolTogglesForSessionAsync(sessionId);

        // Assert
        _service.DisabledToolIds.Should().Contain(toolId);
    }

    [Fact]
    public async Task RestoreToolTogglesForSessionAsync_CachesLoadedData()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var json = System.Text.Json.JsonSerializer.Serialize(new HashSet<Guid> { toolId });
        _jsRuntime.Setup(js => js.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(json);

        // Act - load from storage
        await _service.RestoreToolTogglesForSessionAsync(sessionId);

        // Assert - verify it's now cached
        var cachedIds = _service.GetDisabledToolIdsForSession(sessionId);
        cachedIds.Should().Contain(toolId);
    }

    [Fact]
    public async Task SessionIsolation_DifferentSessionsHaveDifferentToggles()
    {
        // Arrange
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        var toolId1 = Guid.NewGuid();
        var toolId2 = Guid.NewGuid();

        // Set up session 1 with tool1 disabled
        _service.SetCurrentSession(sessionId1);
        await _service.SetToolEnabledAsync(toolId1, enabled: false);

        // Switch to session 2 and disable tool2
        _service.SetCurrentSession(sessionId2);
        _service.RestoreToolTogglesForSession(sessionId2);
        await _service.SetToolEnabledAsync(toolId2, enabled: false);

        // Act - get disabled IDs for each session
        var session1Disabled = _service.GetDisabledToolIdsForSession(sessionId1);
        var session2Disabled = _service.GetDisabledToolIdsForSession(sessionId2);

        // Assert
        session1Disabled.Should().Contain(toolId1);
        session1Disabled.Should().NotContain(toolId2);
        session2Disabled.Should().Contain(toolId2);
        session2Disabled.Should().NotContain(toolId1);
    }

    #endregion
}

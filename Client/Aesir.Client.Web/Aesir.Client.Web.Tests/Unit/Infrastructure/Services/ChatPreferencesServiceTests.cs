using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Aesir.Client.Web.Tests.Unit.Infrastructure.Services;

public class ChatPreferencesServiceTests
{
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly ChatPreferencesService _sut;

    public ChatPreferencesServiceTests()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();

        // Setup default behavior for void localStorage methods
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>(
                It.Is<string>(s => s == "localStorage.setItem" || s == "localStorage.removeItem"),
                It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult)null!);

        _sut = new ChatPreferencesService(_mockJsRuntime.Object, null);
    }

    #region Tool Toggles Tests

    [Fact]
    public async Task GetDisabledToolIdsAsync_ReturnsEmptySet_WhenNothingStored()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.GetDisabledToolIdsAsync(sessionId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisabledToolIdsAsync_ReturnsStoredToolIds_WhenDataExists()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var disabledToolId = Guid.NewGuid();
        var json = $"[\"{disabledToolId}\"]";

        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(json);

        // Act
        var result = await _sut.GetDisabledToolIdsAsync(sessionId);

        // Assert
        result.Should().Contain(disabledToolId);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetToolEnabledAsync_AddsToDisabledSet_WhenEnabledIsFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // Act
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Assert
        var result = _sut.GetDisabledToolIdsCached(sessionId);
        result.Should().Contain(toolId);
    }

    [Fact]
    public async Task SetToolEnabledAsync_RemovesFromDisabledSet_WhenEnabledIsTrue()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // First disable the tool
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);
        _sut.GetDisabledToolIdsCached(sessionId).Should().Contain(toolId);

        // Act - re-enable the tool
        await _sut.SetToolEnabledAsync(sessionId, toolId, true);

        // Assert
        var result = _sut.GetDisabledToolIdsCached(sessionId);
        result.Should().NotContain(toolId);
    }

    [Fact]
    public async Task SetToolEnabledAsync_PersistsToLocalStorage_WhenSessionIdProvided()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var expectedKey = $"aesir_disabled_tools_{sessionId}";

        // Act
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>(
                "localStorage.setItem",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString() == expectedKey)),
            Times.Once);
    }

    [Fact]
    public async Task SetToolEnabledAsync_RaisesOnToolTogglesChanged_WhenStateChanges()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var eventRaised = false;
        _sut.OnToolTogglesChanged += () => eventRaised = true;

        // Act
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SetToolEnabledAsync_DoesNotRaiseEvent_WhenStateUnchanged()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // First disable the tool
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        var eventRaised = false;
        _sut.OnToolTogglesChanged += () => eventRaised = true;

        // Act - disable again (no change)
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_RemovesAllToggles_ForSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId1 = Guid.NewGuid();
        var toolId2 = Guid.NewGuid();

        await _sut.SetToolEnabledAsync(sessionId, toolId1, false);
        await _sut.SetToolEnabledAsync(sessionId, toolId2, false);

        // Act
        await _sut.ClearToolTogglesAsync(sessionId);

        // Assert
        var result = _sut.GetDisabledToolIdsCached(sessionId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearToolTogglesAsync_RemovesFromLocalStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        var expectedKey = $"aesir_disabled_tools_{sessionId}";

        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Act
        await _sut.ClearToolTogglesAsync(sessionId);

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0].ToString() == expectedKey)),
            Times.Once);
    }

    [Fact]
    public async Task GetDisabledToolIdsCached_ReturnsCachedValue_WithoutLocalStorageCall()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // Pre-populate cache
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);
        _mockJsRuntime.Invocations.Clear();

        // Act
        var result = _sut.GetDisabledToolIdsCached(sessionId);

        // Assert
        result.Should().Contain(toolId);
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public void IsToolEnabled_ReturnsTrue_WhenToolNotDisabled()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // Act
        var result = _sut.IsToolEnabled(sessionId, toolId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsToolEnabled_ReturnsFalse_WhenToolDisabled()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var toolId = Guid.NewGuid();
        await _sut.SetToolEnabledAsync(sessionId, toolId, false);

        // Act
        var result = _sut.IsToolEnabled(sessionId, toolId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleSessionsDoNotInterfere_ForToolToggles()
    {
        // Arrange
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var toolId = Guid.NewGuid();

        // Act - disable tool only for session1
        await _sut.SetToolEnabledAsync(session1, toolId, false);

        // Assert
        _sut.GetDisabledToolIdsCached(session1).Should().Contain(toolId);
        _sut.GetDisabledToolIdsCached(session2).Should().NotContain(toolId);
    }

    [Fact]
    public async Task NullSessionId_UsesTemporaryStorage()
    {
        // Arrange
        var toolId = Guid.NewGuid();

        // Act
        await _sut.SetToolEnabledAsync(null, toolId, false);

        // Assert
        var result = _sut.GetDisabledToolIdsCached(null);
        result.Should().Contain(toolId);

        // Should not persist to localStorage for null session
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object[]>()),
            Times.Never);
    }

    #endregion

    #region Think Level Tests

    [Fact]
    public async Task GetThinkLevelAsync_ReturnsNull_WhenNothingStored()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.GetThinkLevelAsync(sessionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetThinkLevelAsync_ReturnsStoredLevel_WhenDataExists()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(ThinkValue.High);

        // Act
        var result = await _sut.GetThinkLevelAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ThinkValue.High);
    }

    [Fact]
    public async Task SetThinkLevelAsync_PersistsToLocalStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.Medium);
        var expectedKey = $"aesir_think_level_{sessionId}";

        // Act
        await _sut.SetThinkLevelAsync(sessionId, level);

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>(
                "localStorage.setItem",
                It.Is<object[]>(args =>
                    args.Length == 2 &&
                    args[0].ToString() == expectedKey &&
                    args[1].ToString() == ThinkValue.Medium)),
            Times.Once);
    }

    [Fact]
    public async Task SetThinkLevelAsync_RaisesOnThinkLevelChanged_WhenStateChanges()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.High);
        var eventRaised = false;
        _sut.OnThinkLevelChanged += () => eventRaised = true;

        // Act
        await _sut.SetThinkLevelAsync(sessionId, level);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task SetThinkLevelAsync_DoesNotRaiseEvent_WhenStateUnchanged()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.High);

        // Set initial level
        await _sut.SetThinkLevelAsync(sessionId, level);

        var eventRaised = false;
        _sut.OnThinkLevelChanged += () => eventRaised = true;

        // Act - set same level again
        await _sut.SetThinkLevelAsync(sessionId, level);

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public async Task ClearThinkLevelAsync_RemovesLevel()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.Low);

        await _sut.SetThinkLevelAsync(sessionId, level);
        _sut.GetThinkLevelCached(sessionId).Should().NotBeNull();

        // Act
        await _sut.ClearThinkLevelAsync(sessionId);

        // Assert
        _sut.GetThinkLevelCached(sessionId).Should().BeNull();
    }

    [Fact]
    public async Task ClearThinkLevelAsync_RemovesFromLocalStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.High);
        var expectedKey = $"aesir_think_level_{sessionId}";

        await _sut.SetThinkLevelAsync(sessionId, level);

        // Act
        await _sut.ClearThinkLevelAsync(sessionId);

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args =>
                    args.Length == 1 &&
                    args[0].ToString() == expectedKey)),
            Times.Once);
    }

    [Fact]
    public async Task GetThinkLevelCached_ReturnsCachedValue_WithoutLocalStorageCall()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(ThinkValue.Medium);

        // Pre-populate cache
        await _sut.SetThinkLevelAsync(sessionId, level);
        _mockJsRuntime.Invocations.Clear();

        // Act
        var result = _sut.GetThinkLevelCached(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ThinkValue.Medium);
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task MultipleSessionsDoNotInterfere_ForThinkLevel()
    {
        // Arrange
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var level1 = new ThinkValue(ThinkValue.High);
        var level2 = new ThinkValue(ThinkValue.Low);

        // Act
        await _sut.SetThinkLevelAsync(session1, level1);
        await _sut.SetThinkLevelAsync(session2, level2);

        // Assert
        _sut.GetThinkLevelCached(session1)!.Value.ToString().Should().Be(ThinkValue.High);
        _sut.GetThinkLevelCached(session2)!.Value.ToString().Should().Be(ThinkValue.Low);
    }

    [Fact]
    public async Task NullSessionId_UsesTemporaryStorage_ForThinkLevel()
    {
        // Arrange
        var level = new ThinkValue(ThinkValue.Medium);

        // Act
        await _sut.SetThinkLevelAsync(null, level);

        // Assert
        var result = _sut.GetThinkLevelCached(null);
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ThinkValue.Medium);

        // Should not persist to localStorage for null session
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task SetThinkLevelAsync_ParsesBooleanFalse()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var level = new ThinkValue(false);

        // Act
        await _sut.SetThinkLevelAsync(sessionId, level);

        // Assert
        var result = _sut.GetThinkLevelCached(sessionId);
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be("false");
    }

    [Fact]
    public async Task GetThinkLevelAsync_ParsesBooleanFalseFromStorage()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync("false");

        // Act
        var result = await _sut.GetThinkLevelAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be("false");
    }

    #endregion
}

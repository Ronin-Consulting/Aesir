using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.HandsFree.Models;
using Aesir.Client.Web.Modules.HandsFree.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.HandsFree.Services;

public class HandsFreeServiceTests
{
    private readonly Mock<IAudioCaptureService> _mockAudioCapture;
    private readonly Mock<IAudioPlaybackService> _mockAudioPlayback;
    private readonly Mock<ISignalRSpeechService> _mockSpeechService;
    private readonly Mock<IChatApiService> _mockChatApiService;
    private readonly Mock<IChatSessionNotifier> _mockChatSessionNotifier;
    private readonly HandsFreeService _sut;

    public HandsFreeServiceTests()
    {
        _mockAudioCapture = new Mock<IAudioCaptureService>();
        _mockAudioPlayback = new Mock<IAudioPlaybackService>();
        _mockSpeechService = new Mock<ISignalRSpeechService>();
        _mockChatApiService = new Mock<IChatApiService>();
        _mockChatSessionNotifier = new Mock<IChatSessionNotifier>();

        _sut = new HandsFreeService(
            _mockAudioCapture.Object,
            _mockAudioPlayback.Object,
            _mockSpeechService.Object,
            _mockChatApiService.Object,
            _mockChatSessionNotifier.Object);
    }

    [Fact]
    public void Constructor_InitializesWithIdleState()
    {
        // Assert
        _sut.State.Should().Be(HandsFreeState.Idle);
    }

    [Fact]
    public void Constructor_InitializesWithNoConversationId()
    {
        // Assert
        _sut.CurrentConversationId.Should().BeNull();
    }

    [Fact]
    public void Constructor_InitializesWithHasExchangedMessagesFalse()
    {
        // Assert
        _sut.HasExchangedMessages.Should().BeFalse();
    }

    [Fact]
    public void CurrentAgentId_CanBeSetAndRetrieved()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        // Act
        _sut.CurrentAgentId = agentId;

        // Assert
        _sut.CurrentAgentId.Should().Be(agentId);
    }

    [Fact]
    public void CurrentConversationId_CanBeSetAndRetrieved()
    {
        // Arrange
        var conversationId = Guid.NewGuid();

        // Act
        _sut.CurrentConversationId = conversationId;

        // Assert
        _sut.CurrentConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsFalse_WhenAudioCaptureInitFails()
    {
        // Arrange
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(false);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(true);

        // Act
        var result = await _sut.InitializeAsync();

        // Assert
        result.Should().BeFalse();
        _sut.State.Should().Be(HandsFreeState.Error);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsFalse_WhenAudioPlaybackInitFails()
    {
        // Arrange
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(false);

        // Act
        var result = await _sut.InitializeAsync();

        // Assert
        result.Should().BeFalse();
        _sut.State.Should().Be(HandsFreeState.Error);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsFalse_WhenSpeechServiceConnectFails()
    {
        // Arrange
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockSpeechService.Setup(x => x.ConnectAsync()).ReturnsAsync(false);

        // Act
        var result = await _sut.InitializeAsync();

        // Assert
        result.Should().BeFalse();
        _sut.State.Should().Be(HandsFreeState.Error);
    }

    [Fact]
    public async Task InitializeAsync_ReturnsTrue_WhenAllServicesInitialize()
    {
        // Arrange
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockSpeechService.Setup(x => x.ConnectAsync()).ReturnsAsync(true);

        // Act
        var result = await _sut.InitializeAsync();

        // Assert
        result.Should().BeTrue();
        _sut.State.Should().Be(HandsFreeState.Idle);
    }

    [Fact]
    public async Task ResetAsync_ClearsErrorState()
    {
        // Arrange
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(false);
        await _sut.InitializeAsync(); // This will set Error state
        _sut.State.Should().Be(HandsFreeState.Error);

        // Act
        await _sut.ResetAsync();

        // Assert
        _sut.State.Should().Be(HandsFreeState.Idle);
        _sut.LastError.Should().BeNull();
    }

    [Fact]
    public async Task InterruptAsync_SetsStateToIdle()
    {
        // Arrange - initialize first
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockSpeechService.Setup(x => x.ConnectAsync()).ReturnsAsync(true);
        await _sut.InitializeAsync();

        // Act
        await _sut.InterruptAsync();

        // Assert
        _sut.State.Should().Be(HandsFreeState.Idle);
    }
}

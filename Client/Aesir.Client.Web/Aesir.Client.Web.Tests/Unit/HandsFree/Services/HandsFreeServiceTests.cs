using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.HandsFree.Models;
using Aesir.Client.Web.Modules.HandsFree.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Web.Tests.Unit.HandsFree.Services;

public class HandsFreeServiceTests
{
    private readonly Mock<IAudioCaptureService> _mockAudioCapture;
    private readonly Mock<IAudioPlaybackService> _mockAudioPlayback;
    private readonly Mock<ISignalRSpeechService> _mockSpeechService;
    private readonly Mock<IChatApiService> _mockChatApiService;
    private readonly Mock<IChatSessionNotifier> _mockChatSessionNotifier;
    private readonly Mock<IChatPreferencesService> _mockChatPreferencesService;
    private readonly Mock<IAgentToolsService> _mockAgentToolsService;
    private readonly Mock<IConfigurationApiService> _mockConfigurationApiService;
    private readonly HandsFreeService _sut;

    public HandsFreeServiceTests()
    {
        _mockAudioCapture = new Mock<IAudioCaptureService>();
        _mockAudioPlayback = new Mock<IAudioPlaybackService>();
        _mockSpeechService = new Mock<ISignalRSpeechService>();
        _mockChatApiService = new Mock<IChatApiService>();
        _mockChatSessionNotifier = new Mock<IChatSessionNotifier>();
        _mockChatPreferencesService = new Mock<IChatPreferencesService>();
        _mockAgentToolsService = new Mock<IAgentToolsService>();
        _mockConfigurationApiService = new Mock<IConfigurationApiService>();

        // Setup default behavior for preferences service
        _mockChatPreferencesService
            .Setup(x => x.GetDisabledToolIdsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new HashSet<Guid>());
        _mockChatPreferencesService
            .Setup(x => x.GetThinkLevelAsync(It.IsAny<Guid?>()))
            .ReturnsAsync((ThinkValue?)null);

        // Setup default behavior for agent tools service
        _mockAgentToolsService
            .Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AesirToolBase>());
        _mockAgentToolsService
            .Setup(x => x.GetAgentToolRequestsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ToolRequest>());

        _sut = new HandsFreeService(
            _mockAudioCapture.Object,
            _mockAudioPlayback.Object,
            _mockSpeechService.Object,
            _mockChatApiService.Object,
            _mockChatSessionNotifier.Object,
            _mockChatPreferencesService.Object,
            _mockAgentToolsService.Object,
            _mockConfigurationApiService.Object);
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

    [Fact]
    public async Task DeactivateAsync_ClearsConversationState()
    {
        // Arrange - set up initial state
        var agentId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        _sut.CurrentAgentId = agentId;
        _sut.CurrentConversationId = conversationId;

        // Initialize services
        _mockAudioCapture.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockAudioPlayback.Setup(x => x.InitializeAsync()).ReturnsAsync(true);
        _mockSpeechService.Setup(x => x.ConnectAsync()).ReturnsAsync(true);
        await _sut.InitializeAsync();

        // Act
        await _sut.DeactivateAsync();

        // Assert - verify all conversation state is cleared
        _sut.CurrentAgentId.Should().BeNull();
        _sut.CurrentConversationId.Should().BeNull();
        _sut.CurrentSessionTitle.Should().BeNull();
        _sut.HasExchangedMessages.Should().BeFalse();
    }

    [Fact]
    public void ConfigurationApiService_CanBeVerifiedThroughSetup()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = new AesirAgentBase
        {
            Id = agentId,
            Name = "Test Agent",
            ChatPromptPersona = PromptPersona.Business
        };

        _mockConfigurationApiService
            .Setup(x => x.GetAgentAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirAgentBase>.Success(agent));

        // Act - just verify the mock can be set up and returns expected value
        var result = _mockConfigurationApiService.Object.GetAgentAsync(agentId, CancellationToken.None).Result;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ChatPromptPersona.Should().Be(PromptPersona.Business);
    }

    [Fact]
    public void AesirChatMessage_NewSystemMessage_CreatesMessageWithPromptTemplate()
    {
        // Arrange - test that system message creation works correctly with Business persona
        var persona = PromptPersona.Business;

        // Act
        var systemMessage = AesirChatMessage.NewSystemMessage(persona, null);

        // Assert
        systemMessage.Should().NotBeNull();
        systemMessage.Role.Should().Be("system");
        // The Business prompt template should contain the {{currentDateTime}} placeholder
        systemMessage.Content.Should().Contain("{{currentDateTime}}");
    }

    [Fact]
    public void AesirChatMessage_NewSystemMessage_CreatesMessageWithCustomContent()
    {
        // Arrange - test custom persona with custom content
        var persona = PromptPersona.Custom;
        var customContent = "You are a custom assistant. Today's date is {{currentDateTime}}.";

        // Act
        var systemMessage = AesirChatMessage.NewSystemMessage(persona, customContent);

        // Assert
        systemMessage.Should().NotBeNull();
        systemMessage.Role.Should().Be("system");
        systemMessage.Content.Should().Be(customContent);
        systemMessage.Content.Should().Contain("{{currentDateTime}}");
    }

    [Fact]
    public void AesirChatMessage_NewSystemMessage_MilitaryPersonaContainsDateTimePlaceholder()
    {
        // Arrange
        var persona = PromptPersona.Military;

        // Act
        var systemMessage = AesirChatMessage.NewSystemMessage(persona, null);

        // Assert
        systemMessage.Should().NotBeNull();
        systemMessage.Role.Should().Be("system");
        // The Military prompt template should contain the {{currentDateTime}} placeholder
        systemMessage.Content.Should().Contain("{{currentDateTime}}");
    }
}

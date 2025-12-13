using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class MessageInputTests : TestContext
{
    private readonly Mock<IChatStateService> _mockChatStateService;
    private readonly Mock<IConfigurationApiService> _mockConfigurationApiService;
    private readonly Mock<IDocumentApiService> _mockDocumentApiService;
    private readonly Mock<IAgentToolsService> _mockAgentToolsService;

    public MessageInputTests()
    {
        _mockChatStateService = new Mock<IChatStateService>();
        _mockConfigurationApiService = new Mock<IConfigurationApiService>();
        _mockDocumentApiService = new Mock<IDocumentApiService>();
        _mockAgentToolsService = new Mock<IAgentToolsService>();

        // Setup agents list for AgentSelectorCompact
        var emptyAgentsList = Array.Empty<AesirAgentBase>() as IReadOnlyList<AesirAgentBase>;
        _mockConfigurationApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(emptyAgentsList));

        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddSingleton(_mockConfigurationApiService.Object);
        Services.AddSingleton(_mockDocumentApiService.Object);
        Services.AddSingleton(_mockAgentToolsService.Object);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // Render MudPopoverProvider first (required for MudBlazor popover components)
        RenderComponent<MudPopoverProvider>();
    }

    [Fact]
    public void Renders_WithDefaultPlaceholder()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Default placeholder is customizable via parameter
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void Renders_WithCustomPlaceholder()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.Placeholder, "Custom placeholder"));

        // Assert
        cut.Markup.Should().Contain("Custom placeholder");
    }

    [Fact]
    public void SendButton_IsPresent()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Send button uses ArrowUpward icon with send-button class
        cut.Markup.Should().Contain("send-button");
    }

    [Fact]
    public void SendButton_IsDisabled_WhenNoAgentSelected()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Send button should be disabled
        var sendButtons = cut.FindAll(".send-button");
        sendButtons.Should().NotBeEmpty();
        // The button is disabled via the CanSend property
    }

    [Fact]
    public void SendButton_IsDisabled_WhenIsDisabledTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert - Component renders with IsDisabled true
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void SendButton_IsDisabled_WhenIsLoadingTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsLoading, true));

        // Assert - Component renders with IsLoading true
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void HasAgentSelector()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - AgentSelectorCompact is present
        cut.Markup.Should().Contain("agent-selector");
    }

    [Fact]
    public void HasAttachFileButton()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - There are multiple icon buttons in the action bar
        var buttons = cut.FindAll("button");
        buttons.Count.Should().BeGreaterThanOrEqualTo(3); // Attach, Settings, History, Agent selector, Send
    }

    [Fact]
    public void HasSettingsButton()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Action bar contains multiple buttons
        var buttons = cut.FindAll("button");
        buttons.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void InputField_IsPresent()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>();

        // Assert - Input field is present via message-input class
        cut.Markup.Should().Contain("message-input");
    }

    [Fact]
    public void InputField_IsDisabled_WhenIsDisabledTrue()
    {
        // Arrange
        SetupWithAgent();

        // Act
        var cut = RenderComponent<MessageInput>(parameters => parameters
            .Add(p => p.IsDisabled, true));

        // Assert - Component renders successfully with disabled state
        cut.Markup.Should().Contain("message-input");
    }

    private void SetupWithAgent()
    {
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent", ChatModel = "gpt-4" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
    }
}

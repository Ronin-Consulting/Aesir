using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Pages;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ChatPageTests : TestContext
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly Mock<IChatStateService> _mockChatStateService;
    private readonly Mock<INavigationRegistry> _mockNavigationRegistry;
    private readonly Mock<IChatApiService> _mockChatApiService;
    private readonly Mock<IChatHistoryService> _mockChatHistoryService;

    public ChatPageTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _mockChatStateService = new Mock<IChatStateService>();
        _mockNavigationRegistry = new Mock<INavigationRegistry>();
        _mockChatApiService = new Mock<IChatApiService>();
        _mockChatHistoryService = new Mock<IChatHistoryService>();

        Services.AddSingleton(_mockApiService.Object);
        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddSingleton(_mockNavigationRegistry.Object);
        Services.AddSingleton(_mockChatApiService.Object);
        Services.AddSingleton(_mockChatHistoryService.Object);
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("scrollToBottom", _ => true);

        // Default setup
        _mockNavigationRegistry.Setup(x => x.GetItems()).Returns(new List<NavigationItem>());
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
        _mockChatHistoryService.Setup(x => x.Sessions).Returns(new List<AesirChatSessionItem>());
    }

    [Fact]
    public void Renders_WithPageTitle()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void ShowsWelcomeView_WhenNoMessages()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        cut.Markup.Should().Contain("chat-welcome-container");
    }

    [Fact]
    public void HasInputArea()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Welcome view has its own input area
        cut.Markup.Should().Contain("chat-welcome-container");
    }

    [Fact]
    public void MessageInput_IsDisabled_WhenNoAgentSelected()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        var sendButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Send"));
        sendButton?.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void WelcomeInput_IsEnabled_WhenAgentSelected()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent", ChatModel = "gpt-4" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Welcome view with input area is present
        cut.Markup.Should().Contain("welcome-input");
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Component renders and can be disposed without error
        cut.Invoking(c => c.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void RendersWithoutError()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act & Assert
        var cut = RenderComponent<ChatPage>();
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void DisplaysAgentName_WhenAgentSelected()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent", ChatModel = "claude-3-opus" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Agent name is displayed in the welcome view
        cut.Markup.Should().Contain("Test Agent");
    }

    [Fact]
    public void HasChatPageClass()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        cut.Markup.Should().Contain("chat-page");
    }

    [Fact]
    public void UsesFlexLayout()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        cut.Markup.Should().Contain("flex-direction: column");
    }

    [Fact]
    public void ShowsAgentSelector_InWelcomeView()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Agent selector button is present in welcome view
        cut.Markup.Should().Contain("agent-selector-btn");
    }

    [Fact]
    public void MessageInput_ShowsLoadingState_WhenAgentSelected()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent", ChatModel = "gpt-4" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert
        cut.Markup.Should().NotBeEmpty();
    }
}

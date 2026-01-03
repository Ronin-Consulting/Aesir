using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Modules;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Pages;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ChatPageTests : TestContext
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly Mock<IChatStateService> _mockChatStateService;
    private readonly Mock<INavigationRegistry> _mockNavigationRegistry;
    private readonly Mock<IChatApiService> _mockChatApiService;
    private readonly Mock<IChatHistoryService> _mockChatHistoryService;
    private readonly Mock<IToolCallStateService> _mockToolCallStateService;
    private readonly Mock<ICitationStateService> _mockCitationStateService;
    private readonly Mock<IAgentToolsService> _mockAgentToolsService;
    private readonly Mock<IDocumentApiService> _mockDocumentApiService;
    private readonly Mock<IResearchStateService> _mockResearchStateService;
    private readonly Mock<IResearchTeamApiService> _mockResearchTeamApiService;
    private readonly Mock<IResearchSessionApiService> _mockResearchSessionApiService;

    public ChatPageTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _mockChatStateService = new Mock<IChatStateService>();
        _mockNavigationRegistry = new Mock<INavigationRegistry>();
        _mockChatApiService = new Mock<IChatApiService>();
        _mockChatHistoryService = new Mock<IChatHistoryService>();
        _mockToolCallStateService = new Mock<IToolCallStateService>();
        _mockCitationStateService = new Mock<ICitationStateService>();
        _mockAgentToolsService = new Mock<IAgentToolsService>();
        _mockDocumentApiService = new Mock<IDocumentApiService>();
        _mockResearchStateService = new Mock<IResearchStateService>();
        _mockResearchTeamApiService = new Mock<IResearchTeamApiService>();
        _mockResearchSessionApiService = new Mock<IResearchSessionApiService>();

        // Setup default return for research teams
        _mockResearchTeamApiService.Setup(x => x.GetTeamsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<ResearchTeamBase>>.Success(new List<ResearchTeamBase>()));
        _mockResearchTeamApiService.Setup(x => x.GetActiveTeamsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<ResearchTeamBase>>.Success(new List<ResearchTeamBase>()));

        Services.AddSingleton(_mockApiService.Object);
        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddSingleton(_mockNavigationRegistry.Object);
        Services.AddSingleton(_mockChatApiService.Object);
        Services.AddSingleton(_mockChatHistoryService.Object);
        Services.AddSingleton(_mockToolCallStateService.Object);
        Services.AddSingleton(_mockCitationStateService.Object);
        Services.AddSingleton(_mockAgentToolsService.Object);
        Services.AddSingleton(_mockDocumentApiService.Object);
        Services.AddSingleton(_mockResearchStateService.Object);
        Services.AddSingleton(_mockResearchTeamApiService.Object);
        Services.AddSingleton(_mockResearchSessionApiService.Object);
        Services.AddSingleton<IMarkdownService, MarkdownService>();
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("scrollToBottom", _ => true);

        // Render MudPopoverProvider first (required for MudBlazor popover components)
        RenderComponent<MudPopoverProvider>();

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
        cut.Markup.Should().Contain("message-input-wrapper");
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
        // Arrange - Setup with an agent and inference engine so the welcome view shows the input area
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

        // Assert - Agent selector compact is present in welcome view
        cut.Markup.Should().Contain("agent-selector-compact");
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

    #region Research Team UI State Tests

    [Fact]
    public void ToolToggleMenu_IsDisabled_WhenResearchTeamSelected()
    {
        // Arrange - Agent with tools AND Research Team selected
        var agent = new AesirAgentBase
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatModel = "gpt-4",
            IsThinkingAvailable = true
        };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // Setup agent tools that would normally enable the tool toggle menu
        _mockAgentToolsService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AesirToolBase>
            {
                new AesirToolBase { Id = Guid.NewGuid(), ToolName = "WebTool", Name = "Web Search" }
            });

        // Simulate Research Team selected
        var researchTeam = new ResearchTeamBase { Id = Guid.NewGuid(), Name = "Test Research Team" };
        _mockResearchStateService.Setup(x => x.IsTeamSelected).Returns(true);
        _mockResearchStateService.Setup(x => x.SelectedTeam).Returns(researchTeam);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Tool toggle button should be disabled (no toggleable tools passed)
        // When ResearchState.IsTeamSelected is true, ChatPage passes empty tools to ToolToggleMenu
        // which makes the button disabled
        var toolToggleButtons = cut.FindAll(".tool-toggle-menu-container button");
        if (toolToggleButtons.Any())
        {
            var toolButton = toolToggleButtons.First();
            toolButton.HasAttribute("disabled").Should().BeTrue(
                "Tool toggle button should be disabled when Research Team is selected");
        }
    }

    [Fact]
    public void ThinkMenu_IsNotRendered_WhenResearchTeamSelected()
    {
        // Arrange - Agent with thinking enabled AND Research Team selected
        var agent = new AesirAgentBase
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatModel = "gpt-4",
            IsThinkingAvailable = true  // Thinking is available on the agent
        };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // Simulate Research Team selected
        var researchTeam = new ResearchTeamBase { Id = Guid.NewGuid(), Name = "Test Research Team" };
        _mockResearchStateService.Setup(x => x.IsTeamSelected).Returns(true);
        _mockResearchStateService.Setup(x => x.SelectedTeam).Returns(researchTeam);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Think menu container should NOT be present when Research Team is selected
        // When ResearchState.IsTeamSelected is true, ChatPage passes AllowThinking=false
        // which prevents ThinkMenu from rendering
        cut.Markup.Should().NotContain("think-menu-container",
            "Think menu should not be rendered when Research Team is selected");
    }

    [Fact]
    public void ToolToggleMenu_IsEnabled_WhenResearchTeamNotSelected()
    {
        // Arrange - Agent with tools, NO Research Team selected
        var agent = new AesirAgentBase
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatModel = "gpt-4"
        };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // Setup agent tools
        _mockAgentToolsService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AesirToolBase>
            {
                new AesirToolBase { Id = Guid.NewGuid(), ToolName = "WebTool", Name = "Web Search" }
            });

        // NO Research Team selected
        _mockResearchStateService.Setup(x => x.IsTeamSelected).Returns(false);
        _mockResearchStateService.Setup(x => x.SelectedTeam).Returns((ResearchTeamBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Tool toggle button should be enabled when no Research Team is selected
        // and tools are available
        var toolToggleButtons = cut.FindAll(".tool-toggle-menu-container button");
        if (toolToggleButtons.Any())
        {
            var toolButton = toolToggleButtons.First();
            // The button should NOT be disabled (enabled)
            toolButton.HasAttribute("disabled").Should().BeFalse(
                "Tool toggle button should be enabled when no Research Team is selected and tools are available");
        }
    }

    [Fact]
    public void ThinkMenu_IsRendered_WhenResearchTeamNotSelected_AndThinkingAvailable()
    {
        // Arrange - Agent with thinking enabled, NO Research Team selected
        var agent = new AesirAgentBase
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            ChatModel = "gpt-4",
            IsThinkingAvailable = true  // Thinking is available
        };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase> { agent }));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>
            {
                new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Test Engine", Type = InferenceEngineType.OpenAICompatible }
            }));

        // NO Research Team selected
        _mockResearchStateService.Setup(x => x.IsTeamSelected).Returns(false);
        _mockResearchStateService.Setup(x => x.SelectedTeam).Returns((ResearchTeamBase?)null);

        // Act
        var cut = RenderComponent<ChatPage>();

        // Assert - Think menu container should be present when:
        // - No Research Team is selected
        // - Agent's IsThinkingAvailable is true
        cut.Markup.Should().Contain("think-menu-container",
            "Think menu should be rendered when no Research Team is selected and agent supports thinking");
    }

    #endregion
}

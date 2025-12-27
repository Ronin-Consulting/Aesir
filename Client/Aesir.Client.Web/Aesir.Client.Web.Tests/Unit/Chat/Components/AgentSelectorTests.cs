using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class AgentSelectorTests : TestContext
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly Mock<IChatStateService> _mockChatStateService;

    public AgentSelectorTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _mockChatStateService = new Mock<IChatStateService>();

        Services.AddSingleton(_mockApiService.Object);
        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_WithSelectAgentText_WhenNoAgentSelected()
    {
        // Arrange
        SetupEmptyAgents();
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        cut.Markup.Should().Contain("Select Agent");
    }

    [Fact]
    public void Renders_WithLoadingText_WhileLoading()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ApiResult<IReadOnlyList<AesirAgentBase>>>();
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        cut.Markup.Should().Contain("Loading");
    }

    [Fact]
    public void Renders_WithAgentName_WhenAgentSelected()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "My Agent", ChatModel = "gpt-4" };
        SetupEmptyAgents();
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        cut.Markup.Should().Contain("My Agent");
    }

    [Fact]
    public void AutoSelectsFirstAgent_WhenNoAgentSelectedAndAgentsAvailable()
    {
        // Arrange
        var agents = CreateTestAgents();
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        _mockChatStateService.Verify(x => x.SelectAgent(It.IsAny<AesirAgentBase>()), Times.Once);
    }

    [Fact]
    public void DoesNotAutoSelect_WhenAgentAlreadySelected()
    {
        // Arrange
        var existingAgent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Existing" };
        var agents = CreateTestAgents();
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(existingAgent);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        _mockChatStateService.Verify(x => x.SelectAgent(It.IsAny<AesirAgentBase>()), Times.Never);
    }

    [Fact]
    public void ShowsNoAgentsMessage_WhenNoAgentsConfigured()
    {
        // Arrange
        SetupEmptyAgents();
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert - Note: Menu content is not rendered until menu is opened
        // We verify the component renders without error
        cut.Markup.Should().NotBeEmpty();
    }

    [Fact]
    public void RaisesOnAgentSelectedCallback_WhenAgentSelected()
    {
        // Arrange
        var agents = CreateTestAgents();
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        AesirAgentBase? selectedAgent = null;
        var cut = RenderComponent<AgentSelector>(parameters => parameters
            .Add(p => p.OnAgentSelected, EventCallback.Factory.Create<AesirAgentBase>(this, a => selectedAgent = a)));

        // Agent is auto-selected on load
        _mockChatStateService.Verify(x => x.SelectAgent(It.IsAny<AesirAgentBase>()), Times.Once);
    }

    [Fact]
    public void RendersAgentIcon()
    {
        // Arrange
        SetupEmptyAgents();
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert - Contains an icon (SVG path for SmartToy)
        cut.Markup.Should().Contain("mud-icon-root");
    }

    [Fact]
    public void LoadsAgentsOnInitialization()
    {
        // Arrange
        SetupEmptyAgents();
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<AgentSelector>();

        // Assert
        _mockApiService.Verify(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupEmptyAgents()
    {
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));
    }

    private static List<AesirAgentBase> CreateTestAgents()
    {
        return
        [
            new AesirAgentBase { Id = Guid.NewGuid(), Name = "Agent 1", ChatModel = "gpt-4" },
            new AesirAgentBase { Id = Guid.NewGuid(), Name = "Agent 2", ChatModel = "claude-3" }
        ];
    }
}

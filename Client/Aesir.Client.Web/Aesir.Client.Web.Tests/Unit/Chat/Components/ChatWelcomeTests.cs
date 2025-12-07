using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class ChatWelcomeTests : TestContext
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly Mock<IChatStateService> _mockChatStateService;

    public ChatWelcomeTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _mockChatStateService = new Mock<IChatStateService>();

        Services.AddSingleton(_mockApiService.Object);
        Services.AddSingleton(_mockChatStateService.Object);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // Default setup - has agents and inference engines
        var agents = new List<AesirAgentBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Test Agent" }
        };
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(
                new List<AesirInferenceEngineBase> { new() { Id = Guid.NewGuid(), Name = "Engine" } }));
    }

    [Fact]
    public void Renders_WithRandomGreeting()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert - Contains one of the random greetings (check for common words)
        var markup = cut.Markup;
        var hasGreeting = markup.Contains("help") ||
                         markup.Contains("Ready") ||
                         markup.Contains("Let's") ||
                         markup.Contains("What") ||
                         markup.Contains("Here to");
        hasGreeting.Should().BeTrue("component should display a greeting");
    }

    [Fact]
    public void ShowsLoadingState_Initially()
    {
        // Arrange - Delay the API response
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(1000);
                return ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>());
            });

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.Markup.Should().Contain("Loading");
    }

    [Fact]
    public void ShowsSetupRequired_WhenNoAgents()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Setup Required"));
        cut.Markup.Should().Contain("Setup Required");
    }

    [Fact]
    public void ShowsSetupRequired_WhenNoInferenceEngines()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(
                new List<AesirInferenceEngineBase>()));

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Setup Required"));
        cut.Markup.Should().Contain("inference engine");
    }

    [Fact]
    public void ShowsApiError_WhenConnectionFails()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Failure("Connection failed"));

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Unable to Connect"));
        cut.Markup.Should().Contain("Unable to Connect");
    }

    [Fact]
    public void IncludesAgentSelector_WhenAgentsAvailable()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "My Agent" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert - Agent selector shows selected agent name
        cut.WaitForState(() => cut.Markup.Contains("My Agent"));
        cut.Markup.Should().Contain("My Agent");
    }

    [Fact]
    public void ShowsInputPlaceholder()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("How can I help you today?"));
        cut.Markup.Should().Contain("How can I help you today?");
    }

    [Fact]
    public void DisablesInput_WhenNoAgentSelected()
    {
        // Arrange
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert - Input should be disabled
        cut.WaitForState(() => !cut.Markup.Contains("Loading"));
        var input = cut.Find("input, textarea");
        input.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task RaisesOnAgentSelectedCallback_WhenAgentSelected()
    {
        // Arrange
        var agents = new List<AesirAgentBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Agent 1" }
        };
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns((AesirAgentBase?)null);

        // Act
        _ = RenderComponent<ChatWelcome>(parameters => parameters
            .Add(p => p.OnAgentSelected, EventCallback.Factory.Create<AesirAgentBase>(this, _ => { })));

        // Assert - ChatState.SelectAgent should be called for auto-selection
        await Task.Delay(100); // Allow async initialization
        _mockChatStateService.Verify(x => x.SelectAgent(It.IsAny<AesirAgentBase>()), Times.Once);
    }

    [Fact]
    public void HasCenteredLayout()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.Markup.Should().Contain("chat-welcome-container");
    }

    [Fact]
    public void ShowsBlueAesirLogo()
    {
        // Arrange
        var agent = new AesirAgentBase { Id = Guid.NewGuid(), Name = "Test Agent" };
        _mockChatStateService.Setup(x => x.SelectedAgent).Returns(agent);

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("logo-blue.svg"));
        cut.Markup.Should().Contain("logo-blue.svg");
    }

    [Fact]
    public void ShowsSetupWizardButton_WhenSetupRequired()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetAgentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));

        // Act
        var cut = RenderComponent<ChatWelcome>();

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Setup Wizard"));
        cut.Markup.Should().Contain("Setup Wizard");
    }
}

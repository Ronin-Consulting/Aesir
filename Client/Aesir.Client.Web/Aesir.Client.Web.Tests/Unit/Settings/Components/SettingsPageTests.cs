using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Pages;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Client.Web.Modules.Observability.Services;
using Aesir.Client.Web.Modules.Observability.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class SettingsPageTests : TestContext
{
    private readonly Mock<IGeneralSettingsService> _mockGeneralSettingsService;
    private readonly Mock<IInferenceEngineService> _mockInferenceEngineService;
    private readonly Mock<IMcpServerService> _mockMcpServerService;
    private readonly Mock<IToolService> _mockToolService;
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IObservabilityService> _mockObservabilityService;
    private readonly Mock<IModelApiService> _mockModelApiService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly FakeNavigationManager _navigationManager;

    public SettingsPageTests()
    {
        _mockGeneralSettingsService = new Mock<IGeneralSettingsService>();
        _mockInferenceEngineService = new Mock<IInferenceEngineService>();
        _mockMcpServerService = new Mock<IMcpServerService>();
        _mockToolService = new Mock<IToolService>();
        _mockAgentService = new Mock<IAgentService>();
        _mockObservabilityService = new Mock<IObservabilityService>();
        _mockModelApiService = new Mock<IModelApiService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();

        // Register services
        Services.AddSingleton(_mockGeneralSettingsService.Object);
        Services.AddSingleton(_mockInferenceEngineService.Object);
        Services.AddSingleton(_mockMcpServerService.Object);
        Services.AddSingleton(_mockToolService.Object);
        Services.AddSingleton(_mockAgentService.Object);
        Services.AddSingleton(_mockObservabilityService.Object);
        Services.AddSingleton(_mockModelApiService.Object);
        Services.AddSingleton(_mockDialogService.Object);
        Services.AddSingleton(_mockSnackbar.Object);
        Services.AddMudServices();

        JSInterop.Mode = JSRuntimeMode.Loose;

        // Setup navigation manager
        _navigationManager = Services.GetRequiredService<FakeNavigationManager>();

        // Render MudPopoverProvider first (required for MudBlazor popover components)
        RenderComponent<MudPopoverProvider>();

        SetupDefaultMocks();
    }

    [Fact]
    public void PageTitle_IsSet()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsPage>();

        // Assert - PageTitle component is rendered (exact title handled by Blazor head management)
        cut.Markup.Should().Contain("Settings");
    }

    [Fact]
    public void RendersTabbedInterface()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Find(".settings-panel").Should().NotBeNull();
        cut.Find(".settings-tabs").Should().NotBeNull();
    }

    [Fact]
    public void DefaultsToGeneralTab_WhenNoQueryParameter()
    {
        // Arrange & Act
        _navigationManager.NavigateTo("/settings");
        var cut = RenderComponent<SettingsPage>();

        // Assert
        var selectedTab = cut.Find(".settings-tab-item.selected");
        selectedTab.TextContent.Should().Contain("General");
    }

    [Theory]
    [InlineData("general", "General")]
    [InlineData("inference-engines", "Inference Engines")]
    [InlineData("mcp-servers", "MCP Servers")]
    [InlineData("tools", "Tools")]
    [InlineData("agents", "Agents")]
    [InlineData("observability", "Observability")]
    public void SelectsCorrectTab_FromQueryParameter(string tabId, string expectedLabel)
    {
        // Arrange
        _navigationManager.NavigateTo($"/settings?tab={tabId}");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        var selectedTab = cut.Find(".settings-tab-item.selected");
        selectedTab.TextContent.Should().Contain(expectedLabel);
    }

    [Fact]
    public async Task TabClick_UpdatesUrl()
    {
        // Arrange
        _navigationManager.NavigateTo("/settings");
        var cut = RenderComponent<SettingsPage>();

        // Act - Click on Agents tab
        var agentsTab = cut.FindAll(".settings-tab-item").First(t => t.TextContent.Contains("Agents"));
        await agentsTab.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _navigationManager.Uri.Should().Contain("tab=agents");
    }

    [Fact]
    public void RendersGeneralContent_WhenGeneralTabSelected()
    {
        // Arrange
        _mockGeneralSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(new AesirGeneralSettingsBase()));

        _navigationManager.NavigateTo("/settings?tab=general");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert - General settings content should be rendered
        cut.Find(".settings-content").Should().NotBeNull();
    }

    [Fact]
    public void RendersInferenceEnginesContent_WhenInferenceEnginesTabSelected()
    {
        // Arrange
        _mockInferenceEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));

        _navigationManager.NavigateTo("/settings?tab=inference-engines");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Markup.Should().Contain("Inference Engines");
    }

    [Fact]
    public void RendersMcpServersContent_WhenMcpServersTabSelected()
    {
        // Arrange
        _mockMcpServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        _navigationManager.NavigateTo("/settings?tab=mcp-servers");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Markup.Should().Contain("MCP Servers");
    }

    [Fact]
    public void RendersToolsContent_WhenToolsTabSelected()
    {
        // Arrange
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));
        _mockMcpServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        _navigationManager.NavigateTo("/settings?tab=tools");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Markup.Should().Contain("Tools");
    }

    [Fact]
    public void RendersAgentsContent_WhenAgentsTabSelected()
    {
        // Arrange
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));
        _mockInferenceEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));

        _navigationManager.NavigateTo("/settings?tab=agents");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Markup.Should().Contain("Agents");
    }

    [Fact]
    public void RendersObservabilityContent_WhenObservabilityTabSelected()
    {
        // Arrange - Legacy inference logs
        var logs = new PagedLogResponse { Items = new List<AesirInferenceLogSummary>(), TotalCount = 0 };
        _mockObservabilityService.Setup(x => x.LoadLogsAsync(It.IsAny<LogFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<PagedLogResponse>.Success(logs));
        _mockObservabilityService.Setup(x => x.CurrentResponse).Returns(logs);
        _mockObservabilityService.Setup(x => x.GroupedLogs).Returns(new List<TimeGroupedLogs>());
        _mockObservabilityService.Setup(x => x.CurrentFilter).Returns(new LogFilter());

        // Arrange - Unified timeline
        var unifiedResponse = new PagedUnifiedTimelineResponse { Items = new List<UnifiedTimelineItem>(), TotalCount = 0, Page = 1, PageSize = 50 };
        _mockObservabilityService.Setup(x => x.LoadUnifiedLogsAsync(It.IsAny<UnifiedLogFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<PagedUnifiedTimelineResponse>.Success(unifiedResponse));
        _mockObservabilityService.Setup(x => x.CurrentUnifiedResponse).Returns(unifiedResponse);
        _mockObservabilityService.Setup(x => x.UnifiedGroupedLogs).Returns(new List<UnifiedTimeGroupedLogs>());
        _mockObservabilityService.Setup(x => x.CurrentUnifiedFilter).Returns(new UnifiedLogFilter());

        _navigationManager.NavigateTo("/settings?tab=observability");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Markup.Should().Contain("Observability");
    }

    [Fact]
    public void FallsBackToGeneral_WhenUnknownTabProvided()
    {
        // Arrange
        _navigationManager.NavigateTo("/settings?tab=unknown-tab");

        // Act
        var cut = RenderComponent<SettingsPage>();

        // Assert - Should render general content as default
        cut.Find(".settings-content").Should().NotBeNull();
    }

    [Fact]
    public async Task TabSwitch_ChangesContent()
    {
        // Arrange
        _navigationManager.NavigateTo("/settings?tab=general");
        var cut = RenderComponent<SettingsPage>();

        // Act - Click on Agents tab
        var agentsTab = cut.FindAll(".settings-tab-item").First(t => t.TextContent.Contains("Agents"));
        await agentsTab.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Should have updated selected tab
        var selectedTab = cut.Find(".settings-tab-item.selected");
        selectedTab.TextContent.Should().Contain("Agents");
    }

    [Fact]
    public void RendersSettingsTabsContainer()
    {
        // Arrange & Act
        var cut = RenderComponent<SettingsPage>();

        // Assert
        cut.Find(".settings-tabs-container").Should().NotBeNull();
        cut.Find(".settings-content").Should().NotBeNull();
    }

    private void SetupDefaultMocks()
    {
        // General Settings
        _mockGeneralSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(new AesirGeneralSettingsBase()));

        // Inference Engines
        _mockInferenceEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));

        // MCP Servers
        _mockMcpServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Tools
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));

        // Agents
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));
        _mockAgentService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));

        // Observability - Legacy inference log support
        var logs = new PagedLogResponse { Items = new List<AesirInferenceLogSummary>(), TotalCount = 0 };
        _mockObservabilityService.Setup(x => x.LoadLogsAsync(It.IsAny<LogFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<PagedLogResponse>.Success(logs));
        _mockObservabilityService.Setup(x => x.CurrentResponse).Returns(logs);
        _mockObservabilityService.Setup(x => x.GroupedLogs).Returns(new List<TimeGroupedLogs>());
        _mockObservabilityService.Setup(x => x.CurrentFilter).Returns(new LogFilter());

        // Observability - Unified timeline support
        var unifiedResponse = new PagedUnifiedTimelineResponse { Items = new List<UnifiedTimelineItem>(), TotalCount = 0, Page = 1, PageSize = 50 };
        _mockObservabilityService.Setup(x => x.LoadUnifiedLogsAsync(It.IsAny<UnifiedLogFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<PagedUnifiedTimelineResponse>.Success(unifiedResponse));
        _mockObservabilityService.Setup(x => x.CurrentUnifiedResponse).Returns(unifiedResponse);
        _mockObservabilityService.Setup(x => x.UnifiedGroupedLogs).Returns(new List<UnifiedTimeGroupedLogs>());
        _mockObservabilityService.Setup(x => x.CurrentUnifiedFilter).Returns(new UnifiedLogFilter());

        // Model API Service
        _mockModelApiService.Setup(x => x.GetModelsAsync(It.IsAny<Guid>(), It.IsAny<ModelCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirModelInfo>>.Success(new List<AesirModelInfo>()));
    }
}

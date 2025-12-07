using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Pages;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class ToolsPageTests : TestContext
{
    private readonly Mock<IToolService> _mockToolService;
    private readonly Mock<IMcpServerService> _mockServerService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public ToolsPageTests()
    {
        _mockToolService = new Mock<IToolService>();
        _mockServerService = new Mock<IMcpServerService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();

        // Register services
        Services.AddSingleton(_mockToolService.Object);
        Services.AddSingleton(_mockServerService.Object);
        Services.AddSingleton(_mockDialogService.Object);
        Services.AddSingleton(_mockSnackbar.Object);
        Services.AddMudServices();

        // Setup JSInterop for MudBlazor components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void PageTitle_DisplaysCorrectText()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Find("h4").TextContent.Should().Contain("Tools");
    }

    [Fact]
    public void AddButton_IsPresent()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Add Internal Tool")).Should().BeTrue();
    }

    [Fact]
    public void DataGrid_DisplaysTools_WhenLoaded()
    {
        // Arrange
        var tools = CreateTestTools();
        var servers = CreateTestServers();
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Web Search Tool");
        cut.Markup.Should().Contain("MCP Tool");
    }

    [Fact]
    public void DataGrid_ShowsNoRecordsMessage_WhenEmpty()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("No tools configured");
    }

    [Fact]
    public void ErrorAlert_Shows_WhenLoadFails()
    {
        // Arrange
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Failure("Connection failed"));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Connection failed");
    }

    [Fact]
    public void TypeChip_DisplaysCorrectText_ForInternal()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Internal", Type = ToolType.Internal }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Internal");
    }

    [Fact]
    public void TypeChip_DisplaysCorrectText_ForMcpServer()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "MCP", Type = ToolType.McpServer }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("MCP Server");
    }

    [Fact]
    public void EditDeleteButtons_ArePresent_ForInternalTools()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Internal Tool", Type = ToolType.Internal }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        var editButtons = cut.FindAll("button.mud-icon-button.mud-primary-text");
        editButtons.Should().HaveCountGreaterThanOrEqualTo(1);
        var deleteButtons = cut.FindAll("button.mud-icon-button.mud-error-text");
        deleteButtons.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Description_DisplaysSubtitle()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Manage internal tools and view discovered MCP server tools");
    }

    [Fact]
    public void FilterChips_ArePresent()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Filter by type:");
        cut.Markup.Should().Contain("All");
    }

    [Fact]
    public void Services_CalledOnInitialization()
    {
        // Arrange
        SetupEmptyData();

        // Act
        _ = RenderComponent<ToolsPage>();

        // Assert
        _mockToolService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockServerService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ToolDescription_IsDisplayed()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Test", Description = "Test Description", Type = ToolType.Internal }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Test Description");
    }

    [Fact]
    public void McpServerTool_ShowsServerLink()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = serverId, Name = "My MCP Server" }
        };
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "MCP Tool", Type = ToolType.McpServer, McpServerId = serverId }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("My MCP Server");
    }

    [Fact]
    public void InternalTool_ShowsBuiltIn()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Internal Tool", Type = ToolType.Internal }
        };
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));

        // Act
        var cut = RenderComponent<ToolsPage>();

        // Assert
        cut.Markup.Should().Contain("Built-in");
    }

    private void SetupEmptyData()
    {
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));
    }

    private static List<AesirToolBase> CreateTestTools()
    {
        return
        [
            new AesirToolBase
            {
                Id = Guid.NewGuid(),
                Name = "Web Search Tool",
                Description = "Search the web",
                Type = ToolType.Internal,
                IconName = "Search"
            },
            new AesirToolBase
            {
                Id = Guid.NewGuid(),
                Name = "MCP Tool",
                Description = "A tool from MCP server",
                Type = ToolType.McpServer,
                McpServerId = Guid.NewGuid()
            }
        ];
    }

    private static List<AesirMcpServerBase> CreateTestServers()
    {
        return
        [
            new AesirMcpServerBase
            {
                Id = Guid.NewGuid(),
                Name = "Test MCP Server"
            }
        ];
    }
}

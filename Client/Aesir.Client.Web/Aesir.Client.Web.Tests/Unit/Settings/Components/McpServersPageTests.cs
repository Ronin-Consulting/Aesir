using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Pages;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class McpServersPageTests : TestContext
{
    private readonly Mock<IMcpServerService> _mockServerService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public McpServersPageTests()
    {
        _mockServerService = new Mock<IMcpServerService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();

        // Register services
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
        SetupEmptyServers();

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Find("h4").TextContent.Should().Contain("MCP Servers");
    }

    [Fact]
    public void AddButton_IsPresent()
    {
        // Arrange
        SetupEmptyServers();

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Add Server")).Should().BeTrue();
    }

    [Fact]
    public void DataGrid_DisplaysServers_WhenLoaded()
    {
        // Arrange
        var servers = CreateTestServers();
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("Local MCP Server");
        cut.Markup.Should().Contain("Remote MCP Server");
    }

    [Fact]
    public void DataGrid_ShowsNoRecordsMessage_WhenEmpty()
    {
        // Arrange
        SetupEmptyServers();

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("No MCP servers configured");
    }

    [Fact]
    public void ErrorAlert_Shows_WhenLoadFails()
    {
        // Arrange
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Failure("Connection failed"));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("Connection failed");
    }

    [Fact]
    public void LocationChip_DisplaysCorrectText_ForLocal()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Local", Location = ServerLocation.Local }
        };
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("Local");
    }

    [Fact]
    public void LocationChip_DisplaysCorrectText_ForRemote()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Remote", Location = ServerLocation.Remote }
        };
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("Remote");
    }

    [Fact]
    public void EditButtons_ArePresent_ForServers()
    {
        // Arrange
        var servers = CreateTestServers();
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        var editButtons = cut.FindAll("button.mud-icon-button.mud-primary-text");
        editButtons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DeleteButtons_ArePresent_ForServers()
    {
        // Arrange
        var servers = CreateTestServers();
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        var deleteButtons = cut.FindAll("button.mud-icon-button.mud-error-text");
        deleteButtons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void DiscoverToolsButtons_ArePresent_ForServers()
    {
        // Arrange
        var servers = CreateTestServers();
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        var discoverButtons = cut.FindAll("button.mud-icon-button.mud-info-text");
        discoverButtons.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void Description_DisplaysSubtitle()
    {
        // Arrange
        SetupEmptyServers();

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("Configure Model Context Protocol servers");
    }

    [Fact]
    public void Service_CalledOnInitialization()
    {
        // Arrange
        SetupEmptyServers();

        // Act
        _ = RenderComponent<McpServersPage>();

        // Assert
        _mockServerService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void LocalServer_DisplaysCommand()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Local", Location = ServerLocation.Local, Command = "npx" }
        };
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("npx");
    }

    [Fact]
    public void RemoteServer_DisplaysUrl()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Remote", Location = ServerLocation.Remote, Url = "https://mcp.example.com" }
        };
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var cut = RenderComponent<McpServersPage>();

        // Assert
        cut.Markup.Should().Contain("https://mcp.example.com");
    }

    private void SetupEmptyServers()
    {
        _mockServerService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(new List<AesirMcpServerBase>()));
    }

    private static List<AesirMcpServerBase> CreateTestServers()
    {
        return
        [
            new AesirMcpServerBase
            {
                Id = Guid.NewGuid(),
                Name = "Local MCP Server",
                Description = "A local MCP server",
                Location = ServerLocation.Local,
                Command = "npx",
                Arguments = ["@modelcontextprotocol/server-filesystem"]
            },
            new AesirMcpServerBase
            {
                Id = Guid.NewGuid(),
                Name = "Remote MCP Server",
                Description = "A remote MCP server",
                Location = ServerLocation.Remote,
                Url = "https://mcp.example.com"
            }
        ];
    }
}

using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Services;

public class McpServerServiceTests
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly McpServerService _service;

    public McpServerServiceTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _service = new McpServerService(_mockApiService.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsServers_WhenApiSucceeds()
    {
        // Arrange
        var servers = new List<AesirMcpServerBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Local Server", Location = ServerLocation.Local },
            new() { Id = Guid.NewGuid(), Name = "Remote Server", Location = ServerLocation.Remote }
        };
        _mockApiService.Setup(x => x.GetMcpServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        _mockApiService.Setup(x => x.GetMcpServersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Failure("Network error"));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsServer_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var server = new AesirMcpServerBase { Id = id, Name = "Test Server" };
        _mockApiService.Setup(x => x.GetMcpServerAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirMcpServerBase>.Success(server));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Server");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetMcpServerAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirMcpServerBase>.Failure("MCP server not found"));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateAsync_ReturnsNewId_WhenSuccessful()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var server = new AesirMcpServerBase { Name = "New Server", Location = ServerLocation.Local };
        _mockApiService.Setup(x => x.CreateMcpServerAsync(server, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(newId));

        // Act
        var result = await _service.CreateAsync(server);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var server = new AesirMcpServerBase { Name = "New Server" };
        _mockApiService.Setup(x => x.CreateMcpServerAsync(server, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Failure("Validation error"));

        // Act
        var result = await _service.CreateAsync(server);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var server = new AesirMcpServerBase { Id = Guid.NewGuid(), Name = "Updated Server" };
        _mockApiService.Setup(x => x.UpdateMcpServerAsync(server, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateAsync(server);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var server = new AesirMcpServerBase { Id = Guid.NewGuid(), Name = "Updated Server" };
        _mockApiService.Setup(x => x.UpdateMcpServerAsync(server, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateAsync(server);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Update failed");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteMcpServerAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteMcpServerAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Delete failed"));

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Delete failed");
    }

    [Fact]
    public async Task DiscoverToolsAsync_ReturnsTools_WhenSuccessful()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var tools = new List<AesirMcpServerToolBase>
        {
            new() { Name = "Tool 1" },
            new() { Name = "Tool 2" }
        };
        _mockApiService.Setup(x => x.GetMcpServerToolsAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerToolBase>>.Success(tools));

        // Act
        var result = await _service.DiscoverToolsAsync(serverId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task DiscoverToolsAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetMcpServerToolsAsync(serverId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerToolBase>>.Failure("Server offline"));

        // Act
        var result = await _service.DiscoverToolsAsync(serverId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Server offline");
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var servers = new List<AesirMcpServerBase>();
        _mockApiService.Setup(x => x.GetMcpServersAsync(cts.Token))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirMcpServerBase>>.Success(servers));

        // Act
        await _service.GetAllAsync(cts.Token);

        // Assert
        _mockApiService.Verify(x => x.GetMcpServersAsync(cts.Token), Times.Once);
    }
}

using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Services;

public class ToolServiceTests
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly ToolService _service;

    public ToolServiceTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _service = new ToolService(_mockApiService.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTools_WhenApiSucceeds()
    {
        // Arrange
        var tools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Internal Tool", Type = ToolType.Internal },
            new() { Id = Guid.NewGuid(), Name = "MCP Tool", Type = ToolType.McpServer }
        };
        _mockApiService.Setup(x => x.GetToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));

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
        _mockApiService.Setup(x => x.GetToolsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Failure("Network error"));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTool_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tool = new AesirToolBase { Id = id, Name = "Test Tool" };
        _mockApiService.Setup(x => x.GetToolAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirToolBase>.Success(tool));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Tool");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetToolAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirToolBase>.Failure("Tool not found"));

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
        var tool = new AesirToolBase { Name = "New Tool", Type = ToolType.Internal };
        _mockApiService.Setup(x => x.CreateToolAsync(tool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(newId));

        // Act
        var result = await _service.CreateAsync(tool);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var tool = new AesirToolBase { Name = "New Tool" };
        _mockApiService.Setup(x => x.CreateToolAsync(tool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Failure("Validation error"));

        // Act
        var result = await _service.CreateAsync(tool);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var tool = new AesirToolBase { Id = Guid.NewGuid(), Name = "Updated Tool" };
        _mockApiService.Setup(x => x.UpdateToolAsync(tool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateAsync(tool);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var tool = new AesirToolBase { Id = Guid.NewGuid(), Name = "Updated Tool" };
        _mockApiService.Setup(x => x.UpdateToolAsync(tool, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateAsync(tool);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Update failed");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteToolAsync(id, It.IsAny<CancellationToken>()))
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
        _mockApiService.Setup(x => x.DeleteToolAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Delete failed"));

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Delete failed");
    }

    [Fact]
    public async Task GetAllAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tools = new List<AesirToolBase>();
        _mockApiService.Setup(x => x.GetToolsAsync(cts.Token))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));

        // Act
        await _service.GetAllAsync(cts.Token);

        // Assert
        _mockApiService.Verify(x => x.GetToolsAsync(cts.Token), Times.Once);
    }
}

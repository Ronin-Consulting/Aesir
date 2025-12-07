using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Services;

public class InferenceEngineServiceTests
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly InferenceEngineService _service;

    public InferenceEngineServiceTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _service = new InferenceEngineService(_mockApiService.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEngines_WhenApiSucceeds()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Ollama", Type = InferenceEngineType.Ollama },
            new() { Id = Guid.NewGuid(), Name = "OpenAI", Type = InferenceEngineType.OpenAICompatible }
        };
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

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
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Failure("Network error"));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Network error");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEngine_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var engine = new AesirInferenceEngineBase { Id = id, Name = "Test Engine" };
        _mockApiService.Setup(x => x.GetInferenceEngineAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirInferenceEngineBase>.Success(engine));

        // Act
        var result = await _service.GetByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Engine");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsFailure_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.GetInferenceEngineAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirInferenceEngineBase>.Failure("Inference engine not found"));

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
        var engine = new AesirInferenceEngineBase { Name = "New Engine", Type = InferenceEngineType.Ollama };
        _mockApiService.Setup(x => x.CreateInferenceEngineAsync(engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(newId));

        // Act
        var result = await _service.CreateAsync(engine);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(newId);
    }

    [Fact]
    public async Task CreateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var engine = new AesirInferenceEngineBase { Name = "New Engine" };
        _mockApiService.Setup(x => x.CreateInferenceEngineAsync(engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Failure("Validation error"));

        // Act
        var result = await _service.CreateAsync(engine);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Validation error");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var engine = new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Updated Engine" };
        _mockApiService.Setup(x => x.UpdateInferenceEngineAsync(engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateAsync(engine);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFailure_WhenApiFails()
    {
        // Arrange
        var engine = new AesirInferenceEngineBase { Id = Guid.NewGuid(), Name = "Updated Engine" };
        _mockApiService.Setup(x => x.UpdateInferenceEngineAsync(engine, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateAsync(engine);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Update failed");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsSuccess_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockApiService.Setup(x => x.DeleteInferenceEngineAsync(id, It.IsAny<CancellationToken>()))
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
        _mockApiService.Setup(x => x.DeleteInferenceEngineAsync(id, It.IsAny<CancellationToken>()))
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
        var engines = new List<AesirInferenceEngineBase>();
        _mockApiService.Setup(x => x.GetInferenceEnginesAsync(cts.Token))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        await _service.GetAllAsync(cts.Token);

        // Assert
        _mockApiService.Verify(x => x.GetInferenceEnginesAsync(cts.Token), Times.Once);
    }
}

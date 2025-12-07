using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Services;

public class GeneralSettingsServiceTests
{
    private readonly Mock<IConfigurationApiService> _mockApiService;
    private readonly GeneralSettingsService _service;

    public GeneralSettingsServiceTests()
    {
        _mockApiService = new Mock<IConfigurationApiService>();
        _service = new GeneralSettingsService(_mockApiService.Object);
    }

    #region GetSettingsAsync Tests

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings_WhenApiSucceeds()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.GetGeneralSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(settings));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RagEmbeddingModel.Should().Be("test-embedding");
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsError_WhenApiFails()
    {
        // Arrange
        _mockApiService
            .Setup(x => x.GetGeneralSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Failure("API Error"));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("API Error");
    }

    [Fact]
    public async Task GetSettingsAsync_CallsApiService()
    {
        // Arrange
        _mockApiService
            .Setup(x => x.GetGeneralSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(new AesirGeneralSettingsBase()));

        // Act
        await _service.GetSettingsAsync();

        // Assert
        _mockApiService.Verify(x => x.GetGeneralSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateSettingsAsync Tests

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _service.UpdateSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsError_WhenApiFails()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateSettingsAsync(settings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Update failed");
    }

    [Fact]
    public async Task UpdateSettingsAsync_CallsApiServiceWithSettings()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(It.IsAny<AesirGeneralSettingsBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        await _service.UpdateSettingsAsync(settings);

        // Assert
        _mockApiService.Verify(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateSettingsAndReloadAsync Tests

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_ReturnsSuccess_WhenBothSucceed()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());
        _mockApiService
            .Setup(x => x.ReloadConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirConfigurationReadinessBase>.Success(new AesirConfigurationReadinessBase { IsReady = true }));

        // Act
        var result = await _service.UpdateSettingsAndReloadAsync(settings);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_ReturnsError_WhenUpdateFails()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        var result = await _service.UpdateSettingsAndReloadAsync(settings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Update failed");
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_ReturnsError_WhenReloadFails()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());
        _mockApiService
            .Setup(x => x.ReloadConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirConfigurationReadinessBase>.Failure("Reload failed"));

        // Act
        var result = await _service.UpdateSettingsAndReloadAsync(settings);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Reload failed");
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_DoesNotReload_WhenUpdateFails()
    {
        // Arrange
        var settings = CreateTestSettings();
        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Update failed"));

        // Act
        await _service.UpdateSettingsAndReloadAsync(settings);

        // Assert
        _mockApiService.Verify(x => x.ReloadConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_CallsUpdateThenReload()
    {
        // Arrange
        var settings = CreateTestSettings();
        var callOrder = new List<string>();

        _mockApiService
            .Setup(x => x.UpdateGeneralSettingsAsync(settings, It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Update"))
            .ReturnsAsync(ApiResult.Success());
        _mockApiService
            .Setup(x => x.ReloadConfigurationAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Reload"))
            .ReturnsAsync(ApiResult<AesirConfigurationReadinessBase>.Success(new AesirConfigurationReadinessBase()));

        // Act
        await _service.UpdateSettingsAndReloadAsync(settings);

        // Assert
        callOrder.Should().ContainInOrder("Update", "Reload");
    }

    #endregion

    #region Helper Methods

    private static AesirGeneralSettingsBase CreateTestSettings()
    {
        return new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "test-embedding",
            RagVisionInferenceEngineId = Guid.NewGuid(),
            RagVisionModel = "test-vision",
            TtsModelPath = "/path/to/tts",
            SttModelPath = "/path/to/stt",
            VadModelPath = "/path/to/vad",
            GoogleSearchEngineId = "search-engine-id",
            GoogleApiKey = "api-key"
        };
    }

    #endregion
}

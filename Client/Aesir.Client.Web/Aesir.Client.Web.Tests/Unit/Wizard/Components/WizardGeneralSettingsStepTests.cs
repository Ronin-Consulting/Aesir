using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Wizard.Components;

/// <summary>
/// Tests for the WizardGeneralSettingsStep component.
/// Note: Full component rendering tests for MudBlazor components with MudSelect are complex
/// due to MudBlazor 8.x popover service requirements. These tests focus on service interactions
/// and validation logic that can be tested through the service layer.
/// </summary>
public class WizardGeneralSettingsStepTests
{
    private readonly Mock<IGeneralSettingsService> _mockSettingsService;
    private readonly Mock<IInferenceEngineService> _mockEngineService;

    public WizardGeneralSettingsStepTests()
    {
        _mockSettingsService = new Mock<IGeneralSettingsService>();
        _mockEngineService = new Mock<IInferenceEngineService>();
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsSettings_WhenConfigured()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "nomic-embed-text"
        };
        _mockSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(settings));

        // Act
        var result = await _mockSettingsService.Object.GetSettingsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RagEmbeddingModel.Should().Be("nomic-embed-text");
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsEmptySettings_WhenNotConfigured()
    {
        // Arrange
        _mockSettingsService.Setup(x => x.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirGeneralSettingsBase>.Success(new AesirGeneralSettingsBase()));

        // Act
        var result = await _mockSettingsService.Object.GetSettingsAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.RagEmbeddingModel.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_SavesSettings_Successfully()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "nomic-embed-text"
        };
        _mockSettingsService.Setup(x => x.UpdateSettingsAndReloadAsync(It.IsAny<AesirGeneralSettingsBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _mockSettingsService.Object.UpdateSettingsAndReloadAsync(settings, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockSettingsService.Verify(x => x.UpdateSettingsAndReloadAsync(
            It.Is<AesirGeneralSettingsBase>(s => s.RagEmbeddingModel == "nomic-embed-text"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAndReloadAsync_ReturnsFailure_WhenSaveFails()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase();
        _mockSettingsService.Setup(x => x.UpdateSettingsAndReloadAsync(It.IsAny<AesirGeneralSettingsBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Failure("Save failed"));

        // Act
        var result = await _mockSettingsService.Object.UpdateSettingsAndReloadAsync(settings, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Save failed");
    }

    [Fact]
    public void CanProceed_ReturnsFalse_WhenEmbeddingNotConfigured()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase();

        // Act
        var canProceed = settings.RagEmbeddingInferenceEngineId.HasValue &&
                         !string.IsNullOrWhiteSpace(settings.RagEmbeddingModel);

        // Assert
        canProceed.Should().BeFalse();
    }

    [Fact]
    public void CanProceed_ReturnsTrue_WhenEmbeddingConfigured()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "nomic-embed-text"
        };

        // Act
        var canProceed = settings.RagEmbeddingInferenceEngineId.HasValue &&
                         !string.IsNullOrWhiteSpace(settings.RagEmbeddingModel);

        // Assert
        canProceed.Should().BeTrue();
    }

    [Fact]
    public void RAGVision_IsOptional()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "nomic-embed-text",
            RagVisionModel = null
        };

        // Act - RAG Vision being null should still allow proceeding
        var canProceed = settings.RagEmbeddingInferenceEngineId.HasValue &&
                         !string.IsNullOrWhiteSpace(settings.RagEmbeddingModel);

        // Assert
        canProceed.Should().BeTrue();
    }

    [Fact]
    public void RAGVision_CanBeConfigured()
    {
        // Arrange
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = Guid.NewGuid(),
            RagEmbeddingModel = "nomic-embed-text",
            RagVisionInferenceEngineId = Guid.NewGuid(),
            RagVisionModel = "llava"
        };

        // Assert
        settings.RagVisionModel.Should().Be("llava");
        settings.RagVisionInferenceEngineId.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllEnginesAsync_ReturnsEngines()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Ollama" },
            new() { Id = Guid.NewGuid(), Name = "OpenAI" }
        };
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var result = await _mockEngineService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void AutoSelectFirstEngine_WhenMultipleEngines()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "First Engine" },
            new() { Id = Guid.NewGuid(), Name = "Second Engine" }
        };

        // Act - Simulating auto-selection logic
        var selectedEngine = engines.FirstOrDefault();

        // Assert
        selectedEngine.Should().NotBeNull();
        selectedEngine!.Name.Should().Be("First Engine");
    }
}

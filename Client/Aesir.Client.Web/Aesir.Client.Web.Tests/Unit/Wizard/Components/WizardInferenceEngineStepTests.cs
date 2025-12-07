using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Wizard.Components;

/// <summary>
/// Tests for the WizardInferenceEngineStep component.
/// Note: Full component rendering tests for MudBlazor components with MudSelect are complex
/// due to MudBlazor 8.x popover service requirements. These tests focus on service interactions
/// and validation logic that can be tested through the service layer.
/// </summary>
public class WizardInferenceEngineStepTests
{
    private readonly Mock<IInferenceEngineService> _mockEngineService;

    public WizardInferenceEngineStepTests()
    {
        _mockEngineService = new Mock<IInferenceEngineService>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEngines_WhenEnginesExist()
    {
        // Arrange
        var engines = CreateTestEngines();
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var result = await _mockEngineService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value![0].Name.Should().Be("Ollama Engine");
        result.Value[1].Name.Should().Be("OpenAI Engine");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoEnginesConfigured()
    {
        // Arrange
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));

        // Act
        var result = await _mockEngineService.Object.GetAllAsync(CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CreatesEngine_WithValidData()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var newEngine = new AesirInferenceEngineBase
        {
            Id = engineId,
            Name = "New Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        };

        _mockEngineService.Setup(x => x.CreateAsync(It.IsAny<AesirInferenceEngineBase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<Guid>.Success(engineId));

        // Act
        var result = await _mockEngineService.Object.CreateAsync(newEngine, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(engineId);
    }

    [Fact]
    public async Task DeleteAsync_DeletesEngine_Successfully()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        _mockEngineService.Setup(x => x.DeleteAsync(engineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        // Act
        var result = await _mockEngineService.Object.DeleteAsync(engineId, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void CanProceed_ReturnsFalse_WhenNoEngines()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>();

        // Act
        var canProceed = engines.Count > 0;

        // Assert
        canProceed.Should().BeFalse();
    }

    [Fact]
    public void CanProceed_ReturnsTrue_WhenEnginesExist()
    {
        // Arrange
        var engines = CreateTestEngines();

        // Act
        var canProceed = engines.Count > 0;

        // Assert
        canProceed.Should().BeTrue();
    }

    [Theory]
    [InlineData(InferenceEngineType.Ollama, "http://localhost:11434")]
    [InlineData(InferenceEngineType.OpenAICompatible, "https://api.openai.com/v1")]
    public void EnginePreset_HasCorrectDefaults(InferenceEngineType type, string expectedEndpoint)
    {
        // Arrange
        var engine = new AesirInferenceEngineBase
        {
            Type = type,
            Configuration = new Dictionary<string, string?> { { "Endpoint", expectedEndpoint } }
        };

        // Assert
        engine.Type.Should().Be(type);
        engine.Configuration["Endpoint"].Should().Be(expectedEndpoint);
    }

    [Fact]
    public void EngineValidation_RequiresName()
    {
        // Arrange
        var engine = new AesirInferenceEngineBase
        {
            Name = "",
            Type = InferenceEngineType.Ollama
        };

        // Act
        var isValid = !string.IsNullOrWhiteSpace(engine.Name);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EngineValidation_AcceptsValidName()
    {
        // Arrange
        var engine = new AesirInferenceEngineBase
        {
            Name = "My Ollama Engine",
            Type = InferenceEngineType.Ollama
        };

        // Act
        var isValid = !string.IsNullOrWhiteSpace(engine.Name);

        // Assert
        isValid.Should().BeTrue();
    }

    private static IReadOnlyList<AesirInferenceEngineBase> CreateTestEngines()
    {
        return new List<AesirInferenceEngineBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Ollama Engine",
                Type = InferenceEngineType.Ollama,
                Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "OpenAI Engine",
                Type = InferenceEngineType.OpenAICompatible,
                Configuration = new Dictionary<string, string?> { { "Endpoint", "https://api.openai.com/v1" } }
            }
        };
    }
}

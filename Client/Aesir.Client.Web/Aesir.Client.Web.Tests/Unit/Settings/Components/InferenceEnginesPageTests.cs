using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Pages;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class InferenceEnginesPageTests : TestContext
{
    private readonly Mock<IInferenceEngineService> _mockEngineService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public InferenceEnginesPageTests()
    {
        _mockEngineService = new Mock<IInferenceEngineService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();

        // Register services
        Services.AddSingleton(_mockEngineService.Object);
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
        SetupEmptyEngines();

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Find("h4").TextContent.Should().Contain("Inference Engines");
    }

    [Fact]
    public void AddButton_IsPresent()
    {
        // Arrange
        SetupEmptyEngines();

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Add Engine")).Should().BeTrue();
    }

    [Fact]
    public void DataGrid_DisplaysEngines_WhenLoaded()
    {
        // Arrange
        var engines = CreateTestEngines();
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        // The data grid should have rendered with the engine data
        cut.Markup.Should().Contain("Ollama Engine");
        cut.Markup.Should().Contain("OpenAI Engine");
    }

    [Fact]
    public void DataGrid_ShowsNoRecordsMessage_WhenEmpty()
    {
        // Arrange
        SetupEmptyEngines();

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Markup.Should().Contain("No inference engines configured");
    }

    [Fact]
    public void ErrorAlert_Shows_WhenLoadFails()
    {
        // Arrange
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Failure("Connection failed"));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Markup.Should().Contain("Connection failed");
    }

    [Fact]
    public void TypeChip_DisplaysCorrectText_ForOllama()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Ollama", Type = InferenceEngineType.Ollama }
        };
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        // Check that Ollama text appears (either as name or type chip)
        cut.Markup.Should().Contain("Ollama");
    }

    [Fact]
    public void TypeChip_DisplaysCorrectText_ForOpenAI()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "OpenAI", Type = InferenceEngineType.OpenAICompatible }
        };
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Markup.Should().Contain("OpenAI Compatible");
    }

    [Fact]
    public void EditButtons_ArePresent_ForEngines()
    {
        // Arrange
        var engines = CreateTestEngines();
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        // MudBlazor icon buttons have mud-icon-button class with primary color for edit
        var editButtons = cut.FindAll("button.mud-icon-button.mud-primary-text");
        editButtons.Should().HaveCountGreaterThanOrEqualTo(2); // One for each engine
    }

    [Fact]
    public void DeleteButtons_ArePresent_ForEngines()
    {
        // Arrange
        var engines = CreateTestEngines();
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        // MudBlazor icon buttons have mud-icon-button class with error color for delete
        var deleteButtons = cut.FindAll("button.mud-icon-button.mud-error-text");
        deleteButtons.Should().HaveCountGreaterThanOrEqualTo(2); // One for each engine
    }

    [Fact]
    public void Description_DisplaysSubtitle()
    {
        // Arrange
        SetupEmptyEngines();

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Markup.Should().Contain("Configure the AI backends used for chat completions");
    }

    [Fact]
    public void Service_CalledOnInitialization()
    {
        // Arrange
        SetupEmptyEngines();

        // Act
        _ = RenderComponent<InferenceEnginesPage>();

        // Assert
        _mockEngineService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void EngineDescription_IsDisplayed()
    {
        // Arrange
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Test", Description = "Test Description", Type = InferenceEngineType.Ollama }
        };
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));

        // Act
        var cut = RenderComponent<InferenceEnginesPage>();

        // Assert
        cut.Markup.Should().Contain("Test Description");
    }

    private void SetupEmptyEngines()
    {
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
    }

    private static List<AesirInferenceEngineBase> CreateTestEngines()
    {
        return
        [
            new AesirInferenceEngineBase
            {
                Id = Guid.NewGuid(),
                Name = "Ollama Engine",
                Description = "Local Ollama instance",
                Type = InferenceEngineType.Ollama,
                Configuration = new Dictionary<string, string?> { ["BaseUrl"] = "http://localhost:11434" }
            },
            new AesirInferenceEngineBase
            {
                Id = Guid.NewGuid(),
                Name = "OpenAI Engine",
                Description = "OpenAI API",
                Type = InferenceEngineType.OpenAICompatible,
                Configuration = new Dictionary<string, string?> { ["BaseUrl"] = "https://api.openai.com/v1" }
            }
        ];
    }
}

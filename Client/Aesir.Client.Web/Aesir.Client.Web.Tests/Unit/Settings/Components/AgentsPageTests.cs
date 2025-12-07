using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Settings.Pages;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Common.Models;
using Aesir.Common.Prompts;

namespace Aesir.Client.Web.Tests.Unit.Settings.Components;

public class AgentsPageTests : TestContext
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IInferenceEngineService> _mockEngineService;
    private readonly Mock<IToolService> _mockToolService;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public AgentsPageTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockEngineService = new Mock<IInferenceEngineService>();
        _mockToolService = new Mock<IToolService>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();

        // Register services
        Services.AddSingleton(_mockAgentService.Object);
        Services.AddSingleton(_mockEngineService.Object);
        Services.AddSingleton(_mockToolService.Object);
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
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Find("h4").TextContent.Should().Contain("Agents");
    }

    [Fact]
    public void AddButton_IsPresent()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Add Agent")).Should().BeTrue();
    }

    [Fact]
    public void DataGrid_DisplaysAgents_WhenLoaded()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = CreateTestAgents(engineId);
        var engines = CreateTestEngines(engineId);
        var tools = new List<AesirToolBase>();

        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockAgentService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Business Agent");
        cut.Markup.Should().Contain("gpt-4");
    }

    [Fact]
    public void DataGrid_ShowsNoRecordsMessage_WhenEmpty()
    {
        // Arrange
        SetupEmptyData();

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("No agents configured");
    }

    [Fact]
    public void ErrorAlert_Shows_WhenLoadFails()
    {
        // Arrange
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Failure("Connection failed"));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Connection failed");
    }

    [Fact]
    public void PersonaChip_DisplaysCorrectText_ForBusiness()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Business Agent",
                ChatModel = "gpt-4",
                ChatPromptPersona = PromptPersona.Business
            }
        };
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Business");
    }

    [Fact]
    public void PersonaChip_DisplaysCorrectText_ForMilitary()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Military Agent",
                ChatModel = "gpt-4",
                ChatPromptPersona = PromptPersona.Military
            }
        };
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Military");
    }

    [Fact]
    public void PersonaChip_DisplaysCorrectText_ForCustom()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Custom Agent",
                ChatModel = "gpt-4",
                ChatPromptPersona = PromptPersona.Custom
            }
        };
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Custom");
    }

    [Fact]
    public void EditDeleteButtons_ArePresent_ForAgents()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = CreateTestAgents(engineId);
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

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
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("Configure AI agents with model parameters, personas, and tool assignments");
    }

    [Fact]
    public void Services_CalledOnInitialization()
    {
        // Arrange
        SetupEmptyData();

        // Act
        _ = RenderComponent<AgentsPage>();

        // Assert
        _mockAgentService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEngineService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockToolService.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void InferenceEngineName_DisplaysCorrectly()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Agent",
                ChatModel = "gpt-4",
                ChatInferenceEngineId = engineId
            }
        };
        var engines = new List<AesirInferenceEngineBase>
        {
            new() { Id = engineId, Name = "OpenAI Engine" }
        };
        var tools = new List<AesirToolBase>();

        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockAgentService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("OpenAI Engine");
    }

    [Fact]
    public void ToolCount_DisplaysCorrectly()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new() { Id = agentId, Name = "Test Agent", ChatModel = "gpt-4" }
        };
        var agentTools = new List<AesirToolBase>
        {
            new() { Id = Guid.NewGuid(), Name = "Tool 1" },
            new() { Id = Guid.NewGuid(), Name = "Tool 2" },
            new() { Id = Guid.NewGuid(), Name = "Tool 3" }
        };

        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockAgentService.Setup(x => x.GetAgentToolsAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(agentTools));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("3 tools");
    }

    [Fact]
    public void ChatModel_DisplaysCorrectly()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Agent",
                ChatModel = "claude-3-opus-20240229"
            }
        };
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("claude-3-opus-20240229");
    }

    [Fact]
    public void AgentDescription_IsDisplayed()
    {
        // Arrange
        var engineId = Guid.NewGuid();
        var agents = new List<AesirAgentBase>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Test Agent",
                Description = "A helpful assistant for coding tasks",
                ChatModel = "gpt-4"
            }
        };
        SetupWithAgents(agents, engineId);

        // Act
        var cut = RenderComponent<AgentsPage>();

        // Assert
        cut.Markup.Should().Contain("A helpful assistant for coding tasks");
    }

    private void SetupEmptyData()
    {
        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(new List<AesirAgentBase>()));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(new List<AesirInferenceEngineBase>()));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(new List<AesirToolBase>()));
    }

    private void SetupWithAgents(List<AesirAgentBase> agents, Guid engineId)
    {
        var engines = CreateTestEngines(engineId);
        var tools = new List<AesirToolBase>();

        _mockAgentService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirAgentBase>>.Success(agents));
        _mockAgentService.Setup(x => x.GetAgentToolsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
        _mockEngineService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirInferenceEngineBase>>.Success(engines));
        _mockToolService.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirToolBase>>.Success(tools));
    }

    private static List<AesirAgentBase> CreateTestAgents(Guid engineId)
    {
        return
        [
            new AesirAgentBase
            {
                Id = Guid.NewGuid(),
                Name = "Business Agent",
                Description = "A business-focused assistant",
                ChatInferenceEngineId = engineId,
                ChatModel = "gpt-4",
                ChatTemperature = 0.7,
                ChatTopP = 1.0,
                ChatMaxTokens = 4096,
                ChatPromptPersona = PromptPersona.Business
            },
            new AesirAgentBase
            {
                Id = Guid.NewGuid(),
                Name = "Custom Agent",
                Description = "A custom assistant",
                ChatInferenceEngineId = engineId,
                ChatModel = "claude-3",
                ChatPromptPersona = PromptPersona.Custom,
                ChatCustomPromptContent = "You are a helpful coding assistant."
            }
        ];
    }

    private static List<AesirInferenceEngineBase> CreateTestEngines(Guid engineId)
    {
        return
        [
            new AesirInferenceEngineBase
            {
                Id = engineId,
                Name = "OpenAI Engine",
                Type = InferenceEngineType.OpenAICompatible
            }
        ];
    }
}

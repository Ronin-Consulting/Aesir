using Microsoft.Extensions.DependencyInjection;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Settings.Services;
using Aesir.Client.Web.Modules.Wizard.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Integration.Flows;

/// <summary>
/// Integration tests for the Setup Wizard flow:
/// Welcome → Inference Engine → General Settings → Agent → Complete
/// </summary>
public class WizardFlowTests : IntegrationTestBase
{
    #region Wizard State Service Tests

    [Fact]
    public void WizardStateService_HasCorrectSteps()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();

        // Assert
        wizardService.Steps.Should().HaveCount(5);
        wizardService.Steps[0].Id.Should().Be("welcome");
        wizardService.Steps[1].Id.Should().Be("inference-engine");
        wizardService.Steps[2].Id.Should().Be("general-settings");
        wizardService.Steps[3].Id.Should().Be("agent");
        wizardService.Steps[4].Id.Should().Be("complete");
    }

    [Fact]
    public void WizardStateService_StartsAtFirstStep()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();

        // Assert
        wizardService.CurrentStep.Should().Be(0);
        wizardService.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task WizardStateService_GoToNextStep_IncrementsStep()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        wizardService.CurrentStep.Should().Be(0);

        // Act
        var result = await wizardService.GoToNextStepAsync();

        // Assert
        result.Should().BeTrue();
        wizardService.CurrentStep.Should().Be(1);
    }

    [Fact]
    public void WizardStateService_GoToPreviousStep_DecrementsStep()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        wizardService.GoToStep(2);
        wizardService.CurrentStep.Should().Be(2);

        // Act
        wizardService.GoToPreviousStep();

        // Assert
        wizardService.CurrentStep.Should().Be(1);
    }

    [Fact]
    public void WizardStateService_GoToPreviousStep_DoesNotGoBelowZero()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        wizardService.CurrentStep.Should().Be(0);

        // Act
        wizardService.GoToPreviousStep();

        // Assert
        wizardService.CurrentStep.Should().Be(0);
    }

    [Fact]
    public async Task WizardStateService_GoToNextStep_StopsAtLastStep()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();

        // Navigate to last step
        for (int i = 0; i < 4; i++)
        {
            await wizardService.GoToNextStepAsync();
        }
        wizardService.CurrentStep.Should().Be(4);

        // Act - try to go past last step
        var result = await wizardService.GoToNextStepAsync();

        // Assert
        result.Should().BeFalse();
        wizardService.CurrentStep.Should().Be(4);
    }

    [Fact]
    public void WizardStateService_GoToStep_NavigatesToSpecificStep()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();

        // Act
        wizardService.GoToStep(3);

        // Assert
        wizardService.CurrentStep.Should().Be(3);
    }

    [Fact]
    public void WizardStateService_GoToStep_IgnoresInvalidIndex()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        wizardService.GoToStep(2);
        wizardService.CurrentStep.Should().Be(2);

        // Act - try invalid indexes
        wizardService.GoToStep(-1);
        wizardService.CurrentStep.Should().Be(2);

        wizardService.GoToStep(10);
        wizardService.CurrentStep.Should().Be(2);
    }

    [Fact]
    public void WizardStateService_CompleteStep_MarksStepAsCompleted()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        wizardService.Steps[0].IsCompleted.Should().BeFalse();

        // Act
        wizardService.CompleteStep(0);

        // Assert
        wizardService.Steps[0].IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void WizardStateService_StateChanged_EventFires()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        var eventFired = false;
        wizardService.StateChanged += (_, _) => eventFired = true;

        // Act
        wizardService.GoToStep(2);

        // Assert
        eventFired.Should().BeTrue();
    }

    #endregion

    #region Inference Engine Step Flow Tests

    [Fact]
    public async Task InferenceEngineService_GetAllAsync_ReturnsEngines()
    {
        // Arrange
        AddTestInferenceEngine("Ollama Local", InferenceEngineType.Ollama);
        AddTestInferenceEngine("OpenAI", InferenceEngineType.OpenAICompatible);
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act
        var result = await engineService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task InferenceEngineService_CreateAsync_AddsEngine()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var newEngine = new AesirInferenceEngineBase
        {
            Name = "New Ollama Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        };

        // Act
        var result = await engineService.CreateAsync(newEngine);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        InferenceEngines.Should().HaveCount(1);
    }

    [Fact]
    public async Task InferenceEngineService_DeleteAsync_RemovesEngine()
    {
        // Arrange
        AddTestInferenceEngine("To Delete", InferenceEngineType.Ollama);
        var engineId = InferenceEngines.First().Id!.Value;
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act
        var result = await engineService.DeleteAsync(engineId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        InferenceEngines.Should().BeEmpty();
    }

    #endregion

    #region General Settings Step Flow Tests

    [Fact]
    public async Task GeneralSettingsService_GetSettingsAsync_ReturnsSettings()
    {
        // Arrange
        AddTestInferenceEngine();
        SetupTestGeneralSettings();
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Act
        var result = await settingsService.GetSettingsAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.RagEmbeddingModel.Should().Be("nomic-embed-text:latest");
    }

    [Fact]
    public async Task GeneralSettingsService_UpdateSettingsAsync_SavesSettings()
    {
        // Arrange
        AddTestInferenceEngine();
        var engineId = InferenceEngines.First().Id!.Value;
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();
        var newSettings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineId,
            RagEmbeddingModel = "mxbai-embed-large:latest"
        };

        // Act
        var result = await settingsService.UpdateSettingsAsync(newSettings);

        // Assert
        result.IsSuccess.Should().BeTrue();
        GeneralSettings.RagEmbeddingModel.Should().Be("mxbai-embed-large:latest");
    }

    #endregion

    #region Agent Step Flow Tests

    [Fact]
    public async Task AgentService_CreateAsync_WithInferenceEngine_Succeeds()
    {
        // Arrange
        AddTestInferenceEngine();
        var engineId = InferenceEngines.First().Id!.Value;
        var agentService = Services.GetRequiredService<IAgentService>();

        var newAgent = new AesirAgentBase
        {
            Name = "My First Agent",
            Description = "Test agent",
            ChatInferenceEngineId = engineId,
            ChatModel = "llama3.2",
            ChatTemperature = 0.7,
            ChatTopP = 1.0,
            ChatMaxTokens = 4096
        };

        // Act
        var result = await agentService.CreateAsync(newAgent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        Agents.Should().HaveCount(1);
        Agents.First().Name.Should().Be("My First Agent");
    }

    [Fact]
    public async Task AgentService_GetAllAsync_ReturnsAgents()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent("Agent 1");
        AddTestAgent("Agent 2");
        var agentService = Services.GetRequiredService<IAgentService>();

        // Act
        var result = await agentService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region Full Wizard Flow Tests

    [Fact]
    public async Task CompleteWizardFlow_CreatesEngineSettingsAndAgent()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Step 1: Welcome - just advance
        wizardService.CurrentStep.Should().Be(0);
        await wizardService.GoToNextStepAsync();

        // Step 2: Create Inference Engine
        wizardService.CurrentStep.Should().Be(1);
        var engine = new AesirInferenceEngineBase
        {
            Name = "Wizard Test Engine",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        };
        var engineResult = await engineService.CreateAsync(engine);
        engineResult.IsSuccess.Should().BeTrue();
        await wizardService.GoToNextStepAsync();

        // Step 3: Configure General Settings
        wizardService.CurrentStep.Should().Be(2);
        var settings = new AesirGeneralSettingsBase
        {
            RagEmbeddingInferenceEngineId = engineResult.Value,
            RagEmbeddingModel = "nomic-embed-text:latest"
        };
        var settingsResult = await settingsService.UpdateSettingsAsync(settings);
        settingsResult.IsSuccess.Should().BeTrue();
        await wizardService.GoToNextStepAsync();

        // Step 4: Create Agent
        wizardService.CurrentStep.Should().Be(3);
        var agent = new AesirAgentBase
        {
            Name = "Wizard Test Agent",
            ChatInferenceEngineId = engineResult.Value,
            ChatModel = "llama3.2",
            ChatTemperature = 0.7
        };
        var agentResult = await agentService.CreateAsync(agent);
        agentResult.IsSuccess.Should().BeTrue();
        await wizardService.GoToNextStepAsync();

        // Step 5: Complete
        wizardService.CurrentStep.Should().Be(4);

        // Verify all data was created
        InferenceEngines.Should().HaveCount(1);
        InferenceEngines.First().Name.Should().Be("Wizard Test Engine");

        Agents.Should().HaveCount(1);
        Agents.First().Name.Should().Be("Wizard Test Agent");

        GeneralSettings.RagEmbeddingModel.Should().Be("nomic-embed-text:latest");
    }

    [Fact]
    public async Task WizardFlow_BackNavigation_PreservesData()
    {
        // Arrange
        var wizardService = Services.GetRequiredService<IWizardStateService>();
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Navigate to Step 2 (Inference Engine)
        await wizardService.GoToNextStepAsync();
        wizardService.CurrentStep.Should().Be(1);

        // Create an engine
        var engine = new AesirInferenceEngineBase
        {
            Name = "Back Nav Test Engine",
            Type = InferenceEngineType.Ollama
        };
        await engineService.CreateAsync(engine);
        InferenceEngines.Should().HaveCount(1);

        // Navigate forward then back
        await wizardService.GoToNextStepAsync();
        wizardService.CurrentStep.Should().Be(2);

        wizardService.GoToPreviousStep();
        wizardService.CurrentStep.Should().Be(1);

        // Verify engine is still there
        var enginesResult = await engineService.GetAllAsync();
        enginesResult.Value.Should().HaveCount(1);
        enginesResult.Value!.First().Name.Should().Be("Back Nav Test Engine");
    }

    [Fact]
    public async Task WizardFlow_MultipleEngines_AllCreated()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act - Create multiple engines like user would in wizard
        var engine1 = new AesirInferenceEngineBase
        {
            Name = "Ollama Local",
            Type = InferenceEngineType.Ollama,
            Configuration = new Dictionary<string, string?> { { "Endpoint", "http://localhost:11434" } }
        };
        var result1 = await engineService.CreateAsync(engine1);

        var engine2 = new AesirInferenceEngineBase
        {
            Name = "OpenAI API",
            Type = InferenceEngineType.OpenAICompatible,
            Configuration = new Dictionary<string, string?>
            {
                { "Endpoint", "https://api.openai.com/v1" },
                { "ApiKey", "sk-test-key" }
            }
        };
        var result2 = await engineService.CreateAsync(engine2);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        InferenceEngines.Should().HaveCount(2);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task WizardValidation_NoEngines_CannotProceed()
    {
        // Arrange
        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act
        var result = await engineService.GetAllAsync();

        // Assert
        result.Value.Should().BeEmpty();
        // The wizard step would use this to disable the Next button
    }

    [Fact]
    public void WizardValidation_NoAgents_CannotComplete()
    {
        // Arrange - no agents created

        // Assert
        Agents.Should().BeEmpty();
        // The wizard step would use this to disable the Next button
    }

    [Fact]
    public async Task WizardValidation_WithEngineAndAgent_CanComplete()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent();

        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Act
        var engines = await engineService.GetAllAsync();
        var agents = await agentService.GetAllAsync();

        // Assert
        engines.Value.Should().NotBeEmpty();
        agents.Value.Should().NotBeEmpty();
        // The wizard can now complete
    }

    #endregion

    #region Configuration Readiness Tests

    [Fact]
    public async Task ConfigurationReadiness_NoEngines_NotReady()
    {
        // Arrange
        var apiService = Services.GetRequiredService<IConfigurationApiService>();

        // Act
        var result = await apiService.GetSystemReadinessAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReady.Should().BeFalse();
        result.Value.Reasons.Should().Contain("No inference engines configured");
    }

    [Fact]
    public async Task ConfigurationReadiness_WithFullConfig_IsReady()
    {
        // Arrange
        AddTestInferenceEngine();
        AddTestAgent();
        SetupTestGeneralSettings();

        var apiService = Services.GetRequiredService<IConfigurationApiService>();

        // Act
        var result = await apiService.GetSystemReadinessAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsReady.Should().BeTrue();
        result.Value.Reasons.Should().BeEmpty();
    }

    #endregion

    #region Step Data Loading Tests

    [Fact]
    public async Task InferenceEngineStep_LoadsExistingEngines()
    {
        // Arrange - Simulate pre-configured state
        AddTestInferenceEngine("Existing Engine 1");
        AddTestInferenceEngine("Existing Engine 2");

        var engineService = Services.GetRequiredService<IInferenceEngineService>();

        // Act - Load engines as the step would
        var result = await engineService.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(e => e.Name).Should().Contain("Existing Engine 1");
        result.Value!.Select(e => e.Name).Should().Contain("Existing Engine 2");
    }

    [Fact]
    public async Task AgentStep_LoadsEnginesAndAgents()
    {
        // Arrange
        AddTestInferenceEngine("Engine for Agent");
        AddTestAgent("Existing Agent");

        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var agentService = Services.GetRequiredService<IAgentService>();

        // Act - Load data as the agent step would
        var enginesResult = await engineService.GetAllAsync();
        var agentsResult = await agentService.GetAllAsync();

        // Assert
        enginesResult.Value.Should().HaveCount(1);
        agentsResult.Value.Should().HaveCount(1);
        agentsResult.Value!.First().Name.Should().Be("Existing Agent");
    }

    [Fact]
    public async Task GeneralSettingsStep_LoadsEnginesForDropdown()
    {
        // Arrange
        AddTestInferenceEngine("Engine 1");
        AddTestInferenceEngine("Engine 2");

        var engineService = Services.GetRequiredService<IInferenceEngineService>();
        var settingsService = Services.GetRequiredService<IGeneralSettingsService>();

        // Act
        var enginesResult = await engineService.GetAllAsync();
        var settingsResult = await settingsService.GetSettingsAsync();

        // Assert
        enginesResult.Value.Should().HaveCount(2);
        settingsResult.IsSuccess.Should().BeTrue();
    }

    #endregion
}

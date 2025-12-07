using Aesir.Client.Web.Modules.Wizard.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Aesir.Client.Web.Tests.Unit.Services;

public class WizardStateServiceTests
{
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly WizardStateService _service;

    public WizardStateServiceTests()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();

        // Setup default behavior for void methods (localStorage.setItem, localStorage.removeItem)
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>(
                It.Is<string>(s => s == "localStorage.setItem" || s == "localStorage.removeItem"),
                It.IsAny<object[]>()))
            .ReturnsAsync((IJSVoidResult)null!);

        _service = new WizardStateService(_mockJsRuntime.Object);
    }

    [Fact]
    public void Steps_ShouldContainAllWizardSteps()
    {
        // Assert
        _service.Steps.Should().HaveCount(5);
        _service.Steps[0].Id.Should().Be("welcome");
        _service.Steps[1].Id.Should().Be("inference-engine");
        _service.Steps[2].Id.Should().Be("general-settings");
        _service.Steps[3].Id.Should().Be("agent");
        _service.Steps[4].Id.Should().Be("complete");
    }

    [Fact]
    public void CurrentStep_ShouldStartAtZero()
    {
        // Assert
        _service.CurrentStep.Should().Be(0);
    }

    [Fact]
    public void IsCompleted_ShouldStartAsFalse()
    {
        // Assert
        _service.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GoToNextStepAsync_ShouldIncrementStep()
    {
        // Arrange
        _service.CurrentStep.Should().Be(0);

        // Act
        var result = await _service.GoToNextStepAsync();

        // Assert
        result.Should().BeTrue();
        _service.CurrentStep.Should().Be(1);
    }

    [Fact]
    public async Task GoToNextStepAsync_ShouldReturnFalse_WhenAtLastStep()
    {
        // Arrange - Navigate to last step
        for (int i = 0; i < _service.Steps.Count - 1; i++)
        {
            await _service.GoToNextStepAsync();
        }
        _service.CurrentStep.Should().Be(4);

        // Act
        var result = await _service.GoToNextStepAsync();

        // Assert
        result.Should().BeFalse();
        _service.CurrentStep.Should().Be(4);
    }

    [Fact]
    public async Task GoToPreviousStep_ShouldDecrementStep()
    {
        // Arrange
        await _service.GoToNextStepAsync();
        await _service.GoToNextStepAsync();
        _service.CurrentStep.Should().Be(2);

        // Act
        _service.GoToPreviousStep();

        // Assert
        _service.CurrentStep.Should().Be(1);
    }

    [Fact]
    public void GoToPreviousStep_ShouldNotGoBelowZero()
    {
        // Arrange
        _service.CurrentStep.Should().Be(0);

        // Act
        _service.GoToPreviousStep();

        // Assert
        _service.CurrentStep.Should().Be(0);
    }

    [Fact]
    public void GoToStep_ShouldNavigateToSpecificStep()
    {
        // Act
        _service.GoToStep(3);

        // Assert
        _service.CurrentStep.Should().Be(3);
    }

    [Fact]
    public void GoToStep_ShouldIgnoreInvalidIndex()
    {
        // Act
        _service.GoToStep(-1);
        _service.CurrentStep.Should().Be(0);

        _service.GoToStep(100);
        _service.CurrentStep.Should().Be(0);
    }

    [Fact]
    public void CompleteStep_ShouldMarkStepAsCompleted()
    {
        // Arrange
        _service.Steps[1].IsCompleted.Should().BeFalse();

        // Act
        _service.CompleteStep(1);

        // Assert
        _service.Steps[1].IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void CompleteStep_ShouldIgnoreInvalidIndex()
    {
        // Act & Assert - should not throw
        _service.CompleteStep(-1);
        _service.CompleteStep(100);
    }

    [Fact]
    public async Task CompleteWizardAsync_ShouldSetIsCompletedToTrue()
    {
        // Act
        await _service.CompleteWizardAsync();

        // Assert
        _service.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteWizardAsync_ShouldPersistToLocalStorage()
    {
        // Act
        await _service.CompleteWizardAsync();

        // Assert
        _mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object[]>(args =>
                args.Length == 2 &&
                args[0].ToString() == "aesir_wizard_completed" &&
                args[1].ToString() == "true")),
            Times.Once);
    }

    [Fact]
    public async Task CheckWizardCompletedAsync_ShouldReturnTrue_WhenWizardWasCompleted()
    {
        // Arrange
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync("true");

        // Act
        var result = await _service.CheckWizardCompletedAsync();

        // Assert
        result.Should().BeTrue();
        _service.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CheckWizardCompletedAsync_ShouldReturnFalse_WhenWizardWasNotCompleted()
    {
        // Arrange
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _service.CheckWizardCompletedAsync();

        // Assert
        result.Should().BeFalse();
        _service.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task CheckWizardCompletedAsync_ShouldReturnFalse_WhenJsRuntimeThrows()
    {
        // Arrange
        _mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ThrowsAsync(new JSDisconnectedException("Disconnected"));

        // Act
        var result = await _service.CheckWizardCompletedAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetWizardAsync_ShouldResetAllState()
    {
        // Arrange - Navigate and complete
        await _service.GoToNextStepAsync();
        await _service.GoToNextStepAsync();
        _service.CompleteStep(0);
        _service.CompleteStep(1);
        await _service.CompleteWizardAsync();

        _service.CurrentStep.Should().Be(2);
        _service.IsCompleted.Should().BeTrue();

        // Act
        await _service.ResetWizardAsync();

        // Assert
        _service.CurrentStep.Should().Be(0);
        _service.IsCompleted.Should().BeFalse();
        _service.Steps.All(s => !s.IsCompleted).Should().BeTrue();
    }

    [Fact]
    public async Task StateChanged_ShouldBeRaised_WhenNavigating()
    {
        // Arrange
        var eventRaised = false;
        _service.StateChanged += (_, _) => eventRaised = true;

        // Act
        await _service.GoToNextStepAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void StateChanged_ShouldBeRaised_WhenGoingToPreviousStep()
    {
        // Arrange
        _service.GoToStep(2);
        var eventRaised = false;
        _service.StateChanged += (_, _) => eventRaised = true;

        // Act
        _service.GoToPreviousStep();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void StateChanged_ShouldBeRaised_WhenCompletingStep()
    {
        // Arrange
        var eventRaised = false;
        _service.StateChanged += (_, _) => eventRaised = true;

        // Act
        _service.CompleteStep(0);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task StateChanged_ShouldBeRaised_WhenCompletingWizard()
    {
        // Arrange
        var eventRaised = false;
        _service.StateChanged += (_, _) => eventRaised = true;

        // Act
        await _service.CompleteWizardAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }
}

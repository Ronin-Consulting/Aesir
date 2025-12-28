using Aesir.Client.Web.Modules.Research.Components;
using Aesir.Common.Models;
using Bunit;
using MudBlazor.Services;

namespace Aesir.Client.Web.Tests.Unit.Research.Components;

public class PhaseProgressBarTests : TestContext
{
    public PhaseProgressBarTests()
    {
        Services.AddMudServices();
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
    }

    [Fact]
    public void PhaseProgressBar_RendersAllPhases()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>();

        // Assert - should contain all 5 phase names
        cut.Markup.Should().Contain("Planning");
        cut.Markup.Should().Contain("Research");
        cut.Markup.Should().Contain("Anonymize");
        cut.Markup.Should().Contain("Review");
        cut.Markup.Should().Contain("Synthesis");
    }

    [Fact]
    public void PhaseProgressBar_WithNoPhase_ShowsAllPending()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>();

        // Assert - should have pending phase steps
        cut.Markup.Should().Contain("pending");
    }

    [Fact]
    public void PhaseProgressBar_WithPlanningPhase_ShowsPlanningActive()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, ResearchPhaseBase.Planning));

        // Assert
        cut.Markup.Should().Contain("active");
    }

    [Fact]
    public void PhaseProgressBar_WithResearchPhase_ShowsPlanningCompleted()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, ResearchPhaseBase.Research));

        // Assert
        cut.Markup.Should().Contain("completed");
        cut.Markup.Should().Contain("active");
    }

    [Fact]
    public void PhaseProgressBar_WhenCompleted_ShowsAllCompleted()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.IsCompleted, true));

        // Assert - all steps should be completed
        var stepElements = cut.FindAll(".phase-step");
        foreach (var step in stepElements)
        {
            step.ClassList.Should().Contain("completed");
        }
    }

    [Fact]
    public void PhaseProgressBar_WhenFailed_ShowsCorrectState()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, ResearchPhaseBase.PeerReview)
            .Add(p => p.IsFailed, true));

        // Assert
        cut.Markup.Should().Contain("pending");
    }

    [Fact]
    public void PhaseProgressBar_HasProgressIndicator()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, ResearchPhaseBase.Research));

        // Assert - should have phase-progress-bar container
        cut.Markup.Should().Contain("phase-progress-bar");
    }

    [Theory]
    [InlineData(ResearchPhaseBase.Planning)]
    [InlineData(ResearchPhaseBase.Research)]
    [InlineData(ResearchPhaseBase.Anonymization)]
    [InlineData(ResearchPhaseBase.PeerReview)]
    [InlineData(ResearchPhaseBase.Synthesis)]
    public void PhaseProgressBar_RendersForAllPhases(ResearchPhaseBase phase)
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, phase));

        // Assert - should render without errors
        cut.Markup.Should().NotBeNullOrEmpty();
        cut.Markup.Should().Contain("phase-step");
    }

    [Fact]
    public void PhaseProgressBar_ShowsStepConnectors()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>(parameters => parameters
            .Add(p => p.CurrentPhase, ResearchPhaseBase.Research));

        // Assert - connectors between steps (4 connectors for 5 steps)
        cut.Markup.Should().Contain("step-connector");
    }

    [Fact]
    public void PhaseProgressBar_ShowsStepNumbers()
    {
        // Act
        var cut = RenderComponent<PhaseProgressBar>();

        // Assert - should show step circles for pending items
        cut.Markup.Should().Contain("step-circle");
    }
}

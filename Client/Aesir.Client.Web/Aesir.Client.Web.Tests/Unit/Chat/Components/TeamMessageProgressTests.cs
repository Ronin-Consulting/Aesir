using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class TeamMessageProgressTests : TestContext
{
    public TeamMessageProgressTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Rendering Tests

    [Fact]
    public void Renders_ProgressSection()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        cut.Markup.Should().Contain("research-progress");
    }

    [Fact]
    public void Renders_ProgressBar()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        cut.Markup.Should().Contain("mud-progress-linear");
    }

    [Fact]
    public void Renders_ProgressPercent()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 75));

        // Assert
        cut.Markup.Should().Contain("75%");
    }

    [Fact]
    public void Renders_ProgressMessage_WhenProvided()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50)
            .Add(p => p.ProgressMessage, "Analyzing sources..."));

        // Assert
        cut.Markup.Should().Contain("progress-message");
        cut.Markup.Should().Contain("Analyzing sources...");
    }

    [Fact]
    public void DoesNotRender_ProgressMessage_WhenEmpty()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50)
            .Add(p => p.ProgressMessage, null));

        // Assert
        var messageElements = cut.FindAll(".progress-message");
        messageElements.Should().BeEmpty();
    }

    #endregion

    #region Phase Icon Tests

    [Theory]
    [InlineData(ResearchPhaseBase.Planning, "Planning Research")]
    [InlineData(ResearchPhaseBase.Research, "Conducting Research")]
    [InlineData(ResearchPhaseBase.Anonymization, "Anonymizing Work")]
    [InlineData(ResearchPhaseBase.PeerReview, "Peer Review")]
    [InlineData(ResearchPhaseBase.Synthesis, "Synthesizing Report")]
    public void Renders_PhaseName(ResearchPhaseBase phase, string expectedPhaseName)
    {
        // Arrange
        var session = CreateSession(phase);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        cut.Markup.Should().Contain(expectedPhaseName);
    }

    [Fact]
    public void Renders_Initializing_WhenNoPhase()
    {
        // Arrange
        var session = CreateSession(null);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 0));

        // Assert
        cut.Markup.Should().Contain("Initializing");
    }

    #endregion

    #region Phase Steps Tests

    [Fact]
    public void Renders_AllPhaseSteps()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        cut.Markup.Should().Contain("Planning");
        cut.Markup.Should().Contain("Research");
        cut.Markup.Should().Contain("Anonymize");
        cut.Markup.Should().Contain("Review");
        cut.Markup.Should().Contain("Synthesis");
    }

    [Fact]
    public void Renders_CompletedSteps_BeforeCurrentPhase()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.PeerReview);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 70));

        // Assert
        // Check for completed class on phase steps
        var completedSteps = cut.FindAll(".phase-step.completed");
        completedSteps.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Renders_ActiveStep_ForCurrentPhase()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        var activeSteps = cut.FindAll(".phase-step.active");
        activeSteps.Count.Should().Be(1);
    }

    [Fact]
    public void Renders_PendingSteps_AfterCurrentPhase()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Planning);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 10));

        // Assert
        var pendingSteps = cut.FindAll(".phase-step.pending");
        pendingSteps.Count.Should().BeGreaterThan(0);
    }

    #endregion

    #region Progress Indicator Tests

    [Fact]
    public void Renders_CheckIcon_ForCompletedPhases()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Synthesis);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 90));

        // Assert
        // Completed phases should have completed class with check icons (SVG paths)
        var completedSteps = cut.FindAll(".phase-step.completed");
        completedSteps.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Renders_Spinner_ForActivePhase()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Research);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 50));

        // Assert
        cut.Markup.Should().Contain("mud-progress-circular");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Renders_WithZeroPercent()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Planning);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 0));

        // Assert
        cut.Markup.Should().Contain("0%");
    }

    [Fact]
    public void Renders_WithHundredPercent()
    {
        // Arrange
        var session = CreateSession(ResearchPhaseBase.Synthesis);

        // Act
        var cut = RenderComponent<TeamMessageProgress>(parameters => parameters
            .Add(p => p.Session, session)
            .Add(p => p.ProgressPercent, 100));

        // Assert
        cut.Markup.Should().Contain("100%");
    }

    #endregion

    #region Helper Methods

    private static ResearchSessionBase CreateSession(ResearchPhaseBase? phase)
    {
        return new ResearchSessionBase
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Query = "Test research query",
            Status = ResearchStatusBase.Researching,
            Mode = ResearchModeBase.Standard,
            CurrentPhase = phase,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

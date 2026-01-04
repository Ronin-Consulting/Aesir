using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class TeamMessageDetailsTests : TestContext
{
    public TeamMessageDetailsTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Rendering Tests

    [Fact]
    public void Renders_DetailsSection()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("research-details");
    }

    [Fact]
    public void Renders_SessionDetailsSection()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Session Details");
    }

    [Fact]
    public void Renders_SessionIdPartial()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        // Should show first 8 characters of session ID
        var sessionIdPrefix = session.Id.ToString("N")[..8];
        cut.Markup.Should().Contain(sessionIdPrefix);
        cut.Markup.Should().Contain("...");
    }

    #endregion

    #region Mode Display Tests

    [Theory]
    [InlineData(ResearchModeBase.Quick, "Quick (No Peer Review)")]
    [InlineData(ResearchModeBase.Standard, "Standard (Single Round)")]
    [InlineData(ResearchModeBase.Deep, "Deep (Iterative)")]
    public void Renders_ModeName(ResearchModeBase mode, string expectedModeName)
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Mode = mode;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain(expectedModeName);
    }

    #endregion

    #region Status Chip Tests

    [Fact]
    public void Renders_StatusChip()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("mud-chip");
    }

    [Fact]
    public void Renders_SuccessColorChip_WhenCompleted()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("mud-chip-color-success");
    }

    [Fact]
    public void Renders_ErrorColorChip_WhenFailed()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Failed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("mud-chip-color-error");
    }

    [Fact]
    public void Renders_WarningColorChip_WhenAwaitingClarification()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.AwaitingClarification);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("mud-chip-color-warning");
    }

    #endregion

    #region Query Section Tests

    [Fact]
    public void Renders_QuerySection()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Research Query");
    }

    [Fact]
    public void Renders_OriginalQuery()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Query = "What are the latest trends in AI?";

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Original Query");
        cut.Markup.Should().Contain("What are the latest trends in AI?");
    }

    [Fact]
    public void Renders_RefinedQuery_WhenDifferentFromOriginal()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Query = "AI trends";
        session.RefinedQuery = "What are the latest trends in AI and machine learning for 2024?";

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Refined Query");
        cut.Markup.Should().Contain("What are the latest trends in AI and machine learning for 2024?");
    }

    [Fact]
    public void DoesNotRender_RefinedQuery_WhenSameAsOriginal()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Query = "What are the latest trends in AI?";
        session.RefinedQuery = "What are the latest trends in AI?";

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert - only one query block, no "refined" class
        var refinedBlocks = cut.FindAll(".query-block.refined");
        refinedBlocks.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_RefinedQuery_WhenNull()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Query = "What are the latest trends in AI?";
        session.RefinedQuery = null;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        var refinedBlocks = cut.FindAll(".query-block.refined");
        refinedBlocks.Should().BeEmpty();
    }

    #endregion

    #region Timeline Tests

    [Fact]
    public void Renders_TimelineSection()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Research Timeline");
        cut.Markup.Should().Contain("timeline");
    }

    [Fact]
    public void Renders_AllTimelinePhases()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Planning");
        cut.Markup.Should().Contain("Research");
        cut.Markup.Should().Contain("Anonymization");
        cut.Markup.Should().Contain("Peer Review");
        cut.Markup.Should().Contain("Synthesis");
    }

    [Fact]
    public void Renders_CompletedTimeline_WhenCompleted()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        var completedItems = cut.FindAll(".timeline-item.completed");
        completedItems.Count.Should().Be(5); // All 5 phases completed
    }

    [Fact]
    public void Renders_ActiveTimeline_ForCurrentPhase()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Researching);
        session.CurrentPhase = ResearchPhaseBase.Research;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        var activeItems = cut.FindAll(".timeline-item.active");
        activeItems.Count.Should().Be(1);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void Renders_CreatedAt()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.CreatedAt = new DateTime(2024, 1, 15, 14, 30, 0);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Created");
        cut.Markup.Should().Contain("Jan 15, 2024");
    }

    [Fact]
    public void Renders_StartedAt_WhenProvided()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = new DateTime(2024, 1, 15, 14, 35, 0);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Started");
    }

    [Fact]
    public void Renders_CompletedAt_WhenProvided()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = new DateTime(2024, 1, 15, 14, 35, 0);
        session.CompletedAt = new DateTime(2024, 1, 15, 14, 45, 0);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Completed");
    }

    [Fact]
    public void Renders_Duration_WhenStartedAndCompleted()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = DateTime.UtcNow.AddMinutes(-10);
        session.CompletedAt = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Duration");
        cut.Markup.Should().Contain("10m");
    }

    #endregion

    #region Error Section Tests

    [Fact]
    public void Renders_ErrorSection_WhenFailed()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Failed);
        session.ErrorMessage = "Network timeout while fetching sources";

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("error-section");
        cut.Markup.Should().Contain("Error Details");
        cut.Markup.Should().Contain("Network timeout while fetching sources");
    }

    [Fact]
    public void DoesNotRender_ErrorSection_WhenNotFailed()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        var errorSections = cut.FindAll(".error-section");
        errorSections.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_ErrorSection_WhenFailedButNoMessage()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Failed);
        session.ErrorMessage = null;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        var errorSections = cut.FindAll(".error-section");
        errorSections.Should().BeEmpty();
    }

    #endregion

    #region Duration Formatting Tests

    [Fact]
    public void FormatsDuration_InSeconds_WhenUnderOneMinute()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = DateTime.UtcNow.AddSeconds(-45);
        session.CompletedAt = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("45s");
    }

    [Fact]
    public void FormatsDuration_InMinutes_WhenUnderOneHour()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = DateTime.UtcNow.AddMinutes(-5).AddSeconds(-30);
        session.CompletedAt = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("5m");
    }

    [Fact]
    public void FormatsDuration_InHours_WhenOverOneHour()
    {
        // Arrange
        var session = CreateSession(ResearchStatusBase.Completed);
        session.StartedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(-30);
        session.CompletedAt = DateTime.UtcNow;

        // Act
        var cut = RenderComponent<TeamMessageDetails>(parameters => parameters
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("1h");
        cut.Markup.Should().Contain("30m");
    }

    #endregion

    #region Helper Methods

    private static ResearchSessionBase CreateSession(ResearchStatusBase status)
    {
        return new ResearchSessionBase
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Query = "Test research query",
            Status = status,
            Mode = ResearchModeBase.Standard,
            CurrentPhase = status == ResearchStatusBase.Completed ? ResearchPhaseBase.Synthesis : ResearchPhaseBase.Research,
            CreatedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = status == ResearchStatusBase.Completed ? DateTime.UtcNow : null
        };
    }

    #endregion
}

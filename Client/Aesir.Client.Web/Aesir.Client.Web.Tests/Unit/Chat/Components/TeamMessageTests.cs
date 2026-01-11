using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class TeamMessageTests : TestContext
{
    private readonly Mock<IMarkdownService> _markdownServiceMock;
    private readonly Mock<IResearchStateService> _researchStateServiceMock;
    private readonly Mock<IResearchSessionApiService> _researchApiServiceMock;

    public TeamMessageTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _markdownServiceMock = new Mock<IMarkdownService>();
        _markdownServiceMock
            .Setup(m => m.ToHtml(It.IsAny<string>()))
            .Returns<string>(s => $"<p>{s}</p>");

        _researchStateServiceMock = new Mock<IResearchStateService>();
        _researchStateServiceMock.Setup(m => m.CurrentProgressMessage).Returns("Processing...");
        _researchStateServiceMock.Setup(m => m.CurrentProgressPercent).Returns(50);

        _researchApiServiceMock = new Mock<IResearchSessionApiService>();
        _researchApiServiceMock
            .Setup(m => m.GetReportMarkdownAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<string>.Success("# Report Content"));

        Services.AddSingleton(_markdownServiceMock.Object);
        Services.AddSingleton(_researchStateServiceMock.Object);
        Services.AddSingleton(_researchApiServiceMock.Object);
    }

    #region Rendering Tests

    [Fact]
    public void Renders_TeamMessage()
    {
        // Arrange
        var message = CreateResearchTeamMessage();

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("team-message");
    }

    [Fact]
    public void Renders_TeamName()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        message.ResearchTeamName = "Legal Research Team";

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Legal Research Team");
    }

    [Fact]
    public void Renders_DefaultTeamName_WhenNull()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        message.ResearchTeamName = null;

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Research Team");
    }

    [Fact]
    public void Renders_TeamIcon()
    {
        // Arrange
        var message = CreateResearchTeamMessage();

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("team-icon");
    }

    [Fact]
    public void Renders_MessageContent()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        message.Content = "This is the research report content.";

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("This is the research report content.");
    }

    [Fact]
    public void Renders_Timestamp_WhenCreatedAtProvided()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        message.CreatedAt = DateTimeOffset.Now.AddMinutes(-5);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("message-timestamp");
    }

    #endregion

    #region Status Tests

    [Fact]
    public void Renders_StatusActive_WhenResearching()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Researching);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("status-active");
    }

    [Fact]
    public void Renders_StatusCompleted_WhenCompleted()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("status-completed");
    }

    [Fact]
    public void Renders_StatusFailed_WhenFailed()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Failed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("status-failed");
    }

    [Fact]
    public void Renders_StatusPending_WhenAwaitingClarification()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.AwaitingClarification);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("status-pending");
    }

    [Fact]
    public void Renders_StatusCancelled_WhenCancelled()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Cancelled);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("status-cancelled");
    }

    #endregion

    #region Progress Section Tests

    [Fact]
    public void Renders_ProgressSection_WhenResearchActive()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Researching);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("research-progress");
    }

    [Fact]
    public void DoesNotRender_ProgressSection_WhenCompleted()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert - no progress section when completed
        var progress = cut.FindAll(".research-progress");
        progress.Should().BeEmpty();
    }

    #endregion

    #region Clarification Section Tests

    [Fact]
    public void Renders_ClarificationSection_WhenAwaitingClarification()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.AwaitingClarification);
        session.ClarificationQuestions = new List<string>
        {
            "What specific aspect would you like to focus on?",
            "What is the time frame for this research?"
        };

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("clarification-section");
        cut.Markup.Should().Contain("What specific aspect would you like to focus on?");
    }

    [Fact]
    public void DoesNotRender_ClarificationSection_WhenNotAwaitingClarification()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Researching);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        var clarification = cut.FindAll(".clarification-section");
        clarification.Should().BeEmpty();
    }

    #endregion

    #region Cancel Button Tests

    [Fact]
    public void Renders_CancelButton_WhenResearchActive()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Researching);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Cancel");
    }

    [Fact]
    public void DoesNotRender_CancelButton_WhenCompleted()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert - Look for button with Cancel text
        var cancelButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Cancel"));
        cancelButtons.Should().BeEmpty();
    }

    #endregion

    #region Error Display Tests

    [Fact]
    public void Renders_ErrorAlert_WhenFailed()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Failed);
        session.ErrorMessage = "Research failed due to network error";

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("mud-alert-text-error");
        cut.Markup.Should().Contain("Research failed due to network error");
    }

    [Fact]
    public void DoesNotRender_ErrorAlert_WhenNotFailed()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        var alerts = cut.FindAll(".mud-alert-text-error");
        alerts.Should().BeEmpty();
    }

    #endregion

    #region Report Section Tests

    [Fact]
    public void Renders_ReportSection_WhenCompletedWithReport()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);
        session.Report = new ResearchReportSummaryBase
        {
            Id = Guid.NewGuid(),
            Title = "Research Report Title",
            ExecutiveSummary = "Executive summary content",
            FindingCount = 5,
            SourceCount = 10,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert - TeamMessageReport now uses "professional-report" as the outer class
        cut.Markup.Should().Contain("professional-report");
        cut.Markup.Should().Contain("Research Report Title");
    }

    [Fact]
    public void DoesNotRender_ReportSection_WhenInProgress()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Researching);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        var reports = cut.FindAll(".research-report");
        reports.Should().BeEmpty();
    }

    #endregion

    #region Theme Tests

    [Fact]
    public void Renders_DarkTheme_WhenIsDarkModeTrue()
    {
        // Arrange
        var message = CreateResearchTeamMessage();

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .AddCascadingValue("IsDarkMode", true));

        // Assert
        cut.Markup.Should().Contain("theme-dark");
    }

    [Fact]
    public void Renders_LightTheme_WhenIsDarkModeFalse()
    {
        // Arrange
        var message = CreateResearchTeamMessage();

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .AddCascadingValue("IsDarkMode", false));

        // Assert
        cut.Markup.Should().Contain("theme-light");
    }

    #endregion

    #region Action Button Tests

    [Fact]
    public void Renders_CopyButton()
    {
        // Arrange
        var message = CreateResearchTeamMessage();

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message));

        // Assert
        cut.Markup.Should().Contain("Copy content");
    }

    [Fact]
    public void Renders_ExpandDetailsButton_WhenCompleted()
    {
        // Arrange
        var message = CreateResearchTeamMessage();
        var session = CreateSession(ResearchStatusBase.Completed);

        // Act
        var cut = RenderComponent<TeamMessage>(parameters => parameters
            .Add(p => p.Message, message)
            .Add(p => p.Session, session));

        // Assert
        cut.Markup.Should().Contain("Show details");
    }

    #endregion

    #region Helper Methods

    private static AesirChatMessage CreateResearchTeamMessage()
    {
        return AesirChatMessage.NewResearchTeamMessage(
            "Test research content",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Research Team");
    }

    private static ResearchSessionBase CreateSession(ResearchStatusBase status)
    {
        return new ResearchSessionBase
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            Query = "Test research query",
            Status = status,
            Mode = ResearchModeBase.Standard,
            CurrentPhase = status switch
            {
                ResearchStatusBase.Planning => ResearchPhaseBase.Planning,
                ResearchStatusBase.Researching => ResearchPhaseBase.Research,
                ResearchStatusBase.PeerReviewing => ResearchPhaseBase.PeerReview,
                ResearchStatusBase.Synthesizing => ResearchPhaseBase.Synthesis,
                _ => null
            },
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class TeamMessageReportTests : TestContext
{
    private readonly Mock<IResearchSessionApiService> _researchApiMock;
    private readonly Mock<IMarkdownService> _markdownServiceMock;

    public TeamMessageReportTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _researchApiMock = new Mock<IResearchSessionApiService>();
        _markdownServiceMock = new Mock<IMarkdownService>();

        _markdownServiceMock
            .Setup(m => m.ToHtml(It.IsAny<string>()))
            .Returns<string>(s => $"<p>{s}</p>");

        Services.AddSingleton(_researchApiMock.Object);
        Services.AddSingleton(_markdownServiceMock.Object);
    }

    #region Rendering Tests

    [Fact]
    public void Renders_ReportSection()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("research-report");
    }

    [Fact]
    public void Renders_ReportTitle()
    {
        // Arrange
        var report = CreateReport();
        report.Title = "AI in Healthcare: A Comprehensive Analysis";

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("AI in Healthcare: A Comprehensive Analysis");
    }

    [Fact]
    public void Renders_ReportIcon()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("report-icon");
    }

    [Fact]
    public void Renders_CompleteChip()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("Complete");
    }

    #endregion

    #region Executive Summary Tests

    [Fact]
    public void Renders_ExecutiveSummary_WhenProvided()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = "This report examines the latest developments in AI healthcare applications.";

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("executive-summary");
        cut.Markup.Should().Contain("This report examines the latest developments in AI healthcare applications.");
    }

    [Fact]
    public void DoesNotRender_ExecutiveSummary_WhenNull()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = null;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        var summaryElements = cut.FindAll(".executive-summary");
        summaryElements.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_ExecutiveSummary_WhenEmpty()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = string.Empty;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        var summaryElements = cut.FindAll(".executive-summary");
        summaryElements.Should().BeEmpty();
    }

    #endregion

    #region Stats Tests

    [Fact]
    public void Renders_FindingCount()
    {
        // Arrange
        var report = CreateReport();
        report.FindingCount = 12;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("12");
        cut.Markup.Should().Contain("Key Findings");
    }

    [Fact]
    public void Renders_SourceCount()
    {
        // Arrange
        var report = CreateReport();
        report.SourceCount = 25;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("25");
        cut.Markup.Should().Contain("Sources Cited");
    }

    [Fact]
    public void Renders_Duration()
    {
        // Arrange
        var report = CreateReport();
        report.CreatedAt = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("Duration");
    }

    [Fact]
    public void Renders_StatsSection()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("report-stats");
    }

    #endregion

    #region Action Button Tests

    [Fact]
    public void Renders_ViewFullReportButton()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("View Full Report");
    }

    [Fact]
    public void Renders_DownloadButton()
    {
        // Arrange
        var report = CreateReport();

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("Download");
    }

    [Fact]
    public void TogglesReportVisibility_OnViewFullReportClick()
    {
        // Arrange
        var report = CreateReport();
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert initial state - no full report preview
        var initialPreview = cut.FindAll(".full-report-preview");
        initialPreview.Should().BeEmpty();

        // Act - click view full report
        var viewButton = cut.FindAll("button").First(b => b.TextContent.Contains("View Full Report"));
        viewButton.Click();

        // Assert - full report preview shown
        cut.Markup.Should().Contain("full-report-preview");

        // Act - click again (now shows "Hide Full Report")
        var hideButton = cut.FindAll("button").First(b => b.TextContent.Contains("Hide Full Report"));
        hideButton.Click();

        // Assert - full report preview hidden
        var finalPreview = cut.FindAll(".full-report-preview");
        finalPreview.Should().BeEmpty();
    }

    #endregion

    #region Report Meta Tests

    [Fact]
    public void Renders_ReportMeta()
    {
        // Arrange
        var report = CreateReport();
        report.FindingCount = 5;
        report.SourceCount = 10;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("5 findings");
        cut.Markup.Should().Contain("10 sources");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Renders_WithZeroFindings()
    {
        // Arrange
        var report = CreateReport();
        report.FindingCount = 0;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("0");
    }

    [Fact]
    public void Renders_WithZeroSources()
    {
        // Arrange
        var report = CreateReport();
        report.SourceCount = 0;

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.Markup.Should().Contain("0");
    }

    #endregion

    #region Helper Methods

    private static ResearchReportSummaryBase CreateReport()
    {
        return new ResearchReportSummaryBase
        {
            Id = Guid.NewGuid(),
            Title = "Test Research Report",
            ExecutiveSummary = "This is an executive summary.",
            FindingCount = 5,
            SourceCount = 10,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

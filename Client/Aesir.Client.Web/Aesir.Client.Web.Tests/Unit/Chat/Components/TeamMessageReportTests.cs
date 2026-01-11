using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class TeamMessageReportTests : TestContext
{
    private readonly Mock<IResearchSessionApiService> _researchApiMock;
    private readonly Mock<IMarkdownService> _markdownServiceMock;
    private readonly Mock<ISnackbar> _snackbarMock;

    public TeamMessageReportTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _researchApiMock = new Mock<IResearchSessionApiService>();
        _markdownServiceMock = new Mock<IMarkdownService>();
        _snackbarMock = new Mock<ISnackbar>();

        _markdownServiceMock
            .Setup(m => m.ToHtml(It.IsAny<string>()))
            .Returns<string>(s => $"<p>{s}</p>");

        Services.AddSingleton(_researchApiMock.Object);
        Services.AddSingleton(_markdownServiceMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
    }

    /// <summary>
    /// Sets up the mock to return a full report when GetReportAsync is called.
    /// </summary>
    private void SetupFullReportMock(ResearchReportSummaryBase summary)
    {
        var fullReport = new ResearchReportBase
        {
            Id = summary.Id,
            Title = summary.Title,
            ExecutiveSummary = summary.ExecutiveSummary,
            CreatedAt = summary.CreatedAt,
            Findings = new List<ResearchFindingBase>
            {
                new ResearchFindingBase { Title = "Test Finding", Content = "Test content", Confidence = "High" }
            },
            Bibliography = new List<ResearchSourceBase>
            {
                new ResearchSourceBase { Title = "Test Source", Author = "Test Author" }
            }
        };

        _researchApiMock
            .Setup(x => x.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchReportBase>.Success(fullReport));
    }

    #region Rendering Tests

    [Fact]
    public void Renders_ReportDocument_WhenLoaded()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert - Wait for async load and check document is rendered
        cut.WaitForState(() => cut.Markup.Contains("report-document"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("report-document");
    }

    [Fact]
    public void Renders_ReportTitle()
    {
        // Arrange
        var report = CreateReport();
        report.Title = "AI in Healthcare: A Comprehensive Analysis";
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("document-title"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("AI in Healthcare: A Comprehensive Analysis");
    }

    [Fact]
    public void Renders_DocumentHeader()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("document-header"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("document-header");
    }

    [Fact]
    public void Renders_DocumentMeta()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("document-meta"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("document-meta");
    }

    #endregion

    #region Executive Summary Tests

    [Fact]
    public void Renders_ExecutiveSummary_WhenProvided()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = "This report examines the latest developments in AI healthcare applications.";
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("executive-summary-section"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("executive-summary-section");
        cut.Markup.Should().Contain("This report examines the latest developments in AI healthcare applications.");
    }

    [Fact]
    public void DoesNotRender_ExecutiveSummary_WhenNull()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = null;
        SetupFullReportMockWithCustomReport(report, executiveSummary: null);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("report-document"), TimeSpan.FromSeconds(2));
        var summaryElements = cut.FindAll(".executive-summary-section");
        summaryElements.Should().BeEmpty();
    }

    [Fact]
    public void DoesNotRender_ExecutiveSummary_WhenEmpty()
    {
        // Arrange
        var report = CreateReport();
        report.ExecutiveSummary = string.Empty;
        SetupFullReportMockWithCustomReport(report, executiveSummary: string.Empty);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("report-document"), TimeSpan.FromSeconds(2));
        var summaryElements = cut.FindAll(".executive-summary-section");
        summaryElements.Should().BeEmpty();
    }

    #endregion

    #region Findings and Sources Tests

    [Fact]
    public void Renders_FindingsSection_WhenFindingsExist()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("findings-list"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("findings-list");
        cut.Markup.Should().Contain("Key Findings");
    }

    [Fact]
    public void Renders_SourcesSection_WhenSourcesExist()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("sources-list"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("sources-list");
        cut.Markup.Should().Contain("Sources");
    }

    [Fact]
    public void Renders_FindingsCount_InMeta()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert - The full report has 1 finding in our mock
        cut.WaitForState(() => cut.Markup.Contains("1 findings"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("1 findings");
    }

    [Fact]
    public void Renders_SourcesCount_InMeta()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert - The full report has 1 source in our mock
        cut.WaitForState(() => cut.Markup.Contains("1 sources"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("1 sources");
    }

    #endregion

    #region Action Button Tests

    [Fact]
    public void Renders_DownloadButton()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Download"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("Download");
    }

    [Fact]
    public void Renders_CopyButton()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Copy"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("Copy");
    }

    [Fact]
    public void Renders_TechnicalDetailsPanel()
    {
        // Arrange
        var report = CreateReport();
        SetupFullReportMock(report);

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert
        cut.WaitForState(() => cut.Markup.Contains("Technical Details"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("Technical Details");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Shows_FallbackContent_WhenLoadFails()
    {
        // Arrange - Set up mock to return failure
        var report = CreateReport();
        report.Title = "Fallback Report Title";

        _researchApiMock
            .Setup(x => x.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchReportBase>.Failure("Report not found"));

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert - Shows fallback with title
        cut.WaitForState(() => cut.Markup.Contains("report-fallback"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("report-fallback");
        cut.Markup.Should().Contain("Fallback Report Title");
    }

    [Fact]
    public void Shows_ErrorMessage_WhenLoadFails()
    {
        // Arrange - Set up mock to return failure
        var report = CreateReport();

        _researchApiMock
            .Setup(x => x.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchReportBase>.Failure("Report not found"));

        // Act
        var cut = RenderComponent<TeamMessageReport>(parameters => parameters
            .Add(p => p.Report, report));

        // Assert - Note: the component shows "Unable to load full report details" not "Failed to load report"
        cut.WaitForState(() => cut.Markup.Contains("Unable to load"), TimeSpan.FromSeconds(2));
        cut.Markup.Should().Contain("Unable to load full report details");
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

    /// <summary>
    /// Sets up the mock to return a full report with custom properties.
    /// </summary>
    private void SetupFullReportMockWithCustomReport(
        ResearchReportSummaryBase summary,
        string? executiveSummary = null,
        List<ResearchFindingBase>? findings = null,
        List<ResearchSourceBase>? bibliography = null)
    {
        var fullReport = new ResearchReportBase
        {
            Id = summary.Id,
            Title = summary.Title,
            ExecutiveSummary = executiveSummary,
            CreatedAt = summary.CreatedAt,
            Findings = findings ?? new List<ResearchFindingBase>(),
            Bibliography = bibliography ?? new List<ResearchSourceBase>()
        };

        _researchApiMock
            .Setup(x => x.GetReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<ResearchReportBase>.Success(fullReport));
    }

    #endregion
}

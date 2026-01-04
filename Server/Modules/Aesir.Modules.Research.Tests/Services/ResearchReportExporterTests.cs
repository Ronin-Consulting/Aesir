using Aesir.Infrastructure.Documents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DocFormat = Aesir.Infrastructure.Documents.DocumentFormat;

namespace Aesir.Modules.Research.Tests.Services;

public class ResearchReportExporterTests
{
    private readonly Mock<ILogger<ResearchReportExporter>> _loggerMock;
    private readonly Mock<IDocumentExporterFactory> _factoryMock;
    private readonly Mock<IDocumentExporter> _exporterMock;
    private readonly ResearchReportExporter _exporter;

    public ResearchReportExporterTests()
    {
        _loggerMock = new Mock<ILogger<ResearchReportExporter>>();
        _factoryMock = new Mock<IDocumentExporterFactory>();
        _exporterMock = new Mock<IDocumentExporter>();

        _factoryMock
            .Setup(f => f.GetExporter(It.IsAny<DocFormat>()))
            .Returns(_exporterMock.Object);

        _exporter = new ResearchReportExporter(_loggerMock.Object, _factoryMock.Object);
    }

    #region ExportAsync Tests

    [Fact]
    public async Task ExportAsync_WithValidReport_ReturnsSuccessResult()
    {
        // Arrange
        var report = CreateSampleReport();
        var expectedBytes = new byte[] { 0x50, 0x44, 0x46 };

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Succeeded(expectedBytes, "application/pdf", "report.pdf"));

        // Act
        var result = await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(expectedBytes);
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().Be("report.pdf");
    }

    [Fact]
    public async Task ExportAsync_WithWordFormat_CallsWordExporter()
    {
        // Arrange
        var report = CreateSampleReport();
        var expectedBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04 };

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Succeeded(expectedBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "report.docx"));

        // Act
        var result = await _exporter.ExportAsync(report, DocFormat.Word);

        // Assert
        _factoryMock.Verify(f => f.GetExporter(DocFormat.Word), Times.Once);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExportAsync_WithExporterFailure_ReturnsFailureResult()
    {
        // Arrange
        var report = CreateSampleReport();

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExportResult.Failed("Export failed"));

        // Act
        var result = await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Export failed");
    }

    [Fact]
    public async Task ExportAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        var report = CreateSampleReport();

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Exporter crashed"));

        // Act
        var result = await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Exporter crashed");
    }

    [Fact]
    public async Task ExportAsync_PassesCorrectDocumentToExporter()
    {
        // Arrange
        var report = CreateSampleReport();
        ExportDocument? capturedDocument = null;

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ExportDocument, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync(ExportResult.Succeeded([], "application/pdf", "report.pdf"));

        // Act
        await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        capturedDocument.Should().NotBeNull();
        capturedDocument!.Title.Should().Be(report.Title);
        capturedDocument.Author.Should().Be("Aesir Research Team");
        capturedDocument.IncludePageNumbers.Should().BeTrue();
    }

    [Fact]
    public async Task ExportAsync_WithExecutiveSummary_AddsExecutiveSummarySection()
    {
        // Arrange
        var report = CreateSampleReport();
        report.ExecutiveSummary = "This is the executive summary.";
        ExportDocument? capturedDocument = null;

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ExportDocument, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync(ExportResult.Succeeded([], "application/pdf", "report.pdf"));

        // Act
        await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        capturedDocument!.Sections.Should().Contain(s => s.Heading == "Executive Summary");
    }

    [Fact]
    public async Task ExportAsync_WithFindings_AddsKeyFindingsSection()
    {
        // Arrange
        var report = CreateSampleReport();
        report.Findings = new List<ResearchFinding>
        {
            new()
            {
                Title = "Finding 1",
                Content = "Content 1",
                Confidence = ConfidenceLevel.High,
                SupportingEvidence = ["Evidence 1", "Evidence 2"],
                Sources = [new ResearchSource { Title = "Source 1", Url = "https://example.com" }]
            }
        };
        ExportDocument? capturedDocument = null;

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ExportDocument, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync(ExportResult.Succeeded([], "application/pdf", "report.pdf"));

        // Act
        await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        capturedDocument!.Sections.Should().Contain(s => s.Heading == "Key Findings");
    }

    [Fact]
    public async Task ExportAsync_WithBibliography_AddsBibliographySection()
    {
        // Arrange
        var report = CreateSampleReport();
        report.Bibliography = new List<ResearchSource>
        {
            new() { Title = "Bibliography Entry 1", SourceType = "Web", Url = "https://example.com" }
        };
        ExportDocument? capturedDocument = null;

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ExportDocument, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync(ExportResult.Succeeded([], "application/pdf", "report.pdf"));

        // Act
        await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        capturedDocument!.Sections.Should().Contain(s => s.Heading == "Bibliography");
    }

    [Fact]
    public async Task ExportAsync_WithMetadata_AddsAppendixSection()
    {
        // Arrange
        var report = CreateSampleReport();
        report.Metadata = new ReportMetadata
        {
            TotalDurationMs = 120000,
            TotalTokensUsed = 50000,
            SourceCount = 15,
            FindingCount = 5,
            AveragePeerReviewScore = 8.5
        };
        ExportDocument? capturedDocument = null;

        _exporterMock
            .Setup(e => e.ExportAsync(It.IsAny<ExportDocument>(), It.IsAny<CancellationToken>()))
            .Callback<ExportDocument, CancellationToken>((doc, _) => capturedDocument = doc)
            .ReturnsAsync(ExportResult.Succeeded([], "application/pdf", "report.pdf"));

        // Act
        await _exporter.ExportAsync(report, DocFormat.Pdf);

        // Assert
        capturedDocument!.Sections.Should().Contain(s => s.Heading == "Appendix: Research Metadata");
    }

    #endregion

    #region GetAvailableFormats Tests

    [Fact]
    public void GetAvailableFormats_ReturnsFormatsFromFactory()
    {
        // Arrange
        var pdfExporter = new Mock<IDocumentExporter>();
        pdfExporter.Setup(e => e.Format).Returns(DocFormat.Pdf);

        var wordExporter = new Mock<IDocumentExporter>();
        wordExporter.Setup(e => e.Format).Returns(DocFormat.Word);

        _factoryMock
            .Setup(f => f.GetAllExporters())
            .Returns([pdfExporter.Object, wordExporter.Object]);

        // Act
        var formats = _exporter.GetAvailableFormats().ToList();

        // Assert
        formats.Should().HaveCount(2);
        formats.Should().Contain(DocFormat.Pdf);
        formats.Should().Contain(DocFormat.Word);
    }

    [Fact]
    public void GetAvailableFormats_WithNoExporters_ReturnsEmptyList()
    {
        // Arrange
        _factoryMock
            .Setup(f => f.GetAllExporters())
            .Returns([]);

        // Act
        var formats = _exporter.GetAvailableFormats().ToList();

        // Assert
        formats.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private static ResearchReport CreateSampleReport()
    {
        return new ResearchReport
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Title = "Sample Research Report",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

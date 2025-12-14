using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Moq;
using Aesir.Client.Web.Modules.Chat.Components;
using Aesir.Client.Web.Modules.Chat.Models;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Infrastructure.Services;

namespace Aesir.Client.Web.Tests.Unit.Chat.Components;

public class CitationViewerTests : TestContext
{
    private readonly Mock<ICitationStateService> _citationStateServiceMock;
    private readonly Mock<IDocumentApiService> _documentApiServiceMock;
    private readonly Mock<IPlatformDetectionService> _platformDetectionMock;
    private readonly Mock<INativeFileService> _nativeFileServiceMock;
    private readonly Mock<ISnackbar> _snackbarMock;

    public CitationViewerTests()
    {
        _citationStateServiceMock = new Mock<ICitationStateService>();
        _documentApiServiceMock = new Mock<IDocumentApiService>();
        _platformDetectionMock = new Mock<IPlatformDetectionService>();
        _nativeFileServiceMock = new Mock<INativeFileService>();
        _snackbarMock = new Mock<ISnackbar>();

        Services.AddMudServices();
        Services.AddSingleton(_citationStateServiceMock.Object);
        Services.AddSingleton(_documentApiServiceMock.Object);
        Services.AddSingleton(_platformDetectionMock.Object);
        Services.AddSingleton(_nativeFileServiceMock.Object);
        Services.AddSingleton(_snackbarMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>
    /// Helper to create a CitationInfo with all required properties.
    /// </summary>
    private static CitationInfo CreateCitation(
        string fileName = "document.pdf",
        CitationFileType fileType = CitationFileType.Pdf,
        int? pageNumber = null)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        var conversationId = "conv-123";
        return new CitationInfo
        {
            ConversationId = conversationId,
            FileName = fileName,
            FileExtension = extension,
            FileType = fileType,
            PageNumber = pageNumber,
            OriginalUrl = $"file:///{conversationId}/{fileName}"
        };
    }

    /// <summary>
    /// Helper to create CitationFileMetadata with all required properties.
    /// </summary>
    private static CitationFileMetadata CreateMetadata(
        string fileName = "document.pdf",
        long fileSize = 1024)
    {
        return new CitationFileMetadata
        {
            FileName = fileName,
            MimeType = "application/pdf",
            FileSize = fileSize
        };
    }

    #region Basic Rendering

    [Fact]
    public void DoesNotRender_WhenViewerIsClosed()
    {
        // Arrange
        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(false);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns((CitationInfo?)null);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert - the overlay element should not be in the DOM (only style may be rendered)
        var overlayElements = cut.FindAll(".citation-viewer-overlay");
        overlayElements.Should().BeEmpty();
    }

    [Fact]
    public void Renders_WhenViewerIsOpen()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("citation-viewer-overlay");
        cut.Markup.Should().Contain("citation-viewer-container");
    }

    [Fact]
    public void Renders_FileName_InHeader()
    {
        // Arrange
        var citation = CreateCitation(fileName: "test-document.pdf");

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("test-document.pdf");
        cut.Markup.Should().Contain("citation-file-name");
    }

    #endregion

    #region Page Number Badge

    [Fact]
    public void Renders_PageBadge_WhenPageNumberProvided()
    {
        // Arrange
        var citation = CreateCitation(pageNumber: 5);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("citation-page-badge");
        cut.Markup.Should().Contain("Page 5");
    }

    [Fact]
    public void DoesNotRender_PageBadge_WhenNoPageNumber()
    {
        // Arrange
        var citation = CreateCitation(pageNumber: null);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert - the page badge element should not be in the DOM
        var pageBadgeElements = cut.FindAll(".citation-page-badge");
        pageBadgeElements.Should().BeEmpty();
    }

    #endregion

    #region Action Buttons

    [Fact]
    public void Renders_CloseButton()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("citation-close-btn");
        cut.Markup.Should().Contain("aria-label=\"Close viewer\"");
    }

    [Fact]
    public void Renders_DownloadButton()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-label=\"Download file\"");
    }

    [Fact]
    public void Renders_CopyLinkButton()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-label=\"Copy citation link\"");
    }

    [Fact]
    public void Renders_CopyAsMarkdownButton()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-label=\"Copy as Markdown\"");
    }

    [Fact]
    public void Renders_OpenInNewTabButton_WhenNotTauri()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);
        _platformDetectionMock.Setup(x => x.IsTauri).Returns(false);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-label=\"Open in new tab\"");
    }

    [Fact]
    public void Renders_OpenInNativeAppButton_WhenTauri()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);
        _platformDetectionMock.Setup(x => x.IsTauri).Returns(true);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-label=\"Open in default app\"");
    }

    #endregion

    #region Accessibility

    [Fact]
    public void HasDialogRole_WhenOpen()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("role=\"dialog\"");
        cut.Markup.Should().Contain("aria-modal=\"true\"");
    }

    [Fact]
    public void HasAriaLabelledBy_ForTitle()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("aria-labelledby=\"citation-viewer-title\"");
        cut.Markup.Should().Contain("id=\"citation-viewer-title\"");
    }

    #endregion

    #region Content Areas

    [Fact]
    public void HasContentArea_WithMainRole()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("citation-viewer-content");
        cut.Markup.Should().Contain("role=\"main\"");
    }

    #endregion

    #region File Type Icons

    [Theory]
    [InlineData(CitationFileType.Pdf)]
    [InlineData(CitationFileType.Image)]
    [InlineData(CitationFileType.Text)]
    [InlineData(CitationFileType.Json)]
    public void Renders_FileIcon_ForFileType(CitationFileType fileType)
    {
        // Arrange
        var citation = CreateCitation(fileType: fileType);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Act
        var cut = RenderComponent<CitationViewer>();

        // Assert
        cut.Markup.Should().Contain("citation-file-icon");
        cut.Markup.Should().Contain("<svg"); // SVG icon should be present
    }

    #endregion

    #region Close Behavior

    [Fact]
    public void CallsCloseCitation_WhenCloseButtonClicked()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        var cut = RenderComponent<CitationViewer>();

        // Act
        cut.Find(".citation-close-btn").Click();

        // Assert
        _citationStateServiceMock.Verify(x => x.CloseCitation(), Times.Once);
    }

    [Fact]
    public void CallsCloseCitation_WhenOverlayClicked()
    {
        // Arrange
        var citation = CreateCitation();

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        var cut = RenderComponent<CitationViewer>();

        // Act
        cut.Find(".citation-viewer-overlay").Click();

        // Assert
        _citationStateServiceMock.Verify(x => x.CloseCitation(), Times.Once);
    }

    #endregion

    #region Deleted Document Tests

    [Fact]
    public async Task ShowsErrorMessage_WhenDocumentIsDeleted_Pdf()
    {
        // Arrange
        var citation = CreateCitation(fileName: "deleted-document.pdf", fileType: CitationFileType.Pdf);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - Should show error message, not the PDF viewer
        cut.Markup.Should().Contain("citation-error");
        cut.Markup.Should().Contain("Unable to load document");
        cut.Markup.Should().Contain("This document has been removed from the conversation");
    }

    [Fact]
    public async Task ShowsErrorMessage_WhenDocumentIsDeleted_Image()
    {
        // Arrange
        var citation = CreateCitation(fileName: "deleted-image.png", fileType: CitationFileType.Image);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - Should show error message, not the image viewer
        cut.Markup.Should().Contain("citation-error");
        cut.Markup.Should().Contain("This document has been removed from the conversation");
    }

    [Fact]
    public async Task ShowsErrorMessage_WhenDocumentIsDeleted_TextFile()
    {
        // Arrange
        var citation = CreateCitation(fileName: "deleted-file.txt", fileType: CitationFileType.Text);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - Should show error message, not the text viewer
        cut.Markup.Should().Contain("citation-error");
        cut.Markup.Should().Contain("This document has been removed from the conversation");
    }

    [Fact]
    public async Task ShowsRetryButton_WhenDocumentIsDeleted()
    {
        // Arrange
        var citation = CreateCitation(fileName: "deleted-document.pdf", fileType: CitationFileType.Pdf);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - Should show retry button
        cut.Markup.Should().Contain("Retry");
    }

    [Fact]
    public async Task DoesNotShowPdfViewer_WhenDocumentIsDeleted()
    {
        // Arrange
        var citation = CreateCitation(fileName: "deleted-document.pdf", fileType: CitationFileType.Pdf);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - PDF viewer should NOT be rendered (no iframe)
        cut.FindAll("iframe").Should().BeEmpty();
    }

    [Fact]
    public async Task LoadsContentSuccessfully_WhenDocumentExists()
    {
        // Arrange
        var citation = CreateCitation(fileName: "existing-document.pdf", fileType: CitationFileType.Pdf);
        var metadata = CreateMetadata(fileName: "existing-document.pdf", fileSize: 1024);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns valid data when file exists
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        _documentApiServiceMock
            .Setup(x => x.GetCitationViewUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://api.example.com/document/existing-document.pdf");

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for loading to complete (either error or content loaded)
        cut.WaitForState(() => !cut.Markup.Contains("Loading document"), TimeSpan.FromSeconds(2));

        // Assert - Should NOT show error message
        cut.FindAll(".citation-error").Should().BeEmpty();
    }

    [Theory]
    [InlineData(CitationFileType.Pdf)]
    [InlineData(CitationFileType.Image)]
    [InlineData(CitationFileType.Text)]
    [InlineData(CitationFileType.Json)]
    [InlineData(CitationFileType.Markdown)]
    [InlineData(CitationFileType.Csv)]
    public async Task ShowsErrorMessage_WhenDocumentIsDeleted_AllFileTypes(CitationFileType fileType)
    {
        // Arrange
        var fileName = fileType switch
        {
            CitationFileType.Pdf => "deleted.pdf",
            CitationFileType.Image => "deleted.png",
            CitationFileType.Text => "deleted.txt",
            CitationFileType.Json => "deleted.json",
            CitationFileType.Markdown => "deleted.md",
            CitationFileType.Csv => "deleted.csv",
            _ => "deleted.file"
        };
        var citation = CreateCitation(fileName: fileName, fileType: fileType);

        _citationStateServiceMock.Setup(x => x.IsViewerOpen).Returns(true);
        _citationStateServiceMock.Setup(x => x.CurrentCitation).Returns(citation);

        // Metadata returns null when file doesn't exist (404)
        _documentApiServiceMock
            .Setup(x => x.GetCitationMetadataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CitationFileMetadata?)null);

        var cut = RenderComponent<CitationViewer>();

        // Act - Trigger the citation changed event on the correct thread
        await cut.InvokeAsync(() => _citationStateServiceMock.Raise(x => x.OnCitationChanged += null));

        // Wait for the error message to appear
        cut.WaitForState(() => cut.Markup.Contains("citation-error"), TimeSpan.FromSeconds(2));

        // Assert - Should show error message for all file types
        cut.Markup.Should().Contain("citation-error");
        cut.Markup.Should().Contain("This document has been removed from the conversation");
    }

    #endregion
}

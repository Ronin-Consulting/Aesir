using Aesir.Client.Web.Modules.Chat.Models;
using Aesir.Client.Web.Modules.Chat.Services;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class CitationStateServiceTests
{
    private readonly Mock<ICitationLinkParser> _mockParser;
    private readonly CitationStateService _sut;

    public CitationStateServiceTests()
    {
        _mockParser = new Mock<ICitationLinkParser>();
        _sut = new CitationStateService(_mockParser.Object);
    }

    #region Initial State Tests

    [Fact]
    public void InitialState_CurrentCitationIsNull()
    {
        // Assert
        _sut.CurrentCitation.Should().BeNull();
    }

    [Fact]
    public void InitialState_IsViewerOpenIsFalse()
    {
        // Assert
        _sut.IsViewerOpen.Should().BeFalse();
    }

    #endregion

    #region OpenCitationAsync Tests

    [Fact]
    public async Task OpenCitationAsync_WithValidCitation_SetsCurren­tCitation()
    {
        // Arrange
        var citation = CreateTestCitation();

        // Act
        await _sut.OpenCitationAsync(citation);

        // Assert
        _sut.CurrentCitation.Should().Be(citation);
        _sut.IsViewerOpen.Should().BeTrue();
    }

    [Fact]
    public async Task OpenCitationAsync_InvokesOnCitationChangedEvent()
    {
        // Arrange
        var citation = CreateTestCitation();
        var eventFired = false;
        _sut.OnCitationChanged += () => eventFired = true;

        // Act
        await _sut.OpenCitationAsync(citation);

        // Assert
        eventFired.Should().BeTrue();
    }

    [Fact]
    public async Task OpenCitationAsync_InvokesOnCitationOpenedEvent()
    {
        // Arrange
        var citation = CreateTestCitation();
        CitationInfo? openedCitation = null;
        _sut.OnCitationOpened += c => openedCitation = c;

        // Act
        await _sut.OpenCitationAsync(citation);

        // Assert
        openedCitation.Should().Be(citation);
    }

    [Fact]
    public async Task OpenCitationAsync_WithNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.OpenCitationAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region OpenCitationByUrlAsync Tests

    [Fact]
    public async Task OpenCitationByUrlAsync_WithValidUrl_ReturnsTrue()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf";
        var citation = CreateTestCitation();
        _mockParser.Setup(p => p.ParseCitationLink(url)).Returns(citation);

        // Act
        var result = await _sut.OpenCitationByUrlAsync(url);

        // Assert
        result.Should().BeTrue();
        _sut.CurrentCitation.Should().Be(citation);
    }

    [Fact]
    public async Task OpenCitationByUrlAsync_WithInvalidUrl_ReturnsFalse()
    {
        // Arrange
        var url = "invalid-url";
        _mockParser.Setup(p => p.ParseCitationLink(url)).Returns((CitationInfo?)null);

        // Act
        var result = await _sut.OpenCitationByUrlAsync(url);

        // Assert
        result.Should().BeFalse();
        _sut.CurrentCitation.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenCitationByUrlAsync_WithEmptyUrl_ReturnsFalse(string? url)
    {
        // Act
        var result = await _sut.OpenCitationByUrlAsync(url!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task OpenCitationByUrlAsync_WithValidUrl_InvokesEvents()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf";
        var citation = CreateTestCitation();
        _mockParser.Setup(p => p.ParseCitationLink(url)).Returns(citation);

        var changedFired = false;
        CitationInfo? openedCitation = null;
        _sut.OnCitationChanged += () => changedFired = true;
        _sut.OnCitationOpened += c => openedCitation = c;

        // Act
        await _sut.OpenCitationByUrlAsync(url);

        // Assert
        changedFired.Should().BeTrue();
        openedCitation.Should().Be(citation);
    }

    #endregion

    #region CloseCitation Tests

    [Fact]
    public async Task CloseCitation_WhenOpen_ClearsCurrentCitation()
    {
        // Arrange
        await _sut.OpenCitationAsync(CreateTestCitation());

        // Act
        _sut.CloseCitation();

        // Assert
        _sut.CurrentCitation.Should().BeNull();
        _sut.IsViewerOpen.Should().BeFalse();
    }

    [Fact]
    public async Task CloseCitation_WhenOpen_InvokesOnCitationClosedEvent()
    {
        // Arrange
        await _sut.OpenCitationAsync(CreateTestCitation());
        var closedFired = false;
        _sut.OnCitationClosed += () => closedFired = true;

        // Act
        _sut.CloseCitation();

        // Assert
        closedFired.Should().BeTrue();
    }

    [Fact]
    public async Task CloseCitation_WhenOpen_InvokesOnCitationChangedEvent()
    {
        // Arrange
        await _sut.OpenCitationAsync(CreateTestCitation());
        var changedFired = false;
        _sut.OnCitationChanged += () => changedFired = true;

        // Act
        _sut.CloseCitation();

        // Assert
        changedFired.Should().BeTrue();
    }

    [Fact]
    public void CloseCitation_WhenAlreadyClosed_DoesNotFireEvents()
    {
        // Arrange
        var changedFired = false;
        var closedFired = false;
        _sut.OnCitationChanged += () => changedFired = true;
        _sut.OnCitationClosed += () => closedFired = true;

        // Act
        _sut.CloseCitation();

        // Assert
        changedFired.Should().BeFalse();
        closedFired.Should().BeFalse();
    }

    #endregion

    #region NavigateToPage Tests

    [Fact]
    public async Task NavigateToPage_WithValidPage_UpdatesPageNumber()
    {
        // Arrange
        var citation = CreateTestCitation(pageNumber: 1);
        await _sut.OpenCitationAsync(citation);

        // Act
        _sut.NavigateToPage(5);

        // Assert
        _sut.CurrentCitation!.PageNumber.Should().Be(5);
    }

    [Fact]
    public async Task NavigateToPage_InvokesOnCitationChangedEvent()
    {
        // Arrange
        await _sut.OpenCitationAsync(CreateTestCitation(pageNumber: 1));
        var changedFired = false;
        _sut.OnCitationChanged += () => changedFired = true;

        // Act
        _sut.NavigateToPage(5);

        // Assert
        changedFired.Should().BeTrue();
    }

    [Fact]
    public void NavigateToPage_WhenNoCitation_DoesNothing()
    {
        // Arrange
        var changedFired = false;
        _sut.OnCitationChanged += () => changedFired = true;

        // Act
        _sut.NavigateToPage(5);

        // Assert
        changedFired.Should().BeFalse();
        _sut.CurrentCitation.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task NavigateToPage_WithInvalidPageNumber_DoesNotUpdate(int invalidPage)
    {
        // Arrange
        var citation = CreateTestCitation(pageNumber: 1);
        await _sut.OpenCitationAsync(citation);
        var changedFired = false;
        _sut.OnCitationChanged += () => changedFired = true;

        // Act
        _sut.NavigateToPage(invalidPage);

        // Assert
        changedFired.Should().BeFalse();
        _sut.CurrentCitation!.PageNumber.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static CitationInfo CreateTestCitation(
        string conversationId = "91c3a876-895d-48bc-80c1-ee917f0026ca",
        string fileName = "test.pdf",
        int? pageNumber = null)
    {
        return new CitationInfo
        {
            ConversationId = conversationId,
            FileName = fileName,
            FileExtension = "pdf",
            PageNumber = pageNumber,
            FileType = CitationFileType.Pdf,
            OriginalUrl = $"file:///{conversationId}/{fileName}"
        };
    }

    #endregion
}

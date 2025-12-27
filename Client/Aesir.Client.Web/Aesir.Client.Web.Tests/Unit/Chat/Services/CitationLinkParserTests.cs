using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Client.Web.Infrastructure.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class CitationLinkParserTests
{
    private readonly CitationLinkParser _sut;

    public CitationLinkParserTests()
    {
        _sut = new CitationLinkParser();
    }

    #region ParseCitationLink Tests

    [Fact]
    public void ParseCitationLink_WithValidPdfUrl_ReturnsCorrectCitation()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf#page=5";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.ConversationId.Should().Be("91c3a876-895d-48bc-80c1-ee917f0026ca");
        result.FileName.Should().Be("report.pdf");
        result.FileExtension.Should().Be("pdf");
        result.PageNumber.Should().Be(5);
        result.FileType.Should().Be(CitationFileType.Pdf);
        result.OriginalUrl.Should().Be(url);
    }

    [Fact]
    public void ParseCitationLink_WithImageUrl_ReturnsImageType()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/diagram.png";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("diagram.png");
        result.FileType.Should().Be(CitationFileType.Image);
        result.PageNumber.Should().BeNull();
    }

    [Fact]
    public void ParseCitationLink_WithEncodedFilename_DecodesCorrectly()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/my%20document%20with%20spaces.pdf";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("my document with spaces.pdf");
    }

    [Theory]
    [InlineData("png", CitationFileType.Image)]
    [InlineData("jpg", CitationFileType.Image)]
    [InlineData("jpeg", CitationFileType.Image)]
    [InlineData("gif", CitationFileType.Image)]
    [InlineData("webp", CitationFileType.Image)]
    [InlineData("bmp", CitationFileType.Image)]
    public void ParseCitationLink_WithImageExtensions_ReturnsImageType(string extension, CitationFileType expectedType)
    {
        // Arrange
        var url = $"file:///91c3a876-895d-48bc-80c1-ee917f0026ca/image.{extension}";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("tiff", CitationFileType.Tiff)]
    [InlineData("tif", CitationFileType.Tiff)]
    public void ParseCitationLink_WithTiffExtensions_ReturnsTiffType(string extension, CitationFileType expectedType)
    {
        // Arrange
        var url = $"file:///91c3a876-895d-48bc-80c1-ee917f0026ca/scan.{extension}";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("txt", CitationFileType.Text)]
    [InlineData("log", CitationFileType.Text)]
    public void ParseCitationLink_WithTextExtensions_ReturnsTextType(string extension, CitationFileType expectedType)
    {
        // Arrange
        var url = $"file:///91c3a876-895d-48bc-80c1-ee917f0026ca/file.{extension}";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("json", CitationFileType.Json)]
    [InlineData("xml", CitationFileType.Xml)]
    [InlineData("csv", CitationFileType.Csv)]
    public void ParseCitationLink_WithStructuredDataExtensions_ReturnsCorrectType(string extension, CitationFileType expectedType)
    {
        // Arrange
        var url = $"file:///91c3a876-895d-48bc-80c1-ee917f0026ca/data.{extension}";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("md", CitationFileType.Markdown)]
    [InlineData("markdown", CitationFileType.Markdown)]
    [InlineData("html", CitationFileType.Html)]
    [InlineData("htm", CitationFileType.Html)]
    public void ParseCitationLink_WithMarkupExtensions_ReturnsCorrectType(string extension, CitationFileType expectedType)
    {
        // Arrange
        var url = $"file:///91c3a876-895d-48bc-80c1-ee917f0026ca/document.{extension}";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(expectedType);
    }

    [Fact]
    public void ParseCitationLink_WithUnknownExtension_ReturnsUnknownType()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/file.xyz";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileType.Should().Be(CitationFileType.Unknown);
    }

    [Fact]
    public void ParseCitationLink_WithNullUrl_ReturnsNull()
    {
        // Act
        var result = _sut.ParseCitationLink(null!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseCitationLink_WithEmptyOrWhitespaceUrl_ReturnsNull(string url)
    {
        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("https://example.com/file.pdf")]
    [InlineData("http://localhost/file.pdf")]
    [InlineData("invalid-url")]
    [InlineData("file:///invalid-guid/file.pdf")]
    public void ParseCitationLink_WithInvalidUrl_ReturnsNull(string url)
    {
        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseCitationLink_WithUppercaseGuid_ParsesCorrectly()
    {
        // Arrange
        var url = "file:///91C3A876-895D-48BC-80C1-EE917F0026CA/report.pdf";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.ConversationId.Should().Be("91C3A876-895D-48BC-80C1-EE917F0026CA");
    }

    [Fact]
    public void ParseCitationLink_WithSubdirectoryPath_ParsesFullPath()
    {
        // Arrange
        var url = "file:///91c3a876-895d-48bc-80c1-ee917f0026ca/folder/subfolder/report.pdf";

        // Act
        var result = _sut.ParseCitationLink(url);

        // Assert
        result.Should().NotBeNull();
        result!.FileName.Should().Be("folder/subfolder/report.pdf");
    }

    #endregion

    #region IsCitationLink Tests

    [Theory]
    [InlineData("file:///91c3a876-895d-48bc-80c1-ee917f0026ca/report.pdf", true)]
    [InlineData("file:///91c3a876-895d-48bc-80c1-ee917f0026ca/image.png#page=1", true)]
    [InlineData("https://example.com/file.pdf", false)]
    [InlineData("http://localhost/file.pdf", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("file:///invalid-guid/file.pdf", false)]
    public void IsCitationLink_ReturnsExpectedResult(string? url, bool expected)
    {
        // Act
        var result = _sut.IsCitationLink(url);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetFileType Tests

    [Theory]
    [InlineData("pdf", CitationFileType.Pdf)]
    [InlineData("PDF", CitationFileType.Pdf)]
    [InlineData(".pdf", CitationFileType.Pdf)]
    [InlineData("png", CitationFileType.Image)]
    [InlineData("jpg", CitationFileType.Image)]
    [InlineData("json", CitationFileType.Json)]
    [InlineData("unknown", CitationFileType.Unknown)]
    [InlineData("", CitationFileType.Unknown)]
    [InlineData(null, CitationFileType.Unknown)]
    public void GetFileType_ReturnsExpectedType(string? extension, CitationFileType expected)
    {
        // Act
        var result = _sut.GetFileType(extension!);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}

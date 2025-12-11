using System.Net;
using System.Net.Http.Json;
using Aesir.Client.Web.Modules.Chat.Models;
using Aesir.Client.Web.Modules.Chat.Services;
using Microsoft.AspNetCore.Components.Forms;
using RichardSzalay.MockHttp;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class DocumentApiServiceTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly DocumentApiService _sut;
    private readonly Guid _conversationId = Guid.NewGuid();

    public DocumentApiServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_mockHttp)
        {
            BaseAddress = new Uri("https://api.test.com")
        };
        _sut = new DocumentApiService(httpClient);
    }

    #region IsFileTypeSupported Tests

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("document.PDF", true)]
    [InlineData("readme.txt", true)]
    [InlineData("readme.md", true)]
    [InlineData("data.json", true)]
    [InlineData("data.xml", true)]
    [InlineData("data.csv", true)]
    [InlineData("image.png", true)]
    [InlineData("image.jpg", true)]
    [InlineData("image.jpeg", true)]
    [InlineData("image.gif", true)]
    [InlineData("image.webp", true)]
    [InlineData("program.exe", false)]
    [InlineData("archive.zip", false)]
    [InlineData("script.js", false)]
    [InlineData("style.css", false)]
    [InlineData("", false)]
    public void IsFileTypeSupported_ReturnsExpectedResult(string filename, bool expected)
    {
        // Act
        var result = _sut.IsFileTypeSupported(filename);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsFileTypeSupported_ReturnsFalse_ForNullFilename()
    {
        // Act
        var result = _sut.IsFileTypeSupported(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsFileSizeValid Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(1024, true)]
    [InlineData(1024 * 1024, true)]
    [InlineData(50 * 1024 * 1024, true)]
    [InlineData(100 * 1024 * 1024, true)] // Exactly at limit
    [InlineData(100 * 1024 * 1024 + 1, false)] // Over limit
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsFileSizeValid_ReturnsExpectedResult(long fileSize, bool expected)
    {
        // Act
        var result = _sut.IsFileSizeValid(fileSize);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetDownloadUrl Tests

    [Fact]
    public void GetDownloadUrl_ReturnsCorrectUrl()
    {
        // Arrange
        var filename = "document.pdf";

        // Act
        var result = _sut.GetDownloadUrl(_conversationId, filename);

        // Assert
        result.Should().Be($"https://api.test.com/document/collections/conversations/{_conversationId}/files/document.pdf/content");
    }

    [Fact]
    public void GetDownloadUrl_EncodesSpecialCharacters()
    {
        // Arrange
        var filename = "my document (1).pdf";

        // Act
        var result = _sut.GetDownloadUrl(_conversationId, filename);

        // Assert
        result.Should().Contain(Uri.EscapeDataString(filename));
    }

    #endregion

    #region GetFilesAsync Tests

    [Fact]
    public async Task GetFilesAsync_ReturnsFiles_OnSuccess()
    {
        // Arrange
        var files = new List<ConversationFile>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FileName = $"/{_conversationId}/document1.pdf",
                MimeType = "application/pdf",
                FileSize = 1024
            },
            new()
            {
                Id = Guid.NewGuid(),
                FileName = $"/{_conversationId}/image.png",
                MimeType = "image/png",
                FileSize = 2048
            }
        };

        _mockHttp.When($"/document/collections/conversations/{_conversationId}/files")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(files));

        // Act
        var result = await _sut.GetFilesAsync(_conversationId);

        // Assert
        result.Should().HaveCount(2);
        result[0].DisplayName.Should().Be("document1.pdf");
        result[1].DisplayName.Should().Be("image.png");
    }

    [Fact]
    public async Task GetFilesAsync_ReturnsEmptyList_WhenNoFiles()
    {
        // Arrange
        _mockHttp.When($"/document/collections/conversations/{_conversationId}/files")
            .Respond("application/json", "[]");

        // Act
        var result = await _sut.GetFilesAsync(_conversationId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilesAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        _mockHttp.When($"/document/collections/conversations/{_conversationId}/files")
            .Respond(HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _sut.GetFilesAsync(_conversationId));
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var filename = "document.pdf";
        _mockHttp.When(HttpMethod.Delete, $"/document/collections/conversations/{_conversationId}/files/{filename}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _sut.DeleteFileAsync(_conversationId, filename);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileAsync_ReturnsFalse_OnNotFound()
    {
        // Arrange
        var filename = "nonexistent.pdf";
        _mockHttp.When(HttpMethod.Delete, $"/document/collections/conversations/{_conversationId}/files/{filename}")
            .Respond(HttpStatusCode.NotFound);

        // Act
        var result = await _sut.DeleteFileAsync(_conversationId, filename);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_EncodesFilename_WithSpecialCharacters()
    {
        // Arrange
        var filename = "my document (1).pdf";
        var encodedFilename = Uri.EscapeDataString(filename);
        _mockHttp.When(HttpMethod.Delete, $"/document/collections/conversations/{_conversationId}/files/{encodedFilename}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _sut.DeleteFileAsync(_conversationId, filename);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region MoveFilesAsync Tests

    [Fact]
    public async Task MoveFilesAsync_ReturnsTrue_OnSuccess()
    {
        // Arrange
        var targetConversationId = Guid.NewGuid();
        _mockHttp.When(HttpMethod.Post, $"/document/collections/conversations/{_conversationId}/move/{targetConversationId}")
            .Respond(HttpStatusCode.OK);

        // Act
        var result = await _sut.MoveFilesAsync(_conversationId, targetConversationId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MoveFilesAsync_ReturnsFalse_OnError()
    {
        // Arrange
        var targetConversationId = Guid.NewGuid();
        _mockHttp.When(HttpMethod.Post, $"/document/collections/conversations/{_conversationId}/move/{targetConversationId}")
            .Respond(HttpStatusCode.InternalServerError);

        // Act
        var result = await _sut.MoveFilesAsync(_conversationId, targetConversationId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region UploadFileAsync Tests

    [Fact]
    public async Task UploadFileAsync_ThrowsException_ForUnsupportedFileType()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("program.exe");
        mockFile.Setup(f => f.Size).Returns(1024);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadFileAsync(_conversationId, mockFile.Object));
        exception.Message.Should().Contain("File type not supported");
    }

    [Fact]
    public async Task UploadFileAsync_ThrowsException_ForOversizedFile()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("document.pdf");
        mockFile.Setup(f => f.Size).Returns(IDocumentApiService.MaxFileSize + 1);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UploadFileAsync(_conversationId, mockFile.Object));
        exception.Message.Should().Contain("File size exceeds");
    }

    [Fact]
    public async Task UploadFileAsync_ReportsProgress()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("document.pdf");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[1024]));

        var response = new FileUploadResponse
        {
            Message = "File uploaded successfully",
            FileName = "document.pdf",
            ConversationId = _conversationId.ToString()
        };

        _mockHttp.When(HttpMethod.Post, $"/document/collections/conversations/{_conversationId}/upload/file")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(response));

        var progressValues = new List<int>();
        var progress = new Progress<int>(p => progressValues.Add(p));

        // Act
        await _sut.UploadFileAsync(_conversationId, mockFile.Object, progress);

        // Assert - Give time for progress events to fire
        await Task.Delay(50);
        progressValues.Should().Contain(0);
        progressValues.Should().Contain(100);
    }

    [Fact]
    public async Task UploadFileAsync_ReturnsResponse_OnSuccess()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("document.pdf");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[1024]));

        var expectedResponse = new FileUploadResponse
        {
            Message = "File uploaded successfully",
            FileName = "document.pdf",
            ConversationId = _conversationId.ToString()
        };

        _mockHttp.When(HttpMethod.Post, $"/document/collections/conversations/{_conversationId}/upload/file")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(expectedResponse));

        // Act
        var result = await _sut.UploadFileAsync(_conversationId, mockFile.Object);

        // Assert
        result.FileName.Should().Be("document.pdf");
        result.Message.Should().Be("File uploaded successfully");
    }

    [Fact]
    public async Task UploadFileAsync_ThrowsException_OnHttpError()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("document.pdf");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[1024]));

        _mockHttp.When(HttpMethod.Post, $"/document/collections/conversations/{_conversationId}/upload/file")
            .Respond(HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.UploadFileAsync(_conversationId, mockFile.Object));
    }

    #endregion
}

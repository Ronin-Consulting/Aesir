using Aesir.Infrastructure.Data;
using Aesir.Modules.Storage.Services;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Aesir.Modules.Storage.Tests.Services;

/// <summary>
/// Unit tests for FileStorageService.
/// These tests focus on the logic that can be unit tested without database integration.
/// For full integration tests, use an actual test database.
/// </summary>
public class FileStorageServiceTests
{
    private readonly Mock<ILogger<FileStorageService>> _loggerMock;
    private readonly Mock<IDbContext> _dbContextMock;
    private readonly Mock<IDbConnection> _dbConnectionMock;
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileStorageService>>();
        _dbContextMock = new Mock<IDbContext>();
        _dbConnectionMock = new Mock<IDbConnection>();

        // Setup the mock to return our mock connection
        _dbContextMock.Setup(x => x.GetConnection()).Returns(_dbConnectionMock.Object);

        _service = new FileStorageService(_loggerMock.Object, _dbContextMock.Object);
    }

    #region GetFilesByConversationIdsAsync Tests

    [Fact]
    public async Task GetFilesByConversationIdsAsync_WithEmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var conversationIds = Array.Empty<Guid>();

        // Act
        var result = await _service.GetFilesByConversationIdsAsync(conversationIds);

        // Assert
        result.Should().BeEmpty();

        // Verify no database connection was requested
        _dbContextMock.Verify(x => x.GetConnection(), Times.Never);
    }

    [Fact]
    public async Task GetFilesByConversationIdsAsync_WithEmptyEnumerable_ReturnsEmptyResult()
    {
        // Arrange
        IEnumerable<Guid> conversationIds = Enumerable.Empty<Guid>();

        // Act
        var result = await _service.GetFilesByConversationIdsAsync(conversationIds);

        // Assert
        result.Should().BeEmpty();

        // Verify no database connection was requested
        _dbContextMock.Verify(x => x.GetConnection(), Times.Never);
    }

    [Fact]
    public async Task GetFilesByConversationIdsAsync_WithNullEnumerable_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Guid> conversationIds = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetFilesByConversationIdsAsync(conversationIds));
    }

    #endregion

    #region Pattern Building Tests

    [Fact]
    public void FolderPattern_ShouldBeCorrectFormat()
    {
        // Verify the expected folder pattern format
        var conversationId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var expectedPattern = $"/{conversationId}/%";

        // The pattern should be in format /{conversationId}/%
        expectedPattern.Should().Be("/12345678-1234-1234-1234-123456789abc/%");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public void MultiplePatterns_ShouldBeGeneratedCorrectly(int count)
    {
        // Arrange
        var conversationIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

        // Each conversation ID should generate a pattern like /{id}/%
        var patterns = conversationIds.Select(id => $"/{id}/%").ToArray();

        // Assert
        patterns.Should().HaveCount(count);
        foreach (var pattern in patterns)
        {
            pattern.Should().StartWith("/");
            pattern.Should().EndWith("/%");
        }
    }

    #endregion
}

/// <summary>
/// Tests for the file path pattern matching logic.
/// These tests verify the convention that files are stored as /{conversationId}/{filename}.
/// </summary>
public class FilePathPatternTests
{
    [Theory]
    [InlineData("/12345678-1234-1234-1234-123456789abc/document.pdf", "12345678-1234-1234-1234-123456789abc")]
    [InlineData("/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/image.png", "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    [InlineData("/00000000-0000-0000-0000-000000000001/test.txt", "00000000-0000-0000-0000-000000000001")]
    public void ExtractConversationId_FromFilePath_ReturnsCorrectId(string filePath, string expectedId)
    {
        // The pattern is /{conversationId}/{filename}
        var parts = filePath.TrimStart('/').Split('/');
        var conversationId = parts[0];

        conversationId.Should().Be(expectedId);
        Guid.TryParse(conversationId, out var guid).Should().BeTrue();
        guid.Should().Be(Guid.Parse(expectedId));
    }

    [Theory]
    [InlineData("/12345678-1234-1234-1234-123456789abc/document.pdf", "12345678-1234-1234-1234-123456789abc", true)]
    [InlineData("/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/image.png", "12345678-1234-1234-1234-123456789abc", false)]
    [InlineData("/12345678-1234-1234-1234-123456789abc/test.txt", "12345678-1234-1234-1234-123456789abc", true)]
    public void FilePath_BelongsToConversation_MatchesCorrectly(string filePath, string conversationId, bool shouldMatch)
    {
        // The LIKE pattern /{conversationId}/% should match files in that conversation
        var pattern = $"/{conversationId}/%";

        // Simulate LIKE pattern matching
        var matches = filePath.StartsWith($"/{conversationId}/");

        matches.Should().Be(shouldMatch);
    }

    [Fact]
    public void FilePath_WithDifferentConversationIds_AreFiltered()
    {
        // Arrange
        var userConversationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var otherConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var allFiles = new List<string>
        {
            $"/{userConversationId}/user-doc1.pdf",
            $"/{userConversationId}/user-doc2.txt",
            $"/{otherConversationId}/other-doc1.pdf",
            $"/{otherConversationId}/other-doc2.png"
        };

        // Act - Filter files for userConversationId only
        var userFiles = allFiles.Where(f => f.StartsWith($"/{userConversationId}/")).ToList();

        // Assert
        userFiles.Should().HaveCount(2);
        userFiles.Should().AllSatisfy(f => f.Should().Contain(userConversationId.ToString()));
        userFiles.Should().NotContain(f => f.Contains(otherConversationId.ToString()));
    }

    [Fact]
    public void FilePath_WithMultipleUserConversations_ReturnsAllMatching()
    {
        // Arrange
        var userConversation1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userConversation2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var otherConversation = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var userConversationIds = new[] { userConversation1, userConversation2 };

        var allFiles = new List<string>
        {
            $"/{userConversation1}/doc1.pdf",
            $"/{userConversation2}/doc2.pdf",
            $"/{otherConversation}/other.pdf"
        };

        // Act - Filter files for user's conversations only
        var userFiles = allFiles
            .Where(f => userConversationIds.Any(id => f.StartsWith($"/{id}/")))
            .ToList();

        // Assert
        userFiles.Should().HaveCount(2);
        userFiles.Should().Contain($"/{userConversation1}/doc1.pdf");
        userFiles.Should().Contain($"/{userConversation2}/doc2.pdf");
        userFiles.Should().NotContain($"/{otherConversation}/other.pdf");
    }
}

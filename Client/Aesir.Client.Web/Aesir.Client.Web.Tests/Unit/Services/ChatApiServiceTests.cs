using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Models;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Services;

public class ChatApiServiceTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly ChatApiService _service;

    public ChatApiServiceTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _service = new ChatApiService(_mockApiClient.Object);
    }

    [Fact]
    public async Task GetChatSessionsAsync_ReturnsSessions()
    {
        // Arrange
        const string userId = "test@example.com";
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Session 1", UserId = userId },
            new() { Id = Guid.NewGuid(), Title = "Session 2", UserId = userId }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirChatSessionItem>>(
                $"/chat/history/user/{Uri.EscapeDataString(userId)}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.GetChatSessionsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetChatSessionAsync_ReturnsSession_WhenFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AesirChatSession
        {
            Id = sessionId,
            Title = "Test Session",
            UserId = "test@example.com",
            Conversation = new AesirConversation()
        };
        _mockApiClient.Setup(x => x.GetAsync<AesirChatSession>(
                $"/chat/history/{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        var result = await _service.GetChatSessionAsync(sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Title.Should().Be("Test Session");
    }

    [Fact]
    public async Task GetChatSessionAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockApiClient.Setup(x => x.GetAsync<AesirChatSession>(
                $"/chat/history/{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AesirChatSession?)null);

        // Act
        var result = await _service.GetChatSessionAsync(sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task UpdateChatSessionTitleAsync_ReturnsSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        const string newTitle = "New Title";
        _mockApiClient.Setup(x => x.PutAsync<object>(
                $"/chat/history/{sessionId}/{Uri.EscapeDataString(newTitle)}",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new object());

        // Act
        var result = await _service.UpdateChatSessionTitleAsync(sessionId, newTitle);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteChatSessionAsync_ReturnsSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockApiClient.Setup(x => x.DeleteAsync(
                $"/chat/history/{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteChatSessionAsync(sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteChatSessionAsync_ReturnsFailure_WhenDeleteFails()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockApiClient.Setup(x => x.DeleteAsync(
                $"/chat/history/{sessionId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteChatSessionAsync(sessionId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to delete");
    }

    [Fact]
    public async Task SearchChatSessionsAsync_ReturnsMatchingSessions()
    {
        // Arrange
        const string userId = "test@example.com";
        const string searchTerm = "important";
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Important meeting", UserId = userId }
        };
        _mockApiClient.Setup(x => x.GetAsync<List<AesirChatSessionItem>>(
                $"/chat/history/user/{Uri.EscapeDataString(userId)}/search/{Uri.EscapeDataString(searchTerm)}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        // Act
        var result = await _service.SearchChatSessionsAsync(userId, searchTerm);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Title.Should().Contain("Important");
    }

    [Fact]
    public async Task StreamChatAsync_YieldsStreamedResults()
    {
        // Arrange
        var request = new TestChatRequest { AgentId = Guid.NewGuid() };
        var streamedResults = new List<AesirChatStreamedResult>
        {
            new() { Delta = new AesirChatMessage { Role = "assistant", Content = "Hello " } },
            new() { Delta = new AesirChatMessage { Role = "assistant", Content = "World!" } }
        };

        _mockApiClient.Setup(x => x.StreamPostAsync<AesirChatStreamedResult>(
                "/chat/completions/agent/streamed",
                It.IsAny<AesirAgentChatRequestBase>(),
                It.IsAny<CancellationToken>()))
            .Returns(streamedResults.ToAsyncEnumerable());

        // Act
        var results = new List<AesirChatStreamedResult>();
        await foreach (var result in _service.StreamChatAsync(request))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Delta!.Content.Should().Be("Hello ");
        results[1].Delta!.Content.Should().Be("World!");
    }

    [Fact]
    public async Task GetChatSessionsAsync_ReturnsEmptyList_WhenNoSessions()
    {
        // Arrange
        const string userId = "new@example.com";
        _mockApiClient.Setup(x => x.GetAsync<List<AesirChatSessionItem>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<AesirChatSessionItem>?)null);

        // Act
        var result = await _service.GetChatSessionsAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChatSessionsAsync_ReturnsFailure_OnHttpError()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetAsync<List<AesirChatSessionItem>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _service.GetChatSessionsAsync("test@example.com");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Connection refused");
    }

    // Test helper classes
    private class TestChatRequest : AesirAgentChatRequestBase;
}

// Extension method to convert List to IAsyncEnumerable for testing
public static class TestExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}

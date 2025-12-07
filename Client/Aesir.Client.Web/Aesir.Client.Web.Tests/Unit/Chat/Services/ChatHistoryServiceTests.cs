using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Chat.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Chat.Services;

public class ChatHistoryServiceTests
{
    private readonly Mock<IChatApiService> _mockChatApiService;
    private readonly ChatHistoryService _sut;

    public ChatHistoryServiceTests()
    {
        _mockChatApiService = new Mock<IChatApiService>();
        _sut = new ChatHistoryService(_mockChatApiService.Object);
    }

    [Fact]
    public void Sessions_ReturnsEmptyList_Initially()
    {
        // Assert
        _sut.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void IsLoading_ReturnsFalse_Initially()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void SearchTerm_ReturnsNull_Initially()
    {
        // Assert
        _sut.SearchTerm.Should().BeNull();
    }

    [Fact]
    public async Task LoadSessionsAsync_SetsIsLoadingTrue_DuringLoad()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        var tcs = new TaskCompletionSource<ApiResult<IReadOnlyList<AesirChatSessionItem>>>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        bool wasLoading = false;
        _sut.OnSessionsChanged += () => wasLoading = wasLoading || _sut.IsLoading;

        // Act
        var loadTask = _sut.LoadSessionsAsync();

        // Assert - While loading
        wasLoading.Should().BeTrue();

        // Complete the task
        tcs.SetResult(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        await loadTask;

        // Assert - After loading
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSessionsAsync_PopulatesSessions_OnSuccess()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Session 1", UpdatedAt = DateTimeOffset.Now },
            new() { Id = Guid.NewGuid(), Title = "Session 2", UpdatedAt = DateTimeOffset.Now.AddHours(-1) }
        };
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        // Act
        await _sut.LoadSessionsAsync();

        // Assert
        _sut.Sessions.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadSessionsAsync_OrdersSessionsByUpdatedAt_Descending()
    {
        // Arrange
        var older = DateTimeOffset.Now.AddHours(-2);
        var newer = DateTimeOffset.Now;
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Old", UpdatedAt = older },
            new() { Id = Guid.NewGuid(), Title = "New", UpdatedAt = newer }
        };
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        // Act
        await _sut.LoadSessionsAsync();

        // Assert
        _sut.Sessions.First().Title.Should().Be("New");
    }

    [Fact]
    public async Task LoadSessionsAsync_ClearsSearchTerm()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        _mockChatApiService.Setup(x => x.SearchChatSessionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        await _sut.SearchSessionsAsync("test");

        // Act
        await _sut.LoadSessionsAsync();

        // Assert
        _sut.SearchTerm.Should().BeNull();
    }

    [Fact]
    public async Task LoadSessionsAsync_RaisesOnSessionsChanged()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        var eventRaised = false;
        _sut.OnSessionsChanged += () => eventRaised = true;

        // Act
        await _sut.LoadSessionsAsync();

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsSession_FromApi()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = new AesirChatSession
        {
            Id = sessionId,
            Title = "Test Session",
            Conversation = new AesirConversation()
        };
        _mockChatApiService.Setup(x => x.GetChatSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<AesirChatSession?>.Success(session));

        // Act
        var result = await _sut.GetSessionAsync(sessionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Session");
    }

    [Fact]
    public async Task DeleteSessionAsync_RemovesFromCache_OnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = sessionId, Title = "To Delete" },
            new() { Id = Guid.NewGuid(), Title = "Keep" }
        };
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        _mockChatApiService.Setup(x => x.DeleteChatSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        await _sut.LoadSessionsAsync();
        _sut.Sessions.Should().HaveCount(2);

        // Act
        await _sut.DeleteSessionAsync(sessionId);

        // Assert
        _sut.Sessions.Should().HaveCount(1);
        _sut.Sessions.Should().NotContain(s => s.Id == sessionId);
    }

    [Fact]
    public async Task DeleteSessionAsync_RaisesOnSessionsChanged_OnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessions = new List<AesirChatSessionItem> { new() { Id = sessionId, Title = "Test" } };
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        _mockChatApiService.Setup(x => x.DeleteChatSessionAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        await _sut.LoadSessionsAsync();
        var eventCount = 0;
        _sut.OnSessionsChanged += () => eventCount++;

        // Act
        await _sut.DeleteSessionAsync(sessionId);

        // Assert
        eventCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateSessionTitleAsync_UpdatesCache_OnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessions = new List<AesirChatSessionItem>
        {
            new() { Id = sessionId, Title = "Old Title" }
        };
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        _mockChatApiService.Setup(x => x.UpdateChatSessionTitleAsync(sessionId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.Success());

        await _sut.LoadSessionsAsync();

        // Act
        await _sut.UpdateSessionTitleAsync(sessionId, "New Title");

        // Assert
        _sut.Sessions.First(s => s.Id == sessionId).Title.Should().Be("New Title");
    }

    [Fact]
    public async Task SearchSessionsAsync_SetsSearchTerm()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.SearchChatSessionsAsync(It.IsAny<string>(), "test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        // Act
        await _sut.SearchSessionsAsync("test");

        // Assert
        _sut.SearchTerm.Should().Be("test");
    }

    [Fact]
    public async Task SearchSessionsAsync_CallsLoadSessionsAsync_WhenSearchTermEmpty()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        // Act
        await _sut.SearchSessionsAsync("");

        // Assert
        _mockChatApiService.Verify(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockChatApiService.Verify(x => x.SearchChatSessionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearSearchAsync_ClearsSearchTermAndReloads()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));
        _mockChatApiService.Setup(x => x.SearchChatSessionsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        await _sut.SearchSessionsAsync("test");
        _sut.SearchTerm.Should().Be("test");

        // Act
        await _sut.ClearSearchAsync();

        // Assert
        _sut.SearchTerm.Should().BeNull();
    }

    [Fact]
    public void SelectSession_RaisesOnSessionSelected()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        Guid? selectedId = null;
        _sut.OnSessionSelected += id => selectedId = id;

        // Act
        _sut.SelectSession(sessionId);

        // Assert
        selectedId.Should().Be(sessionId);
    }

    [Fact]
    public async Task NotifySessionCreatedAsync_RefreshesSessions()
    {
        // Arrange
        var sessions = new List<AesirChatSessionItem>();
        _mockChatApiService.Setup(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<IReadOnlyList<AesirChatSessionItem>>.Success(sessions));

        // Act
        await _sut.NotifySessionCreatedAsync(Guid.NewGuid());

        // Assert
        _mockChatApiService.Verify(x => x.GetChatSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

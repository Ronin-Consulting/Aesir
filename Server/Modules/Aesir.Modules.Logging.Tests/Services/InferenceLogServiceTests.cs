using Aesir.Common.Models;
using Aesir.Infrastructure.Data;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Tests.Services;

public class InferenceLogServiceTests
{
    private readonly Mock<IDbContext> _dbContextMock;
    private readonly Mock<ILogger<InferenceLogService>> _loggerMock;
    private readonly InferenceLogService _service;

    public InferenceLogServiceTests()
    {
        _dbContextMock = new Mock<IDbContext>();
        _loggerMock = new Mock<ILogger<InferenceLogService>>();
        _service = new InferenceLogService(_dbContextMock.Object, _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InferenceLogService(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dbContext");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InferenceLogService(_dbContextMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Helper Methods

    private static AesirInferenceLog CreateTestInferenceLog(
        Guid? id = null,
        Guid? chatSessionId = null,
        InferenceStatus status = InferenceStatus.Completed,
        int toolCallCount = 0)
    {
        return new AesirInferenceLog
        {
            Id = id ?? Guid.NewGuid(),
            ChatSessionId = chatSessionId ?? Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            UserQuery = "Test user query",
            UserQueryTruncated = "Test user query",
            AssistantResponse = "Test assistant response",
            ToolCalls = CreateTestToolCalls(toolCallCount),
            ToolCallCount = toolCallCount,
            TotalDurationMs = 1000,
            StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAt = DateTimeOffset.UtcNow,
            Status = status,
            ErrorMessage = status == InferenceStatus.Failed ? "Test error" : null
        };
    }

    private static List<AesirToolCallInfo> CreateTestToolCalls(int count)
    {
        var toolCalls = new List<AesirToolCallInfo>();
        for (var i = 0; i < count; i++)
        {
            toolCalls.Add(new AesirToolCallInfo
            {
                ToolCallId = $"tool-{i}",
                FunctionName = $"TestFunction{i}",
                PluginName = "TestPlugin",
                ToolType = ToolCallType.DocumentSearch,
                Status = ToolCallStatus.Completed,
                StartedAt = DateTimeOffset.UtcNow.AddSeconds(-10),
                CompletedAt = DateTimeOffset.UtcNow,
                Result = "Test result"
            });
        }
        return toolCalls;
    }

    private static InferenceLogFilterRequest CreateDefaultFilter()
    {
        return new InferenceLogFilterRequest
        {
            Page = 1,
            PageSize = 50,
            SortDirection = SortDirection.Descending
        };
    }

    #endregion
}

public class InferenceLogFilterRequestTests
{
    [Fact]
    public void Page_DefaultValue_IsOne()
    {
        // Arrange & Act
        var filter = new InferenceLogFilterRequest();

        // Assert
        filter.Page.Should().Be(1);
    }

    [Fact]
    public void PageSize_DefaultValue_IsFifty()
    {
        // Arrange & Act
        var filter = new InferenceLogFilterRequest();

        // Assert
        filter.PageSize.Should().Be(50);
    }

    [Fact]
    public void SortDirection_DefaultValue_IsDescending()
    {
        // Arrange & Act
        var filter = new InferenceLogFilterRequest();

        // Assert
        filter.SortDirection.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void AllFilters_CanBeSet()
    {
        // Arrange
        var chatSessionId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        // Act
        var filter = new InferenceLogFilterRequest
        {
            Page = 2,
            PageSize = 25,
            ChatSessionId = chatSessionId,
            ConversationId = conversationId,
            From = from,
            To = to,
            Statuses = new List<InferenceStatus> { InferenceStatus.Completed, InferenceStatus.Failed },
            MinToolCallCount = 1,
            HasToolCalls = true,
            SearchText = "test query",
            SortDirection = SortDirection.Ascending
        };

        // Assert
        filter.Page.Should().Be(2);
        filter.PageSize.Should().Be(25);
        filter.ChatSessionId.Should().Be(chatSessionId);
        filter.ConversationId.Should().Be(conversationId);
        filter.From.Should().Be(from);
        filter.To.Should().Be(to);
        filter.Statuses.Should().HaveCount(2);
        filter.Statuses.Should().Contain(InferenceStatus.Completed);
        filter.Statuses.Should().Contain(InferenceStatus.Failed);
        filter.MinToolCallCount.Should().Be(1);
        filter.HasToolCalls.Should().BeTrue();
        filter.SearchText.Should().Be("test query");
        filter.SortDirection.Should().Be(SortDirection.Ascending);
    }
}

public class PagedInferenceLogResponseTests
{
    [Fact]
    public void TotalPages_WithZeroPageSize_ReturnsZero()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            TotalCount = 100,
            PageSize = 0
        };

        // Act & Assert
        response.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WithPositivePageSize_ReturnsCorrectValue()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            TotalCount = 100,
            PageSize = 25
        };

        // Act & Assert
        response.TotalPages.Should().Be(4);
    }

    [Fact]
    public void TotalPages_WithPartialLastPage_RoundsUp()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            TotalCount = 101,
            PageSize = 25
        };

        // Act & Assert
        response.TotalPages.Should().Be(5);
    }

    [Fact]
    public void HasNextPage_OnFirstPageWithMorePages_ReturnsTrue()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            Page = 1,
            PageSize = 25,
            TotalCount = 100
        };

        // Act & Assert
        response.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_OnLastPage_ReturnsFalse()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            Page = 4,
            PageSize = 25,
            TotalCount = 100
        };

        // Act & Assert
        response.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnFirstPage_ReturnsFalse()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            Page = 1,
            PageSize = 25,
            TotalCount = 100
        };

        // Act & Assert
        response.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_OnSecondPage_ReturnsTrue()
    {
        // Arrange
        var response = new PagedInferenceLogResponse
        {
            Page = 2,
            PageSize = 25,
            TotalCount = 100
        };

        // Act & Assert
        response.HasPreviousPage.Should().BeTrue();
    }
}

public class AesirInferenceLogTests
{
    [Fact]
    public void ToolCalls_DefaultValue_IsEmptyList()
    {
        // Arrange & Act
        var log = new AesirInferenceLog();

        // Assert
        log.ToolCalls.Should().NotBeNull();
        log.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void Status_DefaultValue_IsInProgress()
    {
        // Arrange & Act
        var log = new AesirInferenceLog();

        // Assert
        log.Status.Should().Be(InferenceStatus.InProgress);
    }
}

public class AesirToolCallInfoTests
{
    [Fact]
    public void DurationMs_WithBothTimestamps_CalculatesCorrectly()
    {
        // Arrange
        var startedAt = DateTimeOffset.UtcNow.AddMilliseconds(-500);
        var completedAt = DateTimeOffset.UtcNow;
        var toolCall = new AesirToolCallInfo
        {
            ToolCallId = "test",
            FunctionName = "Test",
            StartedAt = startedAt,
            CompletedAt = completedAt
        };

        // Act & Assert
        toolCall.DurationMs.Should().BeInRange(490, 510);
    }

    [Fact]
    public void DurationMs_WithNoCompletedAt_ReturnsNull()
    {
        // Arrange
        var toolCall = new AesirToolCallInfo
        {
            ToolCallId = "test",
            FunctionName = "Test",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = null
        };

        // Act & Assert
        toolCall.DurationMs.Should().BeNull();
    }
}

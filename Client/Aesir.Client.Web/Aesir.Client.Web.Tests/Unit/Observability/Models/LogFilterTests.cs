using Aesir.Client.Web.Modules.Observability.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Observability.Models;

public class LogFilterTests
{
    #region Default Values Tests

    [Fact]
    public void LogFilter_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var filter = new LogFilter();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.ChatSessionId.Should().BeNull();
        filter.ConversationId.Should().BeNull();
        filter.Statuses.Should().BeEmpty();
        filter.MinToolCallCount.Should().BeNull();
        filter.HasToolCalls.Should().BeNull();
        filter.SearchText.Should().BeNull();
        filter.SortAscending.Should().BeFalse();
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new LogFilter
        {
            Page = 5,
            PageSize = 100,
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now,
            ChatSessionId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Statuses = [InferenceStatus.Completed, InferenceStatus.Failed],
            MinToolCallCount = 2,
            HasToolCalls = true,
            SearchText = "test query"
        };

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Page.Should().Be(original.Page);
        clone.PageSize.Should().Be(original.PageSize);
        clone.From.Should().Be(original.From);
        clone.To.Should().Be(original.To);
        clone.ChatSessionId.Should().Be(original.ChatSessionId);
        clone.ConversationId.Should().Be(original.ConversationId);
        clone.Statuses.Should().BeEquivalentTo(original.Statuses);
        clone.MinToolCallCount.Should().Be(original.MinToolCallCount);
        clone.HasToolCalls.Should().Be(original.HasToolCalls);
        clone.SearchText.Should().Be(original.SearchText);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new LogFilter
        {
            Statuses = [InferenceStatus.Completed],
            HasToolCalls = true
        };

        // Act
        var clone = original.Clone();
        clone.Statuses.Add(InferenceStatus.Failed);
        clone.HasToolCalls = false;
        clone.Page = 10;

        // Assert
        original.Statuses.Should().HaveCount(1);
        original.Statuses.Should().Contain(InferenceStatus.Completed);
        original.HasToolCalls.Should().BeTrue();
        original.Page.Should().Be(1);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllValues()
    {
        // Arrange
        var filter = new LogFilter
        {
            Page = 5,
            PageSize = 100,
            From = DateTimeOffset.Now.AddDays(-7),
            To = DateTimeOffset.Now,
            ChatSessionId = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Statuses = [InferenceStatus.Failed],
            MinToolCallCount = 3,
            HasToolCalls = true,
            SearchText = "search",
            SortAscending = true
        };

        // Act
        filter.Reset();

        // Assert
        filter.Page.Should().Be(1);
        filter.PageSize.Should().Be(50);
        filter.From.Should().BeNull();
        filter.To.Should().BeNull();
        filter.ChatSessionId.Should().BeNull();
        filter.ConversationId.Should().BeNull();
        filter.Statuses.Should().BeEmpty();
        filter.MinToolCallCount.Should().BeNull();
        filter.HasToolCalls.Should().BeNull();
        filter.SearchText.Should().BeNull();
        filter.SortAscending.Should().BeFalse();
    }

    #endregion

    #region ToQueryString Tests

    [Fact]
    public void ToQueryString_WithDefaultValues_IncludesRequiredParams()
    {
        // Arrange
        var filter = new LogFilter();

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("page=1");
        query.Should().Contain("pageSize=50");
        query.Should().Contain("sortDirection=Descending");
    }

    [Fact]
    public void ToQueryString_WithTimeRange_IncludesFromAndTo()
    {
        // Arrange
        var from = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2024, 1, 31, 23, 59, 59, TimeSpan.Zero);
        var filter = new LogFilter
        {
            From = from,
            To = to
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("from=");
        query.Should().Contain("to=");
    }

    [Fact]
    public void ToQueryString_WithStatuses_IncludesEachStatus()
    {
        // Arrange
        var filter = new LogFilter
        {
            Statuses = [InferenceStatus.Completed, InferenceStatus.Failed]
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("statuses=Completed");
        query.Should().Contain("statuses=Failed");
    }

    [Fact]
    public void ToQueryString_WithChatSessionId_IncludesId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var filter = new LogFilter
        {
            ChatSessionId = sessionId
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain($"chatSessionId={sessionId}");
    }

    [Fact]
    public void ToQueryString_WithHasToolCalls_IncludesBoolean()
    {
        // Arrange
        var filter = new LogFilter
        {
            HasToolCalls = true
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("hasToolCalls=true");
    }

    [Fact]
    public void ToQueryString_WithHasToolCallsFalse_IncludesFalse()
    {
        // Arrange
        var filter = new LogFilter
        {
            HasToolCalls = false
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("hasToolCalls=false");
    }

    [Fact]
    public void ToQueryString_WithMinToolCallCount_IncludesCount()
    {
        // Arrange
        var filter = new LogFilter
        {
            MinToolCallCount = 5
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("minToolCallCount=5");
    }

    [Fact]
    public void ToQueryString_WithSearchText_IncludesEncodedText()
    {
        // Arrange
        var filter = new LogFilter
        {
            SearchText = "test query"
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("searchText=test%20query");
    }

    [Fact]
    public void ToQueryString_WithSortAscending_IncludesAscending()
    {
        // Arrange
        var filter = new LogFilter
        {
            SortAscending = true
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().Contain("sortDirection=Ascending");
    }

    [Fact]
    public void ToQueryString_WithEmptySearchText_DoesNotIncludeSearchText()
    {
        // Arrange
        var filter = new LogFilter
        {
            SearchText = ""
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().NotContain("searchText=");
    }

    [Fact]
    public void ToQueryString_WithWhitespaceSearchText_DoesNotIncludeSearchText()
    {
        // Arrange
        var filter = new LogFilter
        {
            SearchText = "   "
        };

        // Act
        var query = filter.ToQueryString();

        // Assert
        query.Should().NotContain("searchText=");
    }

    #endregion
}

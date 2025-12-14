using Aesir.Client.Web.Modules.Observability.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Tests.Unit.Observability.Models;

public class TimeGroupedLogsTests
{
    #region GroupLogs Tests

    [Fact]
    public void GroupLogs_WithEmptyList_ReturnsEmptyGroups()
    {
        // Arrange
        var logs = new List<AesirInferenceLogSummary>();

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups.Should().BeEmpty();
    }

    [Fact]
    public void GroupLogs_WithSingleLog_ReturnsSingleGroup()
    {
        // Arrange
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(DateTimeOffset.Now)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups.Should().HaveCount(1);
        groups[0].Logs.Should().HaveCount(1);
    }

    [Fact]
    public void GroupLogs_WithLogsFromSameDay_GroupsTogether()
    {
        // Arrange
        var today = DateTimeOffset.Now;
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(today),
            CreateLog(today.AddHours(-2)),
            CreateLog(today.AddHours(-5))
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups.Should().HaveCount(1);
        groups[0].Logs.Should().HaveCount(3);
    }

    [Fact]
    public void GroupLogs_WithLogsFromDifferentDays_CreatesMultipleGroups()
    {
        // Arrange
        var today = DateTimeOffset.Now;
        var yesterday = today.AddDays(-1);
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(today),
            CreateLog(yesterday)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups.Should().HaveCount(2);
    }

    [Fact]
    public void GroupLogs_SortsGroupsByDateDescending()
    {
        // Arrange
        var today = DateTimeOffset.Now;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(twoDaysAgo),
            CreateLog(today),
            CreateLog(yesterday)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups.Should().HaveCount(3);
        groups[0].Date.Should().Be(DateOnly.FromDateTime(today.LocalDateTime));
        groups[1].Date.Should().Be(DateOnly.FromDateTime(yesterday.LocalDateTime));
        groups[2].Date.Should().Be(DateOnly.FromDateTime(twoDaysAgo.LocalDateTime));
    }

    [Fact]
    public void GroupLogs_TodayLogs_HaveTodayLabel()
    {
        // Arrange
        var today = DateTimeOffset.Now;
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(today)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups[0].Label.Should().Be("Today");
    }

    [Fact]
    public void GroupLogs_YesterdayLogs_HaveYesterdayLabel()
    {
        // Arrange
        var yesterday = DateTimeOffset.Now.AddDays(-1);
        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(yesterday)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups[0].Label.Should().Be("Yesterday");
    }

    [Fact]
    public void GroupLogs_ThisWeekLogs_HaveThisWeekLabel()
    {
        // Arrange
        // Get a date that's earlier this week but not today or yesterday
        var now = DateTimeOffset.Now;
        var dayOfWeek = (int)now.DayOfWeek;
        // If it's Sunday (0) or Monday (1), the test might not work well, so skip
        if (dayOfWeek < 2)
        {
            return; // Skip test on Sunday/Monday as "This Week" might not have valid dates
        }

        var earlierThisWeek = now.AddDays(-dayOfWeek + 1); // Monday of this week
        if (DateOnly.FromDateTime(earlierThisWeek.LocalDateTime) ==
            DateOnly.FromDateTime(now.LocalDateTime) ||
            DateOnly.FromDateTime(earlierThisWeek.LocalDateTime) ==
            DateOnly.FromDateTime(now.AddDays(-1).LocalDateTime))
        {
            earlierThisWeek = now.AddDays(-2); // Two days ago
            if ((int)earlierThisWeek.DayOfWeek >= dayOfWeek)
            {
                return; // Can't test "This Week" in this scenario
            }
        }

        var logs = new List<AesirInferenceLogSummary>
        {
            CreateLog(earlierThisWeek)
        };

        // Act
        var groups = TimeGroupedLogs.GroupLogs(logs);

        // Assert
        groups[0].Label.Should().Be("This Week");
    }

    #endregion

    #region Helper Methods

    private static AesirInferenceLogSummary CreateLog(DateTimeOffset startedAt)
    {
        return new AesirInferenceLogSummary
        {
            Id = Guid.NewGuid(),
            UserQueryTruncated = "Test query",
            StartedAt = startedAt,
            Status = InferenceStatus.Completed,
            ToolCallCount = 0
        };
    }

    #endregion
}

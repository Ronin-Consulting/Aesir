using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Observability.Models;

/// <summary>
/// Represents inference logs grouped by a time period for timeline display.
/// </summary>
public class TimeGroupedLogs
{
    /// <summary>
    /// Gets or sets the display label for this group (e.g., "Today", "Yesterday", "This Week").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date for this group.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the inference logs in this group.
    /// </summary>
    public List<AesirInferenceLogSummary> Logs { get; set; } = [];

    /// <summary>
    /// Groups inference logs by time period for timeline display.
    /// </summary>
    public static List<TimeGroupedLogs> GroupLogs(IEnumerable<AesirInferenceLogSummary> logs)
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        var yesterday = today.AddDays(-1);
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);

        var groups = new Dictionary<DateOnly, TimeGroupedLogs>();

        foreach (var log in logs)
        {
            var logDate = DateOnly.FromDateTime(log.StartedAt.LocalDateTime);

            if (!groups.TryGetValue(logDate, out var group))
            {
                group = new TimeGroupedLogs
                {
                    Date = logDate,
                    Label = GetDateLabel(logDate, today, yesterday, thisWeekStart, lastWeekStart),
                    Logs = []
                };
                groups[logDate] = group;
            }

            group.Logs.Add(log);
        }

        // Sort groups by date descending (most recent first)
        return groups.Values
            .OrderByDescending(g => g.Date)
            .ToList();
    }

    private static string GetDateLabel(DateOnly date, DateOnly today, DateOnly yesterday, DateOnly thisWeekStart, DateOnly lastWeekStart)
    {
        if (date == today)
            return "Today";

        if (date == yesterday)
            return "Yesterday";

        if (date >= thisWeekStart)
            return "This Week";

        if (date >= lastWeekStart)
            return "Last Week";

        if (date.Year == today.Year && date.Month == today.Month)
            return "This Month";

        if (date.Year == today.Year)
            return date.ToString("MMMM");

        return date.ToString("MMMM yyyy");
    }
}

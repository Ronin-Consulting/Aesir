using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Observability.Models;

/// <summary>
/// Represents unified timeline logs grouped by a time period for timeline display.
/// </summary>
public class UnifiedTimeGroupedLogs
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
    /// Gets or sets the unified timeline items in this group.
    /// </summary>
    public List<UnifiedTimelineItem> Items { get; set; } = [];

    /// <summary>
    /// Groups unified timeline items by time period for timeline display.
    /// </summary>
    public static List<UnifiedTimeGroupedLogs> GroupItems(IEnumerable<UnifiedTimelineItem> items)
    {
        var now = DateTimeOffset.Now;
        var today = DateOnly.FromDateTime(now.LocalDateTime);
        var yesterday = today.AddDays(-1);
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);

        var groups = new Dictionary<DateOnly, UnifiedTimeGroupedLogs>();

        foreach (var item in items)
        {
            var itemDate = DateOnly.FromDateTime(item.StartedAt.LocalDateTime);

            if (!groups.TryGetValue(itemDate, out var group))
            {
                group = new UnifiedTimeGroupedLogs
                {
                    Date = itemDate,
                    Label = GetDateLabel(itemDate, today, yesterday, thisWeekStart, lastWeekStart),
                    Items = []
                };
                groups[itemDate] = group;
            }

            group.Items.Add(item);
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

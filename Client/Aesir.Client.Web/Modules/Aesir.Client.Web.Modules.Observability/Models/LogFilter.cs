using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Observability.Models;

/// <summary>
/// Client-side model for log filtering criteria.
/// </summary>
public class LogFilter
{
    /// <summary>
    /// Gets or sets the current page (1-based).
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the start of the time range filter.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Gets or sets the end of the time range filter.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Gets or sets the chat session ID filter.
    /// </summary>
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the conversation ID filter.
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Gets or sets the log levels to filter by.
    /// </summary>
    public List<KernelLogLevel> Levels { get; set; } = [];

    /// <summary>
    /// Gets or sets the log types to filter by.
    /// </summary>
    public List<KernelLogType> Types { get; set; } = [];

    /// <summary>
    /// Gets or sets the function name search term (partial match).
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Gets or sets the plugin name search term (partial match).
    /// </summary>
    public string? PluginName { get; set; }

    /// <summary>
    /// Gets or sets the message search term (partial match).
    /// </summary>
    public string? MessageSearch { get; set; }

    /// <summary>
    /// Gets or sets whether to sort ascending (default is descending).
    /// </summary>
    public bool SortAscending { get; set; } = false;

    /// <summary>
    /// Creates a copy of the filter.
    /// </summary>
    public LogFilter Clone() => new()
    {
        Page = Page,
        PageSize = PageSize,
        From = From,
        To = To,
        ChatSessionId = ChatSessionId,
        ConversationId = ConversationId,
        Levels = [.. Levels],
        Types = [.. Types],
        FunctionName = FunctionName,
        PluginName = PluginName,
        MessageSearch = MessageSearch,
        SortAscending = SortAscending
    };

    /// <summary>
    /// Resets all filters to default values.
    /// </summary>
    public void Reset()
    {
        Page = 1;
        PageSize = 50;
        From = null;
        To = null;
        ChatSessionId = null;
        ConversationId = null;
        Levels.Clear();
        Types.Clear();
        FunctionName = null;
        PluginName = null;
        MessageSearch = null;
        SortAscending = false;
    }

    /// <summary>
    /// Builds a query string for the API request.
    /// </summary>
    public string ToQueryString()
    {
        var parameters = new List<string>
        {
            $"page={Page}",
            $"pageSize={PageSize}"
        };

        if (From.HasValue)
            parameters.Add($"from={Uri.EscapeDataString(From.Value.ToString("O"))}");

        if (To.HasValue)
            parameters.Add($"to={Uri.EscapeDataString(To.Value.ToString("O"))}");

        if (ChatSessionId.HasValue)
            parameters.Add($"chatSessionId={ChatSessionId.Value}");

        if (ConversationId.HasValue)
            parameters.Add($"conversationId={ConversationId.Value}");

        foreach (var level in Levels)
            parameters.Add($"levels={level}");

        foreach (var type in Types)
            parameters.Add($"types={type}");

        if (!string.IsNullOrWhiteSpace(FunctionName))
            parameters.Add($"functionName={Uri.EscapeDataString(FunctionName)}");

        if (!string.IsNullOrWhiteSpace(PluginName))
            parameters.Add($"pluginName={Uri.EscapeDataString(PluginName)}");

        if (!string.IsNullOrWhiteSpace(MessageSearch))
            parameters.Add($"messageSearch={Uri.EscapeDataString(MessageSearch)}");

        parameters.Add($"sortDirection={( SortAscending ? "Ascending" : "Descending")}");

        return string.Join("&", parameters);
    }
}

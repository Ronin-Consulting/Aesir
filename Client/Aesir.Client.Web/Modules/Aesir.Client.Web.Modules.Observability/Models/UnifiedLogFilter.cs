using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Observability.Models;

/// <summary>
/// Client-side model for unified log filtering criteria.
/// Supports filtering for both inference and document logs.
/// </summary>
public class UnifiedLogFilter
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
    /// Gets or sets the log types to include in results.
    /// If empty, includes all types.
    /// </summary>
    public List<TimelineItemType> LogTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the inference statuses to filter by.
    /// </summary>
    public List<InferenceStatus> InferenceStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the document log statuses to filter by.
    /// </summary>
    public List<DocumentLogStatus> DocumentStatuses { get; set; } = [];

    /// <summary>
    /// Gets or sets the document operation types to filter by.
    /// </summary>
    public List<DocumentOperationType> DocumentOperationTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to filter for inference logs with tool calls.
    /// </summary>
    public bool? HasToolCalls { get; set; }

    /// <summary>
    /// Gets or sets the search text for partial match.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Gets or sets whether to sort ascending (default is descending).
    /// </summary>
    public bool SortAscending { get; set; } = false;

    /// <summary>
    /// Creates a copy of the filter.
    /// </summary>
    public UnifiedLogFilter Clone() => new()
    {
        Page = Page,
        PageSize = PageSize,
        From = From,
        To = To,
        ChatSessionId = ChatSessionId,
        ConversationId = ConversationId,
        LogTypes = [.. LogTypes],
        InferenceStatuses = [.. InferenceStatuses],
        DocumentStatuses = [.. DocumentStatuses],
        DocumentOperationTypes = [.. DocumentOperationTypes],
        HasToolCalls = HasToolCalls,
        SearchText = SearchText,
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
        LogTypes.Clear();
        InferenceStatuses.Clear();
        DocumentStatuses.Clear();
        DocumentOperationTypes.Clear();
        HasToolCalls = null;
        SearchText = null;
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

        foreach (var logType in LogTypes)
            parameters.Add($"logTypes={logType}");

        foreach (var status in InferenceStatuses)
            parameters.Add($"inferenceStatuses={status}");

        foreach (var status in DocumentStatuses)
            parameters.Add($"documentStatuses={status}");

        foreach (var opType in DocumentOperationTypes)
            parameters.Add($"documentOperationTypes={opType}");

        if (HasToolCalls.HasValue)
            parameters.Add($"hasToolCalls={HasToolCalls.Value.ToString().ToLower()}");

        if (!string.IsNullOrWhiteSpace(SearchText))
            parameters.Add($"searchText={Uri.EscapeDataString(SearchText)}");

        parameters.Add($"sortDirection={(SortAscending ? "Ascending" : "Descending")}");

        return string.Join("&", parameters);
    }

    /// <summary>
    /// Gets whether any filters are currently active.
    /// </summary>
    public bool HasActiveFilters =>
        LogTypes.Count > 0 ||
        InferenceStatuses.Count > 0 ||
        DocumentStatuses.Count > 0 ||
        DocumentOperationTypes.Count > 0 ||
        HasToolCalls.HasValue ||
        !string.IsNullOrWhiteSpace(SearchText) ||
        From.HasValue ||
        To.HasValue ||
        ChatSessionId.HasValue ||
        ConversationId.HasValue;
}

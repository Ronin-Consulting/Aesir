using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a unified timeline item that can be either an inference log or document log.
/// Used for displaying mixed log types in a single timeline view.
/// </summary>
public class UnifiedTimelineItem
{
    /// <summary>
    /// The type of timeline item (Inference or Document).
    /// </summary>
    [JsonPropertyName("type")]
    public TimelineItemType Type { get; set; }

    /// <summary>
    /// When the operation started (UTC). Used for sorting.
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// The inference log summary, if this is an inference item.
    /// </summary>
    [JsonPropertyName("inference_log")]
    public AesirInferenceLogSummary? InferenceLog { get; set; }

    /// <summary>
    /// The document log summary, if this is a document item.
    /// </summary>
    [JsonPropertyName("document_log")]
    public AesirDocumentLogSummary? DocumentLog { get; set; }
}

/// <summary>
/// Type of timeline item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimelineItemType
{
    /// <summary>An inference/chat operation.</summary>
    Inference,

    /// <summary>A document operation (upload, embeddings, delete).</summary>
    Document
}

/// <summary>
/// Request model for filtering and searching unified timeline.
/// </summary>
public class UnifiedTimelineFilterRequest
{
    /// <summary>
    /// Page number (1-based).
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page (max 200).
    /// </summary>
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Filter by start time (from).
    /// </summary>
    [JsonPropertyName("from")]
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Filter by end time (to).
    /// </summary>
    [JsonPropertyName("to")]
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Filter by chat session ID.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Filter by conversation ID.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Filter by timeline item types to include.
    /// If empty or null, includes all types.
    /// </summary>
    [JsonPropertyName("log_types")]
    public List<TimelineItemType>? LogTypes { get; set; }

    /// <summary>
    /// Filter by inference statuses (applies to inference logs only).
    /// </summary>
    [JsonPropertyName("inference_statuses")]
    public List<InferenceStatus>? InferenceStatuses { get; set; }

    /// <summary>
    /// Filter by document statuses (applies to document logs only).
    /// </summary>
    [JsonPropertyName("document_statuses")]
    public List<DocumentLogStatus>? DocumentStatuses { get; set; }

    /// <summary>
    /// Filter by document operation types (applies to document logs only).
    /// </summary>
    [JsonPropertyName("operation_types")]
    public List<DocumentOperationType>? OperationTypes { get; set; }

    /// <summary>
    /// Filter to only show inferences with tool calls.
    /// </summary>
    [JsonPropertyName("has_tool_calls")]
    public bool? HasToolCalls { get; set; }

    /// <summary>
    /// Search text to match against user query or file name.
    /// </summary>
    [JsonPropertyName("search_text")]
    public string? SearchText { get; set; }

    /// <summary>
    /// Sort direction for results.
    /// </summary>
    [JsonPropertyName("sort_direction")]
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Paginated response for unified timeline queries.
/// </summary>
public class PagedUnifiedTimelineResponse
{
    /// <summary>
    /// The unified timeline items for the current page.
    /// </summary>
    [JsonPropertyName("items")]
    public IEnumerable<UnifiedTimelineItem> Items { get; set; } = Enumerable.Empty<UnifiedTimelineItem>();

    /// <summary>
    /// Total number of items matching the filter.
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    [JsonPropertyName("total_pages")]
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    [JsonPropertyName("has_next_page")]
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    [JsonPropertyName("has_previous_page")]
    public bool HasPreviousPage => Page > 1;
}

using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a complete inference operation including the user query, tool calls, and AI response.
/// This is the parent record for observability tracking.
/// </summary>
public class AesirInferenceLog
{
    /// <summary>
    /// Unique identifier for this inference log entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The chat session this inference belongs to.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The conversation ID within the chat session.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The full user query that triggered this inference.
    /// </summary>
    [JsonPropertyName("user_query")]
    public string UserQuery { get; set; } = null!;

    /// <summary>
    /// Truncated user query (first 500 chars) for list views.
    /// </summary>
    [JsonPropertyName("user_query_truncated")]
    public string? UserQueryTruncated { get; set; }

    /// <summary>
    /// The AI assistant's response (truncated to 500 chars for list views).
    /// </summary>
    [JsonPropertyName("assistant_response")]
    public string? AssistantResponse { get; set; }

    /// <summary>
    /// The full AI assistant's response (complete, not truncated).
    /// Only populated when fetching full details by ID.
    /// </summary>
    [JsonPropertyName("assistant_response_full")]
    public string? AssistantResponseFull { get; set; }

    /// <summary>
    /// All tool calls made during this inference, with full details.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<AesirToolCallInfo> ToolCalls { get; set; } = new();

    /// <summary>
    /// Number of tool calls made (denormalized for efficient queries).
    /// </summary>
    [JsonPropertyName("tool_call_count")]
    public int ToolCallCount { get; set; }

    /// <summary>
    /// Total duration of the inference in milliseconds.
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public long? TotalDurationMs { get; set; }

    /// <summary>
    /// When the inference started (UTC).
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When the inference completed (UTC).
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The status of this inference operation.
    /// </summary>
    [JsonPropertyName("status")]
    public InferenceStatus Status { get; set; }

    /// <summary>
    /// Error message if the inference failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Status of an inference operation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InferenceStatus
{
    /// <summary>Inference is currently in progress.</summary>
    InProgress,

    /// <summary>Inference completed successfully.</summary>
    Completed,

    /// <summary>Inference failed with an error.</summary>
    Failed,

    /// <summary>Inference was cancelled by the user.</summary>
    Cancelled
}

/// <summary>
/// Summary view of an inference log for list displays.
/// Excludes full tool call details for performance.
/// </summary>
public class AesirInferenceLogSummary
{
    /// <summary>
    /// Unique identifier for this inference log entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The chat session this inference belongs to.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The conversation ID within the chat session.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Truncated user query (first 500 chars) for list views.
    /// </summary>
    [JsonPropertyName("user_query_truncated")]
    public string? UserQueryTruncated { get; set; }

    /// <summary>
    /// The AI assistant's response (truncated to 500 chars).
    /// </summary>
    [JsonPropertyName("assistant_response")]
    public string? AssistantResponse { get; set; }

    /// <summary>
    /// Number of tool calls made.
    /// </summary>
    [JsonPropertyName("tool_call_count")]
    public int ToolCallCount { get; set; }

    /// <summary>
    /// Summary of tool calls - just function names and statuses.
    /// </summary>
    [JsonPropertyName("tool_call_summaries")]
    public List<ToolCallSummary>? ToolCallSummaries { get; set; }

    /// <summary>
    /// Total duration of the inference in milliseconds.
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public long? TotalDurationMs { get; set; }

    /// <summary>
    /// When the inference started (UTC).
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When the inference completed (UTC).
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The status of this inference operation.
    /// </summary>
    [JsonPropertyName("status")]
    public InferenceStatus Status { get; set; }

    /// <summary>
    /// Error message if the inference failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Lightweight summary of a tool call for list views.
/// </summary>
public class ToolCallSummary
{
    /// <summary>
    /// The function name that was called.
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = null!;

    /// <summary>
    /// The plugin name.
    /// </summary>
    [JsonPropertyName("plugin_name")]
    public string? PluginName { get; set; }

    /// <summary>
    /// The tool type for UI styling.
    /// </summary>
    [JsonPropertyName("tool_type")]
    public ToolCallType ToolType { get; set; }

    /// <summary>
    /// The status of the tool call.
    /// </summary>
    [JsonPropertyName("status")]
    public ToolCallStatus Status { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public long? DurationMs { get; set; }
}

/// <summary>
/// Request model for filtering and searching inference logs.
/// </summary>
public class InferenceLogFilterRequest
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
    /// Filter by inference status.
    /// </summary>
    [JsonPropertyName("statuses")]
    public List<InferenceStatus>? Statuses { get; set; }

    /// <summary>
    /// Filter by minimum tool call count.
    /// </summary>
    [JsonPropertyName("min_tool_call_count")]
    public int? MinToolCallCount { get; set; }

    /// <summary>
    /// Filter to only show inferences with tool calls.
    /// </summary>
    [JsonPropertyName("has_tool_calls")]
    public bool? HasToolCalls { get; set; }

    /// <summary>
    /// Search text to match against user query or function names.
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
/// Sort direction for queries.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortDirection
{
    /// <summary>Sort ascending (oldest first).</summary>
    Ascending,

    /// <summary>Sort descending (newest first).</summary>
    Descending
}

/// <summary>
/// Paginated response for inference log queries.
/// </summary>
public class PagedInferenceLogResponse
{
    /// <summary>
    /// The inference log summaries for the current page.
    /// </summary>
    [JsonPropertyName("items")]
    public IEnumerable<AesirInferenceLogSummary> Items { get; set; } = Enumerable.Empty<AesirInferenceLogSummary>();

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

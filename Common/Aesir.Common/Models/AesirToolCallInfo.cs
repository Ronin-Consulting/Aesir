using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents information about a tool call being made during chat completion.
/// </summary>
public class AesirToolCallInfo
{
    /// <summary>
    /// Unique identifier for this tool call (matches SK ToolCallId).
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; } = null!;

    /// <summary>
    /// The name of the function being called.
    /// </summary>
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = null!;

    /// <summary>
    /// The plugin name containing the function.
    /// </summary>
    [JsonPropertyName("plugin_name")]
    public string? PluginName { get; set; }

    /// <summary>
    /// Human-readable description of the function.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// The type/category of tool for UI styling.
    /// </summary>
    [JsonPropertyName("tool_type")]
    public ToolCallType ToolType { get; set; }

    /// <summary>
    /// Input arguments passed to the function (serialized as key-value pairs).
    /// </summary>
    [JsonPropertyName("arguments")]
    public Dictionary<string, string>? Arguments { get; set; }

    /// <summary>
    /// The status of the tool call.
    /// </summary>
    [JsonPropertyName("status")]
    public ToolCallStatus Status { get; set; }

    /// <summary>
    /// Timestamp when the tool call started (UTC).
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the tool call completed (UTC).
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Duration in milliseconds (calculated from started_at and completed_at).
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public long? DurationMs => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds
        : null;

    /// <summary>
    /// The result of the tool call (for completed calls).
    /// May be truncated for large results.
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// Error message if the tool call failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// The chat session ID this tool call belongs to (for logging/observability).
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The conversation ID this tool call belongs to (for logging/observability).
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The underlying C# method name being called.
    /// </summary>
    [JsonPropertyName("underlying_method")]
    public string? UnderlyingMethod { get; set; }
}

/// <summary>
/// Categories of tool calls for UI styling and icons.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolCallType
{
    /// <summary>Document/RAG search tools.</summary>
    DocumentSearch,

    /// <summary>Web search tools.</summary>
    WebSearch,

    /// <summary>MCP server tools.</summary>
    McpServer,

    /// <summary>Image analysis tools.</summary>
    ImageAnalysis,

    /// <summary>Document summarization tools.</summary>
    Summarization,

    /// <summary>Unknown/other tool types.</summary>
    Other
}

/// <summary>
/// Status of a tool call during execution.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ToolCallStatus
{
    /// <summary>Tool call has started, awaiting result.</summary>
    Started,

    /// <summary>Tool call completed successfully.</summary>
    Completed,

    /// <summary>Tool call failed with an error.</summary>
    Failed
}

/// <summary>
/// Type of streaming event for proper client handling.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StreamEventType
{
    /// <summary>Text content delta.</summary>
    Text,

    /// <summary>Thinking/reasoning content delta.</summary>
    Thinking,

    /// <summary>Tool call has started.</summary>
    ToolCallStart,

    /// <summary>Tool call has completed (success or failure).</summary>
    ToolCallResult
}

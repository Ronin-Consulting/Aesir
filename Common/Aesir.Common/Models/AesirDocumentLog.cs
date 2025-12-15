using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a document operation log entry for observability tracking.
/// Tracks file uploads, embedding generation, updates, and deletions.
/// </summary>
public class AesirDocumentLog
{
    /// <summary>
    /// Unique identifier for this document log entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The chat session this operation belongs to.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The conversation ID for conversation-specific documents.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The type of document operation performed.
    /// </summary>
    [JsonPropertyName("operation_type")]
    public DocumentOperationType OperationType { get; set; }

    /// <summary>
    /// The original file name.
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// The storage path of the file.
    /// </summary>
    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    [JsonPropertyName("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// The MIME content type of the file.
    /// </summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Number of text chunks created from the document.
    /// </summary>
    [JsonPropertyName("chunk_count")]
    public int? ChunkCount { get; set; }

    /// <summary>
    /// Number of embeddings generated and stored.
    /// </summary>
    [JsonPropertyName("embedding_count")]
    public int? EmbeddingCount { get; set; }

    /// <summary>
    /// Total token count across all chunks.
    /// </summary>
    [JsonPropertyName("token_count")]
    public int? TokenCount { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds.
    /// </summary>
    [JsonPropertyName("processing_duration_ms")]
    public long? ProcessingDurationMs { get; set; }

    /// <summary>
    /// When the operation started (UTC).
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When the operation completed (UTC).
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The status of this document operation.
    /// </summary>
    [JsonPropertyName("status")]
    public DocumentLogStatus Status { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional metadata for flexible storage.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Type of document operation being logged.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentOperationType
{
    /// <summary>A file was uploaded to storage.</summary>
    FileUploaded,

    /// <summary>Embeddings were generated and stored for a document.</summary>
    EmbeddingsGenerated,

    /// <summary>Embeddings were updated/regenerated for a document.</summary>
    EmbeddingsUpdated,

    /// <summary>A file was deleted from storage.</summary>
    FileDeleted
}

/// <summary>
/// Status of a document operation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DocumentLogStatus
{
    /// <summary>Operation is currently in progress.</summary>
    InProgress,

    /// <summary>Operation completed successfully.</summary>
    Completed,

    /// <summary>Operation failed with an error.</summary>
    Failed
}

/// <summary>
/// Summary view of a document log for list displays.
/// </summary>
public class AesirDocumentLogSummary
{
    /// <summary>
    /// Unique identifier for this document log entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The chat session this operation belongs to.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The conversation ID for conversation-specific documents.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The type of document operation performed.
    /// </summary>
    [JsonPropertyName("operation_type")]
    public DocumentOperationType OperationType { get; set; }

    /// <summary>
    /// The original file name.
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = null!;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    [JsonPropertyName("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// The MIME content type of the file.
    /// </summary>
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }

    /// <summary>
    /// Number of text chunks created from the document.
    /// </summary>
    [JsonPropertyName("chunk_count")]
    public int? ChunkCount { get; set; }

    /// <summary>
    /// Number of embeddings generated and stored.
    /// </summary>
    [JsonPropertyName("embedding_count")]
    public int? EmbeddingCount { get; set; }

    /// <summary>
    /// Total token count across all chunks.
    /// </summary>
    [JsonPropertyName("token_count")]
    public int? TokenCount { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds.
    /// </summary>
    [JsonPropertyName("processing_duration_ms")]
    public long? ProcessingDurationMs { get; set; }

    /// <summary>
    /// When the operation started (UTC).
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When the operation completed (UTC).
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The status of this document operation.
    /// </summary>
    [JsonPropertyName("status")]
    public DocumentLogStatus Status { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Request model for filtering and searching document logs.
/// </summary>
public class DocumentLogFilterRequest
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
    /// Filter by document log status.
    /// </summary>
    [JsonPropertyName("statuses")]
    public List<DocumentLogStatus>? Statuses { get; set; }

    /// <summary>
    /// Filter by operation types.
    /// </summary>
    [JsonPropertyName("operation_types")]
    public List<DocumentOperationType>? OperationTypes { get; set; }

    /// <summary>
    /// Search text to match against file name.
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
/// Paginated response for document log queries.
/// </summary>
public class PagedDocumentLogResponse
{
    /// <summary>
    /// The document log summaries for the current page.
    /// </summary>
    [JsonPropertyName("items")]
    public IEnumerable<AesirDocumentLogSummary> Items { get; set; } = Enumerable.Empty<AesirDocumentLogSummary>();

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

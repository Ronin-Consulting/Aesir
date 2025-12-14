using System.ComponentModel.DataAnnotations;
using Aesir.Common.Models;

namespace Aesir.Modules.Logging.Models;

/// <summary>
/// Request model for filtering and paginating kernel logs.
/// </summary>
public class KernelLogFilterRequest
{
    // === Pagination ===

    /// <summary>
    /// Page number (1-based). Default: 1
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be 1 or greater")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Default: 50, Maximum: 200
    /// </summary>
    [Range(1, 200, ErrorMessage = "PageSize must be between 1 and 200")]
    public int PageSize { get; set; } = 50;

    // === Time Range Filters ===

    /// <summary>
    /// Start of time range filter (UTC). Optional.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// End of time range filter (UTC). Optional.
    /// </summary>
    public DateTimeOffset? To { get; set; }

    // === Session Filters ===

    /// <summary>
    /// Filter by chat session ID. Optional.
    /// </summary>
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// Filter by conversation ID. Optional.
    /// </summary>
    public Guid? ConversationId { get; set; }

    // === Log Level Filter ===

    /// <summary>
    /// Filter by log levels. When null or empty, returns all levels.
    /// Multiple levels are combined with OR logic.
    /// </summary>
    public List<KernelLogLevel>? Levels { get; set; }

    // === Log Type Filter ===

    /// <summary>
    /// Filter by log types. When null or empty, returns all types.
    /// Multiple types are combined with OR logic.
    /// </summary>
    public List<KernelLogType>? Types { get; set; }

    // === Function/Plugin Search ===

    /// <summary>
    /// Partial match search for function name (case-insensitive).
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Partial match search for plugin name (case-insensitive).
    /// </summary>
    public string? PluginName { get; set; }

    // === Text Search ===

    /// <summary>
    /// Partial match search in message field (case-insensitive).
    /// </summary>
    public string? MessageSearch { get; set; }

    // === Sorting ===

    /// <summary>
    /// Sort direction for created_at. Default: Descending (newest first)
    /// </summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

/// <summary>
/// Sort direction for query results.
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}

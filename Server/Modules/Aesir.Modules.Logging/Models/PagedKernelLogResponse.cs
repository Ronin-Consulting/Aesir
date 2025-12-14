namespace Aesir.Modules.Logging.Models;

/// <summary>
/// Paginated response containing kernel logs and metadata.
/// </summary>
public class PagedKernelLogResponse
{
    /// <summary>
    /// The kernel logs for the current page.
    /// </summary>
    public IEnumerable<KernelLog> Items { get; set; } = [];

    /// <summary>
    /// Total number of items matching the filter criteria.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

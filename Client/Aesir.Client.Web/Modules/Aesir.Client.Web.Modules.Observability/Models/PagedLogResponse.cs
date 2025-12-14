using System.Text.Json.Serialization;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Observability.Models;

/// <summary>
/// Client-side model for paginated inference log responses.
/// </summary>
public class PagedLogResponse
{
    /// <summary>
    /// Gets or sets the inference log summaries for the current page.
    /// </summary>
    [JsonPropertyName("items")]
    public IEnumerable<AesirInferenceLogSummary> Items { get; set; } = [];

    /// <summary>
    /// Gets or sets the total count of items matching the filter.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

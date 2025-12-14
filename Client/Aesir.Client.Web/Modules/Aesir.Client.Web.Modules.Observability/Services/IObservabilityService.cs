using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Modules.Observability.Models;

namespace Aesir.Client.Web.Modules.Observability.Services;

/// <summary>
/// Service for fetching and managing observability log data.
/// </summary>
public interface IObservabilityService
{
    /// <summary>
    /// Gets whether logs are currently being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the current filter settings.
    /// </summary>
    LogFilter CurrentFilter { get; }

    /// <summary>
    /// Gets the current paged response.
    /// </summary>
    PagedLogResponse? CurrentResponse { get; }

    /// <summary>
    /// Gets the current grouped logs for timeline display.
    /// </summary>
    IReadOnlyList<TimeGroupedLogs> GroupedLogs { get; }

    /// <summary>
    /// Event raised when logs have been loaded or updated.
    /// </summary>
    event Action? OnLogsChanged;

    /// <summary>
    /// Loads logs using the specified filter.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> LoadLogsAsync(LogFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Refreshes the current logs using the existing filter.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads the next page of logs.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> LoadNextPageAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads the previous page of logs.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> LoadPreviousPageAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies a new filter and reloads logs.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> ApplyFilterAsync(LogFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Clears all filters and reloads logs.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> ClearFilterAsync(CancellationToken ct = default);
}

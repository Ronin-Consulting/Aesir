using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;
using Aesir.Client.Web.Modules.Observability.Models;

namespace Aesir.Client.Web.Modules.Observability.Services;

/// <summary>
/// Service for fetching and managing observability log data including inference and document logs.
/// </summary>
public interface IObservabilityService
{
    /// <summary>
    /// Gets whether logs are currently being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the current filter settings (for legacy inference-only view).
    /// </summary>
    LogFilter CurrentFilter { get; }

    /// <summary>
    /// Gets the current unified filter settings.
    /// </summary>
    UnifiedLogFilter CurrentUnifiedFilter { get; }

    /// <summary>
    /// Gets the current paged response (for legacy inference-only view).
    /// </summary>
    PagedLogResponse? CurrentResponse { get; }

    /// <summary>
    /// Gets the current unified paged response.
    /// </summary>
    PagedUnifiedTimelineResponse? CurrentUnifiedResponse { get; }

    /// <summary>
    /// Gets the current grouped logs for timeline display (for legacy inference-only view).
    /// </summary>
    IReadOnlyList<TimeGroupedLogs> GroupedLogs { get; }

    /// <summary>
    /// Gets the current unified grouped logs for timeline display.
    /// </summary>
    IReadOnlyList<UnifiedTimeGroupedLogs> UnifiedGroupedLogs { get; }

    /// <summary>
    /// Gets the currently selected inference log detail (full log with all tool calls).
    /// </summary>
    AesirInferenceLog? SelectedLogDetail { get; }

    /// <summary>
    /// Gets the currently selected document log detail.
    /// </summary>
    AesirDocumentLog? SelectedDocumentLogDetail { get; }

    /// <summary>
    /// Event raised when logs have been loaded or updated.
    /// </summary>
    event Action? OnLogsChanged;

    /// <summary>
    /// Loads logs using the specified filter (legacy inference-only).
    /// </summary>
    Task<ApiResult<PagedLogResponse>> LoadLogsAsync(LogFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Loads unified timeline logs using the specified filter.
    /// </summary>
    Task<ApiResult<PagedUnifiedTimelineResponse>> LoadUnifiedLogsAsync(UnifiedLogFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Loads the full detail for a specific inference log.
    /// </summary>
    /// <param name="id">The inference log ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<AesirInferenceLog>> LoadLogDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Loads the full detail for a specific document log.
    /// </summary>
    /// <param name="id">The document log ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<AesirDocumentLog>> LoadDocumentLogDetailAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Clears the currently selected log detail.
    /// </summary>
    void ClearSelectedDetail();

    /// <summary>
    /// Refreshes the current logs using the existing filter.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> RefreshAsync(CancellationToken ct = default);

    /// <summary>
    /// Refreshes the current unified logs using the existing filter.
    /// </summary>
    Task<ApiResult<PagedUnifiedTimelineResponse>> RefreshUnifiedAsync(CancellationToken ct = default);

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
    /// Applies a new unified filter and reloads logs.
    /// </summary>
    Task<ApiResult<PagedUnifiedTimelineResponse>> ApplyUnifiedFilterAsync(UnifiedLogFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Clears all filters and reloads logs.
    /// </summary>
    Task<ApiResult<PagedLogResponse>> ClearFilterAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all unified filters and reloads logs.
    /// </summary>
    Task<ApiResult<PagedUnifiedTimelineResponse>> ClearUnifiedFilterAsync(CancellationToken ct = default);
}

using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;
using Aesir.Client.Web.Modules.Observability.Models;

namespace Aesir.Client.Web.Modules.Observability.Services;

/// <summary>
/// Service for fetching and managing observability log data including inference and document logs.
/// </summary>
public class ObservabilityService : IObservabilityService
{
    private readonly IApiClient _apiClient;
    private LogFilter _currentFilter = new();
    private UnifiedLogFilter _currentUnifiedFilter = new();
    private PagedLogResponse? _currentResponse;
    private PagedUnifiedTimelineResponse? _currentUnifiedResponse;
    private List<TimeGroupedLogs> _groupedLogs = [];
    private List<UnifiedTimeGroupedLogs> _unifiedGroupedLogs = [];
    private AesirInferenceLog? _selectedLogDetail;
    private AesirDocumentLog? _selectedDocumentLogDetail;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservabilityService"/> class.
    /// </summary>
    public ObservabilityService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    /// <inheritdoc />
    public bool IsLoading { get; private set; }

    /// <inheritdoc />
    public LogFilter CurrentFilter => _currentFilter;

    /// <inheritdoc />
    public UnifiedLogFilter CurrentUnifiedFilter => _currentUnifiedFilter;

    /// <inheritdoc />
    public PagedLogResponse? CurrentResponse => _currentResponse;

    /// <inheritdoc />
    public PagedUnifiedTimelineResponse? CurrentUnifiedResponse => _currentUnifiedResponse;

    /// <inheritdoc />
    public IReadOnlyList<TimeGroupedLogs> GroupedLogs => _groupedLogs;

    /// <inheritdoc />
    public IReadOnlyList<UnifiedTimeGroupedLogs> UnifiedGroupedLogs => _unifiedGroupedLogs;

    /// <inheritdoc />
    public AesirInferenceLog? SelectedLogDetail => _selectedLogDetail;

    /// <inheritdoc />
    public AesirDocumentLog? SelectedDocumentLogDetail => _selectedDocumentLogDetail;

    /// <inheritdoc />
    public event Action? OnLogsChanged;

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> LoadLogsAsync(LogFilter filter, CancellationToken ct = default)
    {
        IsLoading = true;
        NotifyChanged();

        try
        {
            _currentFilter = filter.Clone();
            var queryString = filter.ToQueryString();
            var response = await _apiClient.GetAsync<PagedLogResponse>($"/logs/inference/search?{queryString}", ct)
                .ConfigureAwait(false);

            if (response != null)
            {
                _currentResponse = response;
                _groupedLogs = TimeGroupedLogs.GroupLogs(response.Items);
                return ApiResult<PagedLogResponse>.Success(response);
            }

            return ApiResult<PagedLogResponse>.Failure("Failed to load inference logs");
        }
        catch (Exception ex)
        {
            return ApiResult<PagedLogResponse>.FromException(ex);
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirInferenceLog>> LoadLogDetailAsync(Guid id, CancellationToken ct = default)
    {
        IsLoading = true;
        NotifyChanged();

        try
        {
            var log = await _apiClient.GetAsync<AesirInferenceLog>($"/logs/inference/{id}", ct)
                .ConfigureAwait(false);

            if (log != null)
            {
                _selectedLogDetail = log;
                return ApiResult<AesirInferenceLog>.Success(log);
            }

            return ApiResult<AesirInferenceLog>.Failure("Inference log not found");
        }
        catch (Exception ex)
        {
            return ApiResult<AesirInferenceLog>.FromException(ex);
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public void ClearSelectedDetail()
    {
        _selectedLogDetail = null;
        _selectedDocumentLogDetail = null;
        NotifyChanged();
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedUnifiedTimelineResponse>> LoadUnifiedLogsAsync(UnifiedLogFilter filter, CancellationToken ct = default)
    {
        IsLoading = true;
        NotifyChanged();

        try
        {
            _currentUnifiedFilter = filter.Clone();
            var queryString = filter.ToQueryString();
            var response = await _apiClient.GetAsync<PagedUnifiedTimelineResponse>($"/logs/unified/search?{queryString}", ct)
                .ConfigureAwait(false);

            if (response != null)
            {
                _currentUnifiedResponse = response;
                _unifiedGroupedLogs = UnifiedTimeGroupedLogs.GroupItems(response.Items);
                return ApiResult<PagedUnifiedTimelineResponse>.Success(response);
            }

            return ApiResult<PagedUnifiedTimelineResponse>.Failure("Failed to load unified timeline logs");
        }
        catch (Exception ex)
        {
            return ApiResult<PagedUnifiedTimelineResponse>.FromException(ex);
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirDocumentLog>> LoadDocumentLogDetailAsync(Guid id, CancellationToken ct = default)
    {
        IsLoading = true;
        NotifyChanged();

        try
        {
            var log = await _apiClient.GetAsync<AesirDocumentLog>($"/logs/document/{id}", ct)
                .ConfigureAwait(false);

            if (log != null)
            {
                _selectedDocumentLogDetail = log;
                return ApiResult<AesirDocumentLog>.Success(log);
            }

            return ApiResult<AesirDocumentLog>.Failure("Document log not found");
        }
        catch (Exception ex)
        {
            return ApiResult<AesirDocumentLog>.FromException(ex);
        }
        finally
        {
            IsLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedUnifiedTimelineResponse>> RefreshUnifiedAsync(CancellationToken ct = default)
    {
        return await LoadUnifiedLogsAsync(_currentUnifiedFilter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedUnifiedTimelineResponse>> ApplyUnifiedFilterAsync(UnifiedLogFilter filter, CancellationToken ct = default)
    {
        // Reset to page 1 when applying new filters
        filter.Page = 1;
        return await LoadUnifiedLogsAsync(filter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedUnifiedTimelineResponse>> ClearUnifiedFilterAsync(CancellationToken ct = default)
    {
        var filter = new UnifiedLogFilter();
        return await LoadUnifiedLogsAsync(filter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> RefreshAsync(CancellationToken ct = default)
    {
        return await LoadLogsAsync(_currentFilter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> LoadNextPageAsync(CancellationToken ct = default)
    {
        if (_currentResponse == null || !_currentResponse.HasNextPage)
            return ApiResult<PagedLogResponse>.Failure("No next page available");

        var filter = _currentFilter.Clone();
        filter.Page++;
        return await LoadLogsAsync(filter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> LoadPreviousPageAsync(CancellationToken ct = default)
    {
        if (_currentResponse == null || !_currentResponse.HasPreviousPage)
            return ApiResult<PagedLogResponse>.Failure("No previous page available");

        var filter = _currentFilter.Clone();
        filter.Page--;
        return await LoadLogsAsync(filter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> ApplyFilterAsync(LogFilter filter, CancellationToken ct = default)
    {
        // Reset to page 1 when applying new filters
        filter.Page = 1;
        return await LoadLogsAsync(filter, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult<PagedLogResponse>> ClearFilterAsync(CancellationToken ct = default)
    {
        var filter = new LogFilter();
        return await LoadLogsAsync(filter, ct).ConfigureAwait(false);
    }

    private void NotifyChanged()
    {
        OnLogsChanged?.Invoke();
    }
}

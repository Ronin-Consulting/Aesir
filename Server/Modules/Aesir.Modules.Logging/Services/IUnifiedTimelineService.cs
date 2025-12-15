using Aesir.Common.Models;

namespace Aesir.Modules.Logging.Services;

/// <summary>
/// Service interface for unified timeline operations.
/// Combines inference and document logs into a single timeline view.
/// </summary>
public interface IUnifiedTimelineService
{
    /// <summary>
    /// Gets unified timeline items (both inference and document logs) with filtering.
    /// Results are merged and sorted by started_at timestamp.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated unified timeline items.</returns>
    Task<PagedUnifiedTimelineResponse> GetUnifiedTimelineAsync(
        UnifiedTimelineFilterRequest filter,
        CancellationToken cancellationToken = default);
}

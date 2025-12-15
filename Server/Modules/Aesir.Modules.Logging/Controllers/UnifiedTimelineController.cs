using Aesir.Common.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Controllers;

/// <summary>
/// API controller for unified timeline queries.
/// Provides endpoints to retrieve both inference and document logs in a unified view.
/// </summary>
[ApiController]
[Route("logs/unified")]
[Produces("application/json")]
public class UnifiedTimelineController : ControllerBase
{
    private readonly IUnifiedTimelineService _unifiedTimelineService;
    private readonly ILogger<UnifiedTimelineController> _logger;

    public UnifiedTimelineController(
        IUnifiedTimelineService unifiedTimelineService,
        ILogger<UnifiedTimelineController> logger)
    {
        _unifiedTimelineService = unifiedTimelineService ?? throw new ArgumentNullException(nameof(unifiedTimelineService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches unified timeline (both inference and document logs) with filtering and pagination.
    /// Results are merged and sorted by started_at timestamp.
    /// </summary>
    /// <param name="filter">Filter, pagination, and search parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated collection of unified timeline items.</returns>
    /// <response code="200">Returns the paginated unified timeline.</response>
    /// <response code="400">Invalid filter parameters.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedUnifiedTimelineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedUnifiedTimelineResponse>> Search(
        [FromQuery] UnifiedTimelineFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "GET /logs/unified/search - Page: {Page}, PageSize: {PageSize}, LogTypes: {LogTypes}",
            filter.Page,
            filter.PageSize,
            filter.LogTypes != null ? string.Join(",", filter.LogTypes) : "all");

        var result = await _unifiedTimelineService.GetUnifiedTimelineAsync(filter, cancellationToken);

        return Ok(result);
    }
}

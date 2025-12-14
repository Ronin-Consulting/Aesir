using Aesir.Common.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Controllers;

/// <summary>
/// API controller for querying inference execution logs.
/// Provides endpoints to retrieve inference logs with their associated tool calls.
/// </summary>
[ApiController]
[Route("logs/inference")]
[Produces("application/json")]
public class InferenceLogsController : ControllerBase
{
    private readonly IInferenceLogService _inferenceLogService;
    private readonly ILogger<InferenceLogsController> _logger;

    public InferenceLogsController(
        IInferenceLogService inferenceLogService,
        ILogger<InferenceLogsController> logger)
    {
        _inferenceLogService = inferenceLogService ?? throw new ArgumentNullException(nameof(inferenceLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a single inference log by its ID with full details.
    /// </summary>
    /// <param name="id">The inference log identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The inference log with all tool calls.</returns>
    /// <response code="200">Returns the inference log.</response>
    /// <response code="404">Inference log not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AesirInferenceLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AesirInferenceLog>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET /logs/inference/{Id}", id);

        var log = await _inferenceLogService.GetByIdAsync(id, cancellationToken);

        if (log == null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    /// <summary>
    /// Retrieves all inference logs for a specific chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of inference log summaries for the session.</returns>
    [HttpGet("chatsession/{chatSessionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AesirInferenceLogSummary>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AesirInferenceLogSummary>> GetByChatSession(
        [FromRoute] Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET /logs/inference/chatsession/{ChatSessionId}", chatSessionId);

        return await _inferenceLogService.GetByChatSessionAsync(chatSessionId, cancellationToken);
    }

    /// <summary>
    /// Retrieves the most recent inference log for a specific chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest inference log for the session.</returns>
    /// <response code="200">Returns the latest inference log.</response>
    /// <response code="404">No inference logs found for the chat session.</response>
    [HttpGet("chatsession/{chatSessionId:guid}/latest")]
    [ProducesResponseType(typeof(AesirInferenceLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AesirInferenceLog>> GetLatestByChatSession(
        [FromRoute] Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET /logs/inference/chatsession/{ChatSessionId}/latest", chatSessionId);

        var log = await _inferenceLogService.GetLatestByChatSessionAsync(chatSessionId, cancellationToken);

        if (log == null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    /// <summary>
    /// Searches inference logs with filtering, pagination, and text search.
    /// </summary>
    /// <param name="filter">Filter, pagination, and search parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated collection of inference log summaries.</returns>
    /// <response code="200">Returns the paginated inference logs.</response>
    /// <response code="400">Invalid filter parameters.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedInferenceLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedInferenceLogResponse>> Search(
        [FromQuery] InferenceLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "GET /logs/inference/search - Page: {Page}, PageSize: {PageSize}, Statuses: {Statuses}",
            filter.Page,
            filter.PageSize,
            filter.Statuses != null ? string.Join(",", filter.Statuses) : "all");

        var result = await _inferenceLogService.SearchAsync(filter, cancellationToken);

        return Ok(result);
    }
}

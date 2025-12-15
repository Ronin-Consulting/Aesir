using Aesir.Common.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Logging.Controllers;

/// <summary>
/// API controller for querying document operation logs.
/// Provides endpoints to retrieve document logs for file uploads, embeddings, and deletions.
/// </summary>
[ApiController]
[Route("logs/document")]
[Produces("application/json")]
public class DocumentLogsController : ControllerBase
{
    private readonly IDocumentLogService _documentLogService;
    private readonly ILogger<DocumentLogsController> _logger;

    public DocumentLogsController(
        IDocumentLogService documentLogService,
        ILogger<DocumentLogsController> logger)
    {
        _documentLogService = documentLogService ?? throw new ArgumentNullException(nameof(documentLogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves a single document log by its ID with full details.
    /// </summary>
    /// <param name="id">The document log identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document log.</returns>
    /// <response code="200">Returns the document log.</response>
    /// <response code="404">Document log not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AesirDocumentLog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AesirDocumentLog>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET /logs/document/{Id}", id);

        var log = await _documentLogService.GetByIdAsync(id, cancellationToken);

        if (log == null)
        {
            return NotFound();
        }

        return Ok(log);
    }

    /// <summary>
    /// Retrieves all document logs for a specific conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of document log summaries for the conversation.</returns>
    [HttpGet("conversation/{conversationId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AesirDocumentLogSummary>), StatusCodes.Status200OK)]
    public async Task<IEnumerable<AesirDocumentLogSummary>> GetByConversation(
        [FromRoute] Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GET /logs/document/conversation/{ConversationId}", conversationId);

        return await _documentLogService.GetByConversationAsync(conversationId, cancellationToken);
    }

    /// <summary>
    /// Searches document logs with filtering, pagination, and text search.
    /// </summary>
    /// <param name="filter">Filter, pagination, and search parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated collection of document log summaries.</returns>
    /// <response code="200">Returns the paginated document logs.</response>
    /// <response code="400">Invalid filter parameters.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedDocumentLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedDocumentLogResponse>> Search(
        [FromQuery] DocumentLogFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "GET /logs/document/search - Page: {Page}, PageSize: {PageSize}, Operations: {Operations}",
            filter.Page,
            filter.PageSize,
            filter.OperationTypes != null ? string.Join(",", filter.OperationTypes) : "all");

        var result = await _documentLogService.SearchAsync(filter, cancellationToken);

        return Ok(result);
    }
}

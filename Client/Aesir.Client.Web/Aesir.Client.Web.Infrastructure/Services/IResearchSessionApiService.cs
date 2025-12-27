using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service interface for Research Session API operations.
/// Provides typed methods for managing research sessions.
/// </summary>
public interface IResearchSessionApiService
{
    /// <summary>
    /// Gets all research sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID (defaults to "default").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchSessionListBase>> GetSessionsAsync(string userId = "default", CancellationToken ct = default);

    /// <summary>
    /// Gets a research session by ID.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchSessionBase>> GetSessionAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the full report for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchReportBase>> GetReportAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the report as markdown.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<string>> GetReportMarkdownAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Starts a new research session.
    /// </summary>
    /// <param name="request">The create session request.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchSessionBase>> StartResearchAsync(CreateResearchSessionRequestBase request, CancellationToken ct = default);

    /// <summary>
    /// Submits clarification answers and continues research.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="answers">The answers to clarification questions.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchSessionBase>> SubmitClarificationAsync(Guid sessionId, Dictionary<string, string> answers, CancellationToken ct = default);

    /// <summary>
    /// Cancels an in-progress research session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult> CancelResearchAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a research session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);
}

/// <summary>
/// Request to create a new research session.
/// </summary>
public class CreateResearchSessionRequestBase
{
    /// <summary>
    /// The research query to investigate.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the research team to use.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The research mode.
    /// </summary>
    public ResearchModeBase Mode { get; set; } = ResearchModeBase.Standard;

    /// <summary>
    /// Optional document collection IDs for RAG.
    /// </summary>
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// The user ID creating the session.
    /// </summary>
    public string UserId { get; set; } = "default";
}

/// <summary>
/// List response for research sessions.
/// </summary>
public class ResearchSessionListBase
{
    /// <summary>
    /// The research sessions.
    /// </summary>
    public List<ResearchSessionBase> Sessions { get; set; } = new();

    /// <summary>
    /// Total count of sessions.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Full research report model.
/// </summary>
public class ResearchReportBase
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ExecutiveSummary { get; set; }
    public string? MethodologySection { get; set; }
    public string? AlternativePerspectives { get; set; }
    public string? ResearchGaps { get; set; }
    public string? FullMarkdown { get; set; }
    public List<ResearchFindingBase>? Findings { get; set; }
    public List<ResearchSourceBase>? Bibliography { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Research finding in a report.
/// </summary>
public class ResearchFindingBase
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Confidence { get; set; } = "Medium";
    public List<string>? SupportingEvidence { get; set; }
}

/// <summary>
/// Research source citation.
/// </summary>
public class ResearchSourceBase
{
    public string Title { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Author { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string? Snippet { get; set; }
}

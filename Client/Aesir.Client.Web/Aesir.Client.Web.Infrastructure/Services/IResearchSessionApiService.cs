using System.Text.Json.Serialization;
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
    /// Gets all research sessions for a specific conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<ResearchSessionListBase>> GetSessionsByConversationAsync(Guid conversationId, CancellationToken ct = default);

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

    /// <summary>
    /// Exports the research report as a PDF file.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<FileDownloadResult> ExportReportPdfAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Exports the research report as a Word document.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<FileDownloadResult> ExportReportWordAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the available export formats.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task<ApiResult<List<ExportFormatInfo>>> GetExportFormatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Request to create a new research session.
/// </summary>
public class CreateResearchSessionRequestBase
{
    /// <summary>
    /// The research query to investigate.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the research team to use.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid TeamId { get; set; }

    /// <summary>
    /// The research mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public ResearchModeBase Mode { get; set; } = ResearchModeBase.Standard;

    /// <summary>
    /// Optional document collection IDs for RAG.
    /// </summary>
    [JsonPropertyName("document_collection_ids")]
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// The user ID creating the session.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = "default";

    /// <summary>
    /// The ChatSession ID to link this research session to.
    /// If provided, research will be linked to an existing ChatSession.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }
}

/// <summary>
/// List response for research sessions.
/// </summary>
public class ResearchSessionListBase
{
    /// <summary>
    /// The research sessions.
    /// </summary>
    [JsonPropertyName("sessions")]
    public List<ResearchSessionBase> Sessions { get; set; } = new();

    /// <summary>
    /// Total count of sessions.
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

/// <summary>
/// Full research report model.
/// </summary>
public class ResearchReportBase
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("executive_summary")]
    public string? ExecutiveSummary { get; set; }

    [JsonPropertyName("methodology_section")]
    public string? MethodologySection { get; set; }

    [JsonPropertyName("alternative_perspectives")]
    public string? AlternativePerspectives { get; set; }

    [JsonPropertyName("research_gaps")]
    public string? ResearchGaps { get; set; }

    [JsonPropertyName("full_markdown")]
    public string? FullMarkdown { get; set; }

    [JsonPropertyName("findings")]
    public List<ResearchFindingBase>? Findings { get; set; }

    [JsonPropertyName("bibliography")]
    public List<ResearchSourceBase>? Bibliography { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Research finding in a report.
/// </summary>
public class ResearchFindingBase
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "Medium";

    [JsonPropertyName("supporting_evidence")]
    public List<string>? SupportingEvidence { get; set; }
}

/// <summary>
/// Research source citation.
/// </summary>
public class ResearchSourceBase
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("published_date")]
    public DateTime? PublishedDate { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
}

/// <summary>
/// Information about an available export format.
/// </summary>
public class ExportFormatInfo
{
    /// <summary>
    /// The format name (e.g., "Pdf", "Word").
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// The MIME content type for this format.
    /// </summary>
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The file extension for this format (e.g., ".pdf", ".docx").
    /// </summary>
    [JsonPropertyName("extension")]
    public string Extension { get; set; } = string.Empty;
}

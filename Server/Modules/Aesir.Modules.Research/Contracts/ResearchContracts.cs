using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Contracts;

/// <summary>
/// Request to create a new research session.
/// </summary>
public class CreateResearchSessionRequest
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
    /// The research mode (Quick, Standard, or Deep).
    /// </summary>
    public ResearchMode Mode { get; set; } = ResearchMode.Standard;

    /// <summary>
    /// Optional document collection IDs for RAG.
    /// </summary>
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// The user ID creating the session.
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// The ChatSession ID to link this research session to.
    /// If provided, research will be linked to an existing ChatSession.
    /// </summary>
    public Guid? ConversationId { get; set; }
}

/// <summary>
/// Request to submit clarification answers.
/// </summary>
public class SubmitClarificationRequest
{
    /// <summary>
    /// The answers to clarification questions.
    /// Key is the question ID, value is the answer.
    /// </summary>
    public Dictionary<string, string> Answers { get; set; } = new();
}

/// <summary>
/// Response for research session operations.
/// </summary>
public class ResearchSessionResponse
{
    /// <summary>
    /// The session ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The research team ID.
    /// </summary>
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// The user who created the session.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The original research query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The refined query after clarification.
    /// </summary>
    public string? RefinedQuery { get; set; }

    /// <summary>
    /// The research mode.
    /// </summary>
    public ResearchMode Mode { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    public ResearchStatus Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    public ResearchPhase? CurrentPhase { get; set; }

    /// <summary>
    /// Clarification questions (if awaiting clarification).
    /// </summary>
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the session completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The generated report (if completed).
    /// </summary>
    public ResearchReportSummary? Report { get; set; }

    /// <summary>
    /// Creates a response from a session entity.
    /// </summary>
    public static ResearchSessionResponse FromSession(ResearchSession session)
    {
        return new ResearchSessionResponse
        {
            Id = session.Id,
            ResearchTeamId = session.ResearchTeamId,
            UserId = session.UserId,
            Query = session.Query,
            RefinedQuery = session.RefinedQuery,
            Mode = session.Mode,
            Status = session.Status,
            CurrentPhase = session.CurrentPhase,
            ClarificationQuestions = session.ClarificationQuestions,
            ErrorMessage = session.ErrorMessage,
            CreatedAt = session.CreatedAt,
            StartedAt = session.StartedAt,
            CompletedAt = session.CompletedAt,
            Report = session.Report != null ? ResearchReportSummary.FromReport(session.Report) : null
        };
    }
}

/// <summary>
/// Summary view of a research report.
/// </summary>
public class ResearchReportSummary
{
    /// <summary>
    /// The report ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The executive summary.
    /// </summary>
    public string? ExecutiveSummary { get; set; }

    /// <summary>
    /// Number of findings in the report.
    /// </summary>
    public int FindingCount { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// When the report was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a summary from a full report.
    /// </summary>
    public static ResearchReportSummary FromReport(ResearchReport report)
    {
        return new ResearchReportSummary
        {
            Id = report.Id,
            Title = report.Title,
            ExecutiveSummary = report.ExecutiveSummary,
            FindingCount = report.Findings?.Count ?? 0,
            SourceCount = report.Bibliography?.Count ?? 0,
            CreatedAt = report.CreatedAt
        };
    }
}

/// <summary>
/// Progress update for SignalR broadcast.
/// </summary>
public class ResearchProgressUpdate
{
    /// <summary>
    /// The session ID.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    public ResearchStatus Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    public ResearchPhase Phase { get; set; }

    /// <summary>
    /// Progress description.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// The agent role performing the work (if applicable).
    /// </summary>
    public ResearchRole? AgentRole { get; set; }

    /// <summary>
    /// When this update was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// List response with pagination info.
/// </summary>
public class ResearchSessionListResponse
{
    /// <summary>
    /// The research sessions.
    /// </summary>
    public List<ResearchSessionResponse> Sessions { get; set; } = new();

    /// <summary>
    /// Total count of sessions.
    /// </summary>
    public int TotalCount { get; set; }
}

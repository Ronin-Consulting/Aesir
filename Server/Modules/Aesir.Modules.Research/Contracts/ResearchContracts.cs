using System.Text.Json.Serialization;
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
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the research team to use.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid TeamId { get; set; }

    /// <summary>
    /// The research mode (Quick, Standard, or Deep).
    /// </summary>
    [JsonPropertyName("mode")]
    public ResearchMode Mode { get; set; } = ResearchMode.Standard;

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
/// Request to submit clarification answers.
/// </summary>
public class SubmitClarificationRequest
{
    /// <summary>
    /// The answers to clarification questions.
    /// Key is the question ID, value is the answer.
    /// </summary>
    [JsonPropertyName("answers")]
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
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research team ID.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// The user who created the session.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The original research query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The refined query after clarification.
    /// </summary>
    [JsonPropertyName("refined_query")]
    public string? RefinedQuery { get; set; }

    /// <summary>
    /// The research mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public ResearchMode Mode { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResearchStatus Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    [JsonPropertyName("current_phase")]
    public ResearchPhase? CurrentPhase { get; set; }

    /// <summary>
    /// Clarification questions (if awaiting clarification).
    /// </summary>
    [JsonPropertyName("clarification_questions")]
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session started processing.
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the session completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// The generated report (if completed).
    /// </summary>
    [JsonPropertyName("report")]
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
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The report title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The executive summary.
    /// </summary>
    [JsonPropertyName("executive_summary")]
    public string? ExecutiveSummary { get; set; }

    /// <summary>
    /// Number of findings in the report.
    /// </summary>
    [JsonPropertyName("finding_count")]
    public int FindingCount { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    [JsonPropertyName("source_count")]
    public int SourceCount { get; set; }

    /// <summary>
    /// When the report was created.
    /// </summary>
    [JsonPropertyName("created_at")]
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
/// Unified progress event with multi-agent tracking.
/// </summary>
public class ResearchProgressUpdate
{
    /// <summary>
    /// The session ID.
    /// </summary>
    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResearchStatus Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    [JsonPropertyName("phase")]
    public ResearchPhase Phase { get; set; }

    /// <summary>
    /// Progress description.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progress_percent")]
    public int ProgressPercent { get; set; }

    /// <summary>
    /// The agent role performing the work (if applicable).
    /// Deprecated: Use ActiveAgents instead for multi-agent support.
    /// </summary>
    [JsonPropertyName("agent_role")]
    public ResearchRole? AgentRole { get; set; }

    /// <summary>
    /// List of currently active agents with their activities.
    /// Empty when no agents are actively working (e.g., phase transitions).
    /// </summary>
    [JsonPropertyName("active_agents")]
    public List<ActiveAgentInfo> ActiveAgents { get; set; } = new();

    /// <summary>
    /// When this update was created.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Information about an active agent during research.
/// </summary>
public class ActiveAgentInfo
{
    /// <summary>
    /// The team member ID (unique per session).
    /// </summary>
    [JsonPropertyName("team_member_id")]
    public Guid TeamMemberId { get; set; }

    /// <summary>
    /// The agent's research role.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Display name for the role.
    /// </summary>
    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// What the agent is currently doing.
    /// </summary>
    [JsonPropertyName("activity")]
    public string Activity { get; set; } = string.Empty;

    /// <summary>
    /// Agent-specific progress within the current activity (0-100).
    /// Null if progress not applicable (e.g., waiting for LLM response).
    /// </summary>
    [JsonPropertyName("agent_progress_percent")]
    public int? AgentProgressPercent { get; set; }
}

/// <summary>
/// List response with pagination info.
/// </summary>
public class ResearchSessionListResponse
{
    /// <summary>
    /// The research sessions.
    /// </summary>
    [JsonPropertyName("sessions")]
    public List<ResearchSessionResponse> Sessions { get; set; } = new();

    /// <summary>
    /// Total count of sessions.
    /// </summary>
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
}

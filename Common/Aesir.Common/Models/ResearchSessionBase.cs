using System.Text.Json.Serialization;

namespace Aesir.Common.Models;

/// <summary>
/// Base model for a research session.
/// </summary>
public class ResearchSessionBase
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research team ID.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// The conversation/chat session ID this research is linked to.
    /// </summary>
    [JsonPropertyName("conversation_id")]
    public Guid? ConversationId { get; set; }

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
    public ResearchModeBase Mode { get; set; }

    /// <summary>
    /// The current status.
    /// </summary>
    [JsonPropertyName("status")]
    public ResearchStatusBase Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    [JsonPropertyName("current_phase")]
    public ResearchPhaseBase? CurrentPhase { get; set; }

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
    public ResearchReportSummaryBase? Report { get; set; }
}

/// <summary>
/// Summary view of a research report.
/// </summary>
public class ResearchReportSummaryBase
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
}

/// <summary>
/// Progress update for research sessions.
/// </summary>
public class ResearchProgressBase
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
    public ResearchStatusBase Status { get; set; }

    /// <summary>
    /// The current phase.
    /// </summary>
    [JsonPropertyName("phase")]
    public ResearchPhaseBase Phase { get; set; }

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
    /// </summary>
    [JsonPropertyName("agent_role")]
    public ResearchRoleBase? AgentRole { get; set; }

    /// <summary>
    /// When this update was created.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Status of a research session.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchStatusBase
{
    Created = 0,
    AwaitingClarification = 1,
    Planning = 2,
    Researching = 3,
    Anonymizing = 4,
    PeerReviewing = 5,
    Synthesizing = 6,
    Completed = 7,
    Failed = 8,
    Cancelled = 9
}

/// <summary>
/// Current phase of research.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResearchPhaseBase
{
    Clarification = 0,
    Planning = 1,
    Research = 2,
    Anonymization = 3,
    PeerReview = 4,
    Synthesis = 5
}

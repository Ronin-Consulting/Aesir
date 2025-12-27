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
    [JsonPropertyName("researchTeamId")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// The user who created the session.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The original research query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// The refined query after clarification.
    /// </summary>
    [JsonPropertyName("refinedQuery")]
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
    [JsonPropertyName("currentPhase")]
    public ResearchPhaseBase? CurrentPhase { get; set; }

    /// <summary>
    /// Clarification questions (if awaiting clarification).
    /// </summary>
    [JsonPropertyName("clarificationQuestions")]
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session started processing.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the session completed.
    /// </summary>
    [JsonPropertyName("completedAt")]
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
    [JsonPropertyName("executiveSummary")]
    public string? ExecutiveSummary { get; set; }

    /// <summary>
    /// Number of findings in the report.
    /// </summary>
    [JsonPropertyName("findingCount")]
    public int FindingCount { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    [JsonPropertyName("sourceCount")]
    public int SourceCount { get; set; }

    /// <summary>
    /// When the report was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
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
    [JsonPropertyName("sessionId")]
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
    [JsonPropertyName("progressPercent")]
    public int ProgressPercent { get; set; }

    /// <summary>
    /// The agent role performing the work (if applicable).
    /// </summary>
    [JsonPropertyName("agentRole")]
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

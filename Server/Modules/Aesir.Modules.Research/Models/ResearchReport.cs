using System.Text.Json.Serialization;
using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents the final synthesized research report produced by the Chairman.
/// </summary>
public class ResearchReport : IEntity
{
    /// <summary>
    /// Unique identifier for the report.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this report belongs to.
    /// </summary>
    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// Report title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Executive summary section.
    /// </summary>
    [JsonPropertyName("executive_summary")]
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// Methodology description section.
    /// </summary>
    [JsonPropertyName("methodology_section")]
    public string MethodologySection { get; set; } = string.Empty;

    /// <summary>
    /// Key findings with confidence levels.
    /// </summary>
    [JsonPropertyName("findings")]
    public List<ResearchFinding>? Findings { get; set; }

    /// <summary>
    /// Alternative perspectives and dissenting views.
    /// </summary>
    [JsonPropertyName("alternative_perspectives")]
    public string? AlternativePerspectives { get; set; }

    /// <summary>
    /// Identified research gaps for future investigation.
    /// </summary>
    [JsonPropertyName("research_gaps")]
    public string? ResearchGaps { get; set; }

    /// <summary>
    /// Bibliography of all sources used.
    /// </summary>
    [JsonPropertyName("bibliography")]
    public List<ResearchSource>? Bibliography { get; set; }

    /// <summary>
    /// Complete markdown report for display/export.
    /// </summary>
    [JsonPropertyName("full_markdown")]
    public string FullMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Report metadata (duration, token counts, agent stats).
    /// </summary>
    [JsonPropertyName("metadata")]
    public ReportMetadata? Metadata { get; set; }

    /// <summary>
    /// Total token usage for report generation.
    /// </summary>
    [JsonPropertyName("tokens_used")]
    public int? TokensUsed { get; set; }

    /// <summary>
    /// When the report was generated.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// A key finding from the research with confidence level.
/// </summary>
public class ResearchFinding
{
    /// <summary>
    /// Finding title.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Finding description/content.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level based on peer review scores and consensus.
    /// </summary>
    [JsonPropertyName("confidence")]
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Supporting evidence for this finding.
    /// </summary>
    [JsonPropertyName("supporting_evidence")]
    public List<string>? SupportingEvidence { get; set; }

    /// <summary>
    /// Sources supporting this finding.
    /// </summary>
    [JsonPropertyName("sources")]
    public List<ResearchSource>? Sources { get; set; }

    /// <summary>
    /// Which agent roles contributed to this finding.
    /// </summary>
    [JsonPropertyName("contributing_roles")]
    public List<ResearchRole>? ContributingRoles { get; set; }
}

/// <summary>
/// Metadata about the research report.
/// </summary>
public class ReportMetadata
{
    /// <summary>
    /// Total research duration in milliseconds.
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Total tokens used across all phases.
    /// </summary>
    [JsonPropertyName("total_tokens_used")]
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    [JsonPropertyName("source_count")]
    public int SourceCount { get; set; }

    /// <summary>
    /// Number of findings generated.
    /// </summary>
    [JsonPropertyName("finding_count")]
    public int FindingCount { get; set; }

    /// <summary>
    /// Average peer review score.
    /// </summary>
    [JsonPropertyName("average_peer_review_score")]
    public double AveragePeerReviewScore { get; set; }

    /// <summary>
    /// Per-agent statistics.
    /// </summary>
    [JsonPropertyName("agent_statistics")]
    public Dictionary<string, AgentStats>? AgentStatistics { get; set; }
}

/// <summary>
/// Statistics for an individual agent's contribution.
/// </summary>
public class AgentStats
{
    /// <summary>
    /// Agent's role in the research.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Tokens used by this agent.
    /// </summary>
    [JsonPropertyName("tokens_used")]
    public int TokensUsed { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    [JsonPropertyName("duration_ms")]
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    [JsonPropertyName("sources_cited")]
    public int SourcesCited { get; set; }

    /// <summary>
    /// Number of tool calls made.
    /// </summary>
    [JsonPropertyName("tool_calls_made")]
    public int ToolCallsMade { get; set; }

    /// <summary>
    /// Average peer review score received.
    /// </summary>
    [JsonPropertyName("average_review_score")]
    public double AverageReviewScore { get; set; }
}

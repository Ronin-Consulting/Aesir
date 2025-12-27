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
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this report belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Report title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Executive summary section.
    /// </summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>
    /// Methodology description section.
    /// </summary>
    public string MethodologySection { get; set; } = string.Empty;

    /// <summary>
    /// Key findings with confidence levels.
    /// </summary>
    public List<ResearchFinding>? Findings { get; set; }

    /// <summary>
    /// Alternative perspectives and dissenting views.
    /// </summary>
    public string? AlternativePerspectives { get; set; }

    /// <summary>
    /// Identified research gaps for future investigation.
    /// </summary>
    public string? ResearchGaps { get; set; }

    /// <summary>
    /// Bibliography of all sources used.
    /// </summary>
    public List<ResearchSource>? Bibliography { get; set; }

    /// <summary>
    /// Complete markdown report for display/export.
    /// </summary>
    public string FullMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// Report metadata (duration, token counts, agent stats).
    /// </summary>
    public ReportMetadata? Metadata { get; set; }

    /// <summary>
    /// Total token usage for report generation.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// When the report was generated.
    /// </summary>
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
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Finding description/content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level based on peer review scores and consensus.
    /// </summary>
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Supporting evidence for this finding.
    /// </summary>
    public List<string>? SupportingEvidence { get; set; }

    /// <summary>
    /// Sources supporting this finding.
    /// </summary>
    public List<ResearchSource>? Sources { get; set; }

    /// <summary>
    /// Which agent roles contributed to this finding.
    /// </summary>
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
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Total tokens used across all phases.
    /// </summary>
    public int TotalTokensUsed { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    public int SourceCount { get; set; }

    /// <summary>
    /// Number of findings generated.
    /// </summary>
    public int FindingCount { get; set; }

    /// <summary>
    /// Average peer review score.
    /// </summary>
    public double AveragePeerReviewScore { get; set; }

    /// <summary>
    /// Per-agent statistics.
    /// </summary>
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
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Tokens used by this agent.
    /// </summary>
    public int TokensUsed { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of sources cited.
    /// </summary>
    public int SourcesCited { get; set; }

    /// <summary>
    /// Number of tool calls made.
    /// </summary>
    public int ToolCallsMade { get; set; }

    /// <summary>
    /// Average peer review score received.
    /// </summary>
    public double AverageReviewScore { get; set; }
}

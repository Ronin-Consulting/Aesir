using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents an agent's research submission - their work product for a research session.
/// </summary>
public class ResearchSubmission : IEntity
{
    /// <summary>
    /// Unique identifier for the submission.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this submission belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The agent that produced this submission.
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// The role the agent was playing.
    /// </summary>
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Round number (for multi-round Deep mode).
    /// </summary>
    public int RoundNumber { get; set; } = 1;

    /// <summary>
    /// Anonymized identifier for peer review (A, B, C).
    /// </summary>
    public string? AnonymizedId { get; set; }

    /// <summary>
    /// The agent's chain-of-thought research plan.
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// The agent's research findings content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Extended thinking trace (if enabled).
    /// </summary>
    public string? ThinkingTrace { get; set; }

    /// <summary>
    /// Sources cited in the research.
    /// </summary>
    public List<ResearchSource>? Sources { get; set; }

    /// <summary>
    /// Tool calls made during research.
    /// </summary>
    public List<ResearchToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Token usage for this submission.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Status of this submission.
    /// </summary>
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    /// <summary>
    /// Error message if submission failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the submission was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the submission was completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// A source citation from research.
/// </summary>
public class ResearchSource
{
    /// <summary>
    /// Title of the source.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL or file path of the source.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Type of source (document, web, etc.).
    /// </summary>
    public string? SourceType { get; set; }

    /// <summary>
    /// Relevant quote from the source.
    /// </summary>
    public string? Quote { get; set; }

    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    public double? RelevanceScore { get; set; }
}

/// <summary>
/// A tool call made during research.
/// </summary>
public class ResearchToolCall
{
    /// <summary>
    /// Name of the tool called.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Input provided to the tool.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Output returned by the tool.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// When the tool was called.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

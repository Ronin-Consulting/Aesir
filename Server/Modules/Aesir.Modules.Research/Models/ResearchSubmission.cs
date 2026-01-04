using System.Text.Json.Serialization;
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
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this submission belongs to.
    /// </summary>
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// The agent that produced this submission.
    /// </summary>
    [JsonPropertyName("agentId")]
    public Guid AgentId { get; set; }

    /// <summary>
    /// The role the agent was playing.
    /// </summary>
    [JsonPropertyName("role")]
    public ResearchRole Role { get; set; }

    /// <summary>
    /// Round number (for multi-round Deep mode).
    /// </summary>
    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; } = 1;

    /// <summary>
    /// Anonymized identifier for peer review (A, B, C).
    /// </summary>
    [JsonPropertyName("anonymizedId")]
    public string? AnonymizedId { get; set; }

    /// <summary>
    /// The agent's chain-of-thought research plan.
    /// </summary>
    [JsonPropertyName("plan")]
    public string? Plan { get; set; }

    /// <summary>
    /// The agent's research findings content.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Extended thinking trace (if enabled).
    /// </summary>
    [JsonPropertyName("thinkingTrace")]
    public string? ThinkingTrace { get; set; }

    /// <summary>
    /// Sources cited in the research.
    /// </summary>
    [JsonPropertyName("sources")]
    public List<ResearchSource>? Sources { get; set; }

    /// <summary>
    /// Tool calls made during research.
    /// </summary>
    [JsonPropertyName("toolCalls")]
    public List<ResearchToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Token usage for this submission.
    /// </summary>
    [JsonPropertyName("tokensUsed")]
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// Status of this submission.
    /// </summary>
    [JsonPropertyName("status")]
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    /// <summary>
    /// Error message if submission failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the submission was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the submission was completed.
    /// </summary>
    [JsonPropertyName("completedAt")]
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
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL or file path of the source.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Type of source (document, web, etc.).
    /// </summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>
    /// Relevant quote from the source.
    /// </summary>
    [JsonPropertyName("quote")]
    public string? Quote { get; set; }

    /// <summary>
    /// Relevance score (0-1).
    /// </summary>
    [JsonPropertyName("relevanceScore")]
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
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Input provided to the tool.
    /// </summary>
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    /// <summary>
    /// Output returned by the tool.
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    /// <summary>
    /// When the tool was called.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

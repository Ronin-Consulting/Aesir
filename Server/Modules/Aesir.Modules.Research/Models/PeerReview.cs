using System.Text.Json.Serialization;
using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents a peer review of a research submission.
/// Each non-author agent reviews other agents' anonymized submissions.
/// </summary>
public class PeerReview : IEntity
{
    /// <summary>
    /// Unique identifier for the peer review.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this review belongs to.
    /// </summary>
    [JsonPropertyName("session_id")]
    public Guid SessionId { get; set; }

    /// <summary>
    /// The submission being reviewed.
    /// </summary>
    [JsonPropertyName("submission_id")]
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// The agent performing the review.
    /// </summary>
    [JsonPropertyName("reviewer_agent_id")]
    public Guid ReviewerAgentId { get; set; }

    /// <summary>
    /// The role of the reviewing agent.
    /// </summary>
    [JsonPropertyName("reviewer_role")]
    public ResearchRole ReviewerRole { get; set; }

    /// <summary>
    /// Score for research depth (1-10).
    /// </summary>
    [JsonPropertyName("score_depth")]
    public double ScoreDepth { get; set; }

    /// <summary>
    /// Score for accuracy/correctness (1-10).
    /// </summary>
    [JsonPropertyName("score_accuracy")]
    public double ScoreAccuracy { get; set; }

    /// <summary>
    /// Score for source quality (1-10).
    /// </summary>
    [JsonPropertyName("score_source_quality")]
    public double ScoreSourceQuality { get; set; }

    /// <summary>
    /// Score for novelty/insight (1-10).
    /// </summary>
    [JsonPropertyName("score_novelty")]
    public double ScoreNovelty { get; set; }

    /// <summary>
    /// Score for coherence/clarity (1-10).
    /// </summary>
    [JsonPropertyName("score_coherence")]
    public double ScoreCoherence { get; set; }

    /// <summary>
    /// Calculated weighted average of all scores.
    /// </summary>
    [JsonPropertyName("weighted_average")]
    public double WeightedAverage { get; set; }

    /// <summary>
    /// Identified strengths of the submission.
    /// </summary>
    [JsonPropertyName("strengths")]
    public string? Strengths { get; set; }

    /// <summary>
    /// Suggested improvements.
    /// </summary>
    [JsonPropertyName("improvements")]
    public string? Improvements { get; set; }

    /// <summary>
    /// Detailed critique text.
    /// </summary>
    [JsonPropertyName("critique")]
    public string Critique { get; set; } = string.Empty;

    /// <summary>
    /// Whether the reviewer endorses this submission.
    /// </summary>
    [JsonPropertyName("endorses")]
    public bool Endorses { get; set; } = true;

    /// <summary>
    /// Token usage for this review.
    /// </summary>
    [JsonPropertyName("tokens_used")]
    public int? TokensUsed { get; set; }

    /// <summary>
    /// When the review was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

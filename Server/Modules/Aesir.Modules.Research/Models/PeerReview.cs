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
    public Guid Id { get; set; }

    /// <summary>
    /// The research session this review belongs to.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The submission being reviewed.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// The agent performing the review.
    /// </summary>
    public Guid ReviewerAgentId { get; set; }

    /// <summary>
    /// The role of the reviewing agent.
    /// </summary>
    public ResearchRole ReviewerRole { get; set; }

    /// <summary>
    /// Score for research depth (1-10).
    /// </summary>
    public double ScoreDepth { get; set; }

    /// <summary>
    /// Score for accuracy/correctness (1-10).
    /// </summary>
    public double ScoreAccuracy { get; set; }

    /// <summary>
    /// Score for source quality (1-10).
    /// </summary>
    public double ScoreSourceQuality { get; set; }

    /// <summary>
    /// Score for novelty/insight (1-10).
    /// </summary>
    public double ScoreNovelty { get; set; }

    /// <summary>
    /// Score for coherence/clarity (1-10).
    /// </summary>
    public double ScoreCoherence { get; set; }

    /// <summary>
    /// Calculated weighted average of all scores.
    /// </summary>
    public double WeightedAverage { get; set; }

    /// <summary>
    /// Identified strengths of the submission.
    /// </summary>
    public string? Strengths { get; set; }

    /// <summary>
    /// Suggested improvements.
    /// </summary>
    public string? Improvements { get; set; }

    /// <summary>
    /// Detailed critique text.
    /// </summary>
    public string Critique { get; set; } = string.Empty;

    /// <summary>
    /// Whether the reviewer endorses this submission.
    /// </summary>
    public bool Endorses { get; set; } = true;

    /// <summary>
    /// Token usage for this review.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// When the review was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

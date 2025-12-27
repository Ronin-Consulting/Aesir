using Aesir.Infrastructure.Data;

namespace Aesir.Modules.Research.Models;

/// <summary>
/// Represents a research session - an execution of research for a user query.
/// </summary>
public class ResearchSession : IEntity
{
    /// <summary>
    /// Unique identifier for the session.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who initiated the research.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the research team configuration used.
    /// </summary>
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// Reference to the chat conversation this research is part of.
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The original user query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Refined query after clarification (if applicable).
    /// </summary>
    public string? RefinedQuery { get; set; }

    /// <summary>
    /// Research mode (Quick, Standard, Deep).
    /// </summary>
    public ResearchMode Mode { get; set; } = ResearchMode.Standard;

    /// <summary>
    /// Current status of the research session.
    /// </summary>
    public ResearchStatus Status { get; set; } = ResearchStatus.Created;

    /// <summary>
    /// Current phase of the research workflow.
    /// </summary>
    public ResearchPhase? CurrentPhase { get; set; }

    /// <summary>
    /// Document collection IDs for RAG corpus (from conversation attachments).
    /// </summary>
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// Clarification questions generated for the user.
    /// </summary>
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// User's answers to clarification questions.
    /// </summary>
    public Dictionary<string, string>? ClarificationAnswers { get; set; }

    /// <summary>
    /// Error message if research failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When research execution started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When research completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property: the research team used.
    /// </summary>
    public ResearchTeam? ResearchTeam { get; set; }

    /// <summary>
    /// Navigation property: agent submissions.
    /// </summary>
    public List<ResearchSubmission>? Submissions { get; set; }

    /// <summary>
    /// Navigation property: peer reviews.
    /// </summary>
    public List<PeerReview>? PeerReviews { get; set; }

    /// <summary>
    /// Navigation property: the final report.
    /// </summary>
    public ResearchReport? Report { get; set; }
}

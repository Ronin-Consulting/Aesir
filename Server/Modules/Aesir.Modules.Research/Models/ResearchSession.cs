using System.Text.Json.Serialization;
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
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// User who initiated the research.
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the research team configuration used.
    /// </summary>
    [JsonPropertyName("researchTeamId")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// Reference to the chat conversation this research is part of.
    /// </summary>
    [JsonPropertyName("conversationId")]
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// The original user query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Refined query after clarification (if applicable).
    /// </summary>
    [JsonPropertyName("refinedQuery")]
    public string? RefinedQuery { get; set; }

    /// <summary>
    /// Research mode (Quick, Standard, Deep).
    /// </summary>
    [JsonPropertyName("mode")]
    public ResearchMode Mode { get; set; } = ResearchMode.Standard;

    /// <summary>
    /// Current status of the research session.
    /// </summary>
    [JsonPropertyName("status")]
    public ResearchStatus Status { get; set; } = ResearchStatus.Created;

    /// <summary>
    /// Current phase of the research workflow.
    /// </summary>
    [JsonPropertyName("currentPhase")]
    public ResearchPhase? CurrentPhase { get; set; }

    /// <summary>
    /// Document collection IDs for RAG corpus (from conversation attachments).
    /// </summary>
    [JsonPropertyName("documentCollectionIds")]
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// Clarification questions generated for the user.
    /// </summary>
    [JsonPropertyName("clarificationQuestions")]
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// User's answers to clarification questions.
    /// </summary>
    [JsonPropertyName("clarificationAnswers")]
    public Dictionary<string, string>? ClarificationAnswers { get; set; }

    /// <summary>
    /// Error message if research failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When research execution started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When research completed.
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property: the research team used.
    /// </summary>
    [JsonIgnore]
    public ResearchTeam? ResearchTeam { get; set; }

    /// <summary>
    /// Navigation property: agent submissions.
    /// </summary>
    [JsonPropertyName("submissions")]
    public List<ResearchSubmission>? Submissions { get; set; }

    /// <summary>
    /// Navigation property: peer reviews.
    /// </summary>
    [JsonPropertyName("peerReviews")]
    public List<PeerReview>? PeerReviews { get; set; }

    /// <summary>
    /// Navigation property: the final report.
    /// </summary>
    [JsonPropertyName("report")]
    public ResearchReport? Report { get; set; }
}

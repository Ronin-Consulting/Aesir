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
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the research team configuration used.
    /// </summary>
    [JsonPropertyName("research_team_id")]
    public Guid? ResearchTeamId { get; set; }

    /// <summary>
    /// Reference to the chat session (aesir_chat_session.id) this research is linked to.
    /// Enables research results to be persisted in chat history and allows
    /// users to continue conversations after research completes.
    /// </summary>
    [JsonPropertyName("chat_session_id")]
    public Guid? ChatSessionId { get; set; }

    /// <summary>
    /// The original user query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Refined query after clarification (if applicable).
    /// </summary>
    [JsonPropertyName("refined_query")]
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
    [JsonPropertyName("current_phase")]
    public ResearchPhase? CurrentPhase { get; set; }

    /// <summary>
    /// Document collection IDs for RAG corpus (from conversation attachments).
    /// </summary>
    [JsonPropertyName("document_collection_ids")]
    public List<Guid>? DocumentCollectionIds { get; set; }

    /// <summary>
    /// Clarification questions generated for the user.
    /// </summary>
    [JsonPropertyName("clarification_questions")]
    public List<string>? ClarificationQuestions { get; set; }

    /// <summary>
    /// User's answers to clarification questions.
    /// </summary>
    [JsonPropertyName("clarification_answers")]
    public Dictionary<string, string>? ClarificationAnswers { get; set; }

    /// <summary>
    /// Error message if research failed.
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the session was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When research execution started.
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When research completed.
    /// </summary>
    [JsonPropertyName("completed_at")]
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
    [JsonPropertyName("peer_reviews")]
    public List<PeerReview>? PeerReviews { get; set; }

    /// <summary>
    /// Navigation property: the final report.
    /// </summary>
    [JsonPropertyName("report")]
    public ResearchReport? Report { get; set; }
}

using Aesir.Modules.Research.Models;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Repository interface for managing research sessions.
/// </summary>
public interface IResearchSessionRepository
{
    /// <summary>
    /// Gets a research session by its unique identifier, including related data.
    /// </summary>
    /// <param name="id">The unique identifier of the research session.</param>
    /// <returns>The research session if found, null otherwise.</returns>
    Task<ResearchSession?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all research sessions for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A collection of research sessions for the user.</returns>
    Task<IEnumerable<ResearchSession>> GetByUserIdAsync(string userId);

    /// <summary>
    /// Gets research sessions by conversation ID.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>A collection of research sessions for the conversation.</returns>
    Task<IEnumerable<ResearchSession>> GetByConversationIdAsync(Guid conversationId);

    /// <summary>
    /// Creates a new research session.
    /// </summary>
    /// <param name="session">The research session to create.</param>
    /// <returns>The created research session with its generated ID.</returns>
    Task<ResearchSession> AddAsync(ResearchSession session);

    /// <summary>
    /// Updates an existing research session.
    /// </summary>
    /// <param name="session">The research session to update.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateAsync(ResearchSession session);

    /// <summary>
    /// Updates the status and phase of a research session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="status">The new status.</param>
    /// <param name="phase">The current phase (optional).</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateStatusAsync(Guid id, ResearchStatus status, ResearchPhase? phase = null);

    /// <summary>
    /// Deletes a research session and all related data (cascaded).
    /// </summary>
    /// <param name="id">The unique identifier of the research session to delete.</param>
    /// <returns>True if the deletion was successful, false otherwise.</returns>
    Task<bool> RemoveAsync(Guid id);

    /// <summary>
    /// Adds a submission to a research session.
    /// </summary>
    /// <param name="submission">The submission to add.</param>
    /// <returns>The created submission with its generated ID.</returns>
    Task<ResearchSubmission> AddSubmissionAsync(ResearchSubmission submission);

    /// <summary>
    /// Updates a research submission.
    /// </summary>
    /// <param name="submission">The submission to update.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    Task<bool> UpdateSubmissionAsync(ResearchSubmission submission);

    /// <summary>
    /// Gets all submissions for a research session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A collection of submissions for the session.</returns>
    Task<IEnumerable<ResearchSubmission>> GetSubmissionsBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Adds a peer review to a submission.
    /// </summary>
    /// <param name="review">The peer review to add.</param>
    /// <returns>The created peer review with its generated ID.</returns>
    Task<PeerReview> AddPeerReviewAsync(PeerReview review);

    /// <summary>
    /// Gets all peer reviews for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A collection of peer reviews for the session.</returns>
    Task<IEnumerable<PeerReview>> GetPeerReviewsBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Adds or updates the research report for a session.
    /// </summary>
    /// <param name="report">The research report to add or update.</param>
    /// <returns>The created or updated research report.</returns>
    Task<ResearchReport> UpsertReportAsync(ResearchReport report);

    /// <summary>
    /// Gets the research report for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>The research report if found, null otherwise.</returns>
    Task<ResearchReport?> GetReportBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Adds a trail entry to the audit log.
    /// </summary>
    /// <param name="entry">The trail entry to add.</param>
    /// <returns>The created trail entry with its generated ID.</returns>
    Task<ResearchTrailEntry> AddTrailEntryAsync(ResearchTrailEntry entry);

    /// <summary>
    /// Gets all trail entries for a session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <returns>A collection of trail entries for the session, ordered by timestamp.</returns>
    Task<IEnumerable<ResearchTrailEntry>> GetTrailEntriesBySessionIdAsync(Guid sessionId);
}

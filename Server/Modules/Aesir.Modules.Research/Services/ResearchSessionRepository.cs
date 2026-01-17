using Aesir.Infrastructure.Data;
using Aesir.Modules.Research.Models;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Repository implementation for managing research sessions and related data.
/// </summary>
public class ResearchSessionRepository(
    ILogger<ResearchSessionRepository> logger,
    IDbContext dbContext) : IResearchSessionRepository
{
    static ResearchSessionRepository()
    {
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<Guid>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<string>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<Dictionary<string, string>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<ResearchSource>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<ResearchToolCall>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<List<ResearchFinding>?>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<ReportMetadata?>());
    }

    /// <inheritdoc />
    public async Task<ResearchSession?> GetByIdAsync(Guid id)
    {
        const string sessionSql = @"
            SELECT
                id as Id,
                user_id as UserId,
                research_team_id as ResearchTeamId,
                chat_session_id as ChatSessionId,
                query as Query,
                refined_query as RefinedQuery,
                mode as Mode,
                status as Status,
                current_phase as CurrentPhase,
                document_collection_ids as DocumentCollectionIds,
                clarification_questions as ClarificationQuestions,
                clarification_answers as ClarificationAnswers,
                error_message as ErrorMessage,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                started_at as StartedAt,
                completed_at as CompletedAt
            FROM aesir.aesir_research_session
            WHERE id = @Id";

        var session = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<ResearchSession>(sessionSql, new { Id = id }).ConfigureAwait(false)).ConfigureAwait(false);

        // Also load the report if session exists and is completed
        if (session != null && session.Status == ResearchStatus.Completed)
        {
            session.Report = await GetReportBySessionIdAsync(id).ConfigureAwait(false);
        }

        return session;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchSession>> GetByUserIdAsync(string userId)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                research_team_id as ResearchTeamId,
                chat_session_id as ChatSessionId,
                query as Query,
                refined_query as RefinedQuery,
                mode as Mode,
                status as Status,
                current_phase as CurrentPhase,
                document_collection_ids as DocumentCollectionIds,
                clarification_questions as ClarificationQuestions,
                clarification_answers as ClarificationAnswers,
                error_message as ErrorMessage,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                started_at as StartedAt,
                completed_at as CompletedAt
            FROM aesir.aesir_research_session
            WHERE user_id = @UserId
            ORDER BY created_at DESC";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<ResearchSession>(sql, new { UserId = userId }).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchSession>> GetByChatSessionIdAsync(Guid chatSessionId)
    {
        const string sql = @"
            SELECT
                id as Id,
                user_id as UserId,
                research_team_id as ResearchTeamId,
                chat_session_id as ChatSessionId,
                query as Query,
                refined_query as RefinedQuery,
                mode as Mode,
                status as Status,
                current_phase as CurrentPhase,
                document_collection_ids as DocumentCollectionIds,
                clarification_questions as ClarificationQuestions,
                clarification_answers as ClarificationAnswers,
                error_message as ErrorMessage,
                created_at as CreatedAt,
                updated_at as UpdatedAt,
                started_at as StartedAt,
                completed_at as CompletedAt
            FROM aesir.aesir_research_session
            WHERE chat_session_id = @ChatSessionId
            ORDER BY created_at DESC";

        var sessions = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<ResearchSession>(sql, new { ChatSessionId = chatSessionId }).ConfigureAwait(false)).ConfigureAwait(false);

        // Load reports for completed sessions
        var sessionList = sessions.ToList();
        foreach (var session in sessionList.Where(s => s.Status == ResearchStatus.Completed))
        {
            session.Report = await GetReportBySessionIdAsync(session.Id).ConfigureAwait(false);
        }

        return sessionList;
    }

    /// <inheritdoc />
    public async Task<ResearchSession> AddAsync(ResearchSession session)
    {
        if (session.Id == Guid.Empty)
            session.Id = Guid.NewGuid();

        var now = DateTime.UtcNow;
        session.CreatedAt = now;
        session.UpdatedAt = now;

        const string sql = @"
            INSERT INTO aesir.aesir_research_session
                (id, user_id, research_team_id, chat_session_id, query, refined_query,
                 mode, status, current_phase, document_collection_ids,
                 clarification_questions, clarification_answers, error_message,
                 created_at, updated_at, started_at, completed_at)
            VALUES
                (@Id, @UserId, @ResearchTeamId, @ChatSessionId, @Query, @RefinedQuery,
                 @Mode, @Status, @CurrentPhase, @DocumentCollectionIds::jsonb,
                 @ClarificationQuestions::jsonb, @ClarificationAnswers::jsonb, @ErrorMessage,
                 @CreatedAt, @UpdatedAt, @StartedAt, @CompletedAt)";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, session).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        logger.LogInformation("Created research session {SessionId} for query: {Query}",
            session.Id, session.Query.Length > 50 ? session.Query[..50] + "..." : session.Query);

        return session;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ResearchSession session)
    {
        session.UpdatedAt = DateTime.UtcNow;

        const string sql = @"
            UPDATE aesir.aesir_research_session
            SET user_id = @UserId,
                research_team_id = @ResearchTeamId,
                chat_session_id = @ChatSessionId,
                query = @Query,
                refined_query = @RefinedQuery,
                mode = @Mode,
                status = @Status,
                current_phase = @CurrentPhase,
                document_collection_ids = @DocumentCollectionIds::jsonb,
                clarification_questions = @ClarificationQuestions::jsonb,
                clarification_answers = @ClarificationAnswers::jsonb,
                error_message = @ErrorMessage,
                updated_at = @UpdatedAt,
                started_at = @StartedAt,
                completed_at = @CompletedAt
            WHERE id = @Id";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, session).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(Guid id, ResearchStatus status, ResearchPhase? phase = null)
    {
        const string sql = @"
            UPDATE aesir.aesir_research_session
            SET status = @Status,
                current_phase = @Phase,
                updated_at = @UpdatedAt,
                started_at = CASE WHEN @Status = 1 AND started_at IS NULL THEN @UpdatedAt ELSE started_at END,
                completed_at = CASE WHEN @Status IN (3, 4) THEN @UpdatedAt ELSE completed_at END
            WHERE id = @Id";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                Status = status,
                Phase = phase,
                UpdatedAt = DateTime.UtcNow
            }).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(Guid id)
    {
        // Related data is deleted via cascade
        const string sql = @"
            DELETE FROM aesir.aesir_research_session
            WHERE id = @Id";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, new { Id = id }).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        logger.LogInformation("Deleted research session {SessionId}", id);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<ResearchSubmission> AddSubmissionAsync(ResearchSubmission submission)
    {
        if (submission.Id == Guid.Empty)
            submission.Id = Guid.NewGuid();

        submission.CreatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO aesir.aesir_research_submission
                (id, session_id, agent_id, role, round_number, anonymized_id,
                 plan, content, thinking_trace, sources, tool_calls,
                 tokens_used, duration_ms, status, error_message, created_at, completed_at)
            VALUES
                (@Id, @SessionId, @AgentId, @Role, @RoundNumber, @AnonymizedId,
                 @Plan, @Content, @ThinkingTrace, @Sources::jsonb, @ToolCalls::jsonb,
                 @TokensUsed, @DurationMs, @Status, @ErrorMessage, @CreatedAt, @CompletedAt)";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, submission).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        logger.LogDebug("Created submission {SubmissionId} for session {SessionId}",
            submission.Id, submission.SessionId);

        return submission;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateSubmissionAsync(ResearchSubmission submission)
    {
        const string sql = @"
            UPDATE aesir.aesir_research_submission
            SET agent_id = @AgentId,
                role = @Role,
                round_number = @RoundNumber,
                anonymized_id = @AnonymizedId,
                plan = @Plan,
                content = @Content,
                thinking_trace = @ThinkingTrace,
                sources = @Sources::jsonb,
                tool_calls = @ToolCalls::jsonb,
                tokens_used = @TokensUsed,
                duration_ms = @DurationMs,
                status = @Status,
                error_message = @ErrorMessage,
                completed_at = @CompletedAt
            WHERE id = @Id";

        var rowsAffected = await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, submission).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        return rowsAffected > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResearchSubmission>> GetSubmissionsBySessionIdAsync(Guid sessionId)
    {
        const string sql = @"
            SELECT
                id as Id,
                session_id as SessionId,
                agent_id as AgentId,
                role as Role,
                round_number as RoundNumber,
                anonymized_id as AnonymizedId,
                plan as Plan,
                content as Content,
                thinking_trace as ThinkingTrace,
                sources as Sources,
                tool_calls as ToolCalls,
                tokens_used as TokensUsed,
                duration_ms as DurationMs,
                status as Status,
                error_message as ErrorMessage,
                created_at as CreatedAt,
                completed_at as CompletedAt
            FROM aesir.aesir_research_submission
            WHERE session_id = @SessionId
            ORDER BY round_number, role";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<ResearchSubmission>(sql, new { SessionId = sessionId }).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PeerReview> AddPeerReviewAsync(PeerReview review)
    {
        if (review.Id == Guid.Empty)
            review.Id = Guid.NewGuid();

        review.CreatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO aesir.aesir_research_peer_review
                (id, session_id, submission_id, reviewer_agent_id, reviewer_role,
                 score_depth, score_accuracy, score_source_quality, score_novelty, score_coherence,
                 weighted_average, strengths, improvements, critique, endorses, tokens_used, created_at)
            VALUES
                (@Id, @SessionId, @SubmissionId, @ReviewerAgentId, @ReviewerRole,
                 @ScoreDepth, @ScoreAccuracy, @ScoreSourceQuality, @ScoreNovelty, @ScoreCoherence,
                 @WeightedAverage, @Strengths, @Improvements, @Critique, @Endorses, @TokensUsed, @CreatedAt)";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, review).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        logger.LogDebug("Created peer review {ReviewId} for submission {SubmissionId}",
            review.Id, review.SubmissionId);

        return review;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PeerReview>> GetPeerReviewsBySessionIdAsync(Guid sessionId)
    {
        const string sql = @"
            SELECT
                id as Id,
                session_id as SessionId,
                submission_id as SubmissionId,
                reviewer_agent_id as ReviewerAgentId,
                reviewer_role as ReviewerRole,
                score_depth as ScoreDepth,
                score_accuracy as ScoreAccuracy,
                score_source_quality as ScoreSourceQuality,
                score_novelty as ScoreNovelty,
                score_coherence as ScoreCoherence,
                weighted_average as WeightedAverage,
                strengths as Strengths,
                improvements as Improvements,
                critique as Critique,
                endorses as Endorses,
                tokens_used as TokensUsed,
                created_at as CreatedAt
            FROM aesir.aesir_research_peer_review
            WHERE session_id = @SessionId
            ORDER BY created_at";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryAsync<PeerReview>(sql, new { SessionId = sessionId }).ConfigureAwait(false)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ResearchReport> UpsertReportAsync(ResearchReport report)
    {
        if (report.Id == Guid.Empty)
            report.Id = Guid.NewGuid();

        report.CreatedAt = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO aesir.aesir_research_report
                (id, session_id, title, executive_summary, methodology_section,
                 findings, alternative_perspectives, research_gaps, bibliography,
                 full_markdown, metadata, tokens_used, created_at)
            VALUES
                (@Id, @SessionId, @Title, @ExecutiveSummary, @MethodologySection,
                 @Findings::jsonb, @AlternativePerspectives, @ResearchGaps, @Bibliography::jsonb,
                 @FullMarkdown, @Metadata::jsonb, @TokensUsed, @CreatedAt)
            ON CONFLICT (session_id) DO UPDATE SET
                title = @Title,
                executive_summary = @ExecutiveSummary,
                methodology_section = @MethodologySection,
                findings = @Findings::jsonb,
                alternative_perspectives = @AlternativePerspectives,
                research_gaps = @ResearchGaps,
                bibliography = @Bibliography::jsonb,
                full_markdown = @FullMarkdown,
                metadata = @Metadata::jsonb,
                tokens_used = @TokensUsed,
                created_at = @CreatedAt";

        await dbContext.UnitOfWorkAsync(async connection =>
            await connection.ExecuteAsync(sql, report).ConfigureAwait(false), withTransaction: true).ConfigureAwait(false);

        logger.LogInformation("Upserted research report {ReportId} for session {SessionId}",
            report.Id, report.SessionId);

        return report;
    }

    /// <inheritdoc />
    public async Task<ResearchReport?> GetReportBySessionIdAsync(Guid sessionId)
    {
        const string sql = @"
            SELECT
                id as Id,
                session_id as SessionId,
                title as Title,
                executive_summary as ExecutiveSummary,
                methodology_section as MethodologySection,
                findings as Findings,
                alternative_perspectives as AlternativePerspectives,
                research_gaps as ResearchGaps,
                bibliography as Bibliography,
                full_markdown as FullMarkdown,
                metadata as Metadata,
                tokens_used as TokensUsed,
                created_at as CreatedAt
            FROM aesir.aesir_research_report
            WHERE session_id = @SessionId";

        return await dbContext.UnitOfWorkAsync(async connection =>
            await connection.QueryFirstOrDefaultAsync<ResearchReport>(sql, new { SessionId = sessionId }).ConfigureAwait(false)).ConfigureAwait(false);
    }
}

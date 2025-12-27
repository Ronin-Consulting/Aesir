using System.Text.Json;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Service for logging research trail events for transparency and debugging.
/// </summary>
public interface IResearchTrailService
{
    /// <summary>
    /// Logs a session created event.
    /// </summary>
    Task LogSessionCreatedAsync(ResearchSession session);

    /// <summary>
    /// Logs a phase change event.
    /// </summary>
    Task LogPhaseChangedAsync(Guid sessionId, ResearchPhase newPhase);

    /// <summary>
    /// Logs an agent starting work.
    /// </summary>
    Task LogAgentStartedAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string description);

    /// <summary>
    /// Logs an agent completing work.
    /// </summary>
    Task LogAgentCompletedAsync(Guid sessionId, Guid? submissionId, ResearchRole role, long durationMs, object? output = null);

    /// <summary>
    /// Logs a tool call.
    /// </summary>
    Task LogToolCallAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string toolName, object? input, object? output, long durationMs);

    /// <summary>
    /// Logs a RAG query.
    /// </summary>
    Task LogRagQueryAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string query, int resultCount, long durationMs);

    /// <summary>
    /// Logs a web search.
    /// </summary>
    Task LogWebSearchAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string query, int resultCount, long durationMs);

    /// <summary>
    /// Logs a peer review submission.
    /// </summary>
    Task LogPeerReviewSubmittedAsync(Guid sessionId, PeerReview review);

    /// <summary>
    /// Logs report generation.
    /// </summary>
    Task LogReportGeneratedAsync(Guid sessionId, ResearchReport report);

    /// <summary>
    /// Logs session completion.
    /// </summary>
    Task LogSessionCompletedAsync(ResearchSession session);

    /// <summary>
    /// Logs an error.
    /// </summary>
    Task LogErrorAsync(Guid sessionId, Guid? submissionId, ResearchRole? role, string errorMessage, Exception? exception = null);

    /// <summary>
    /// Gets all trail entries for a session.
    /// </summary>
    Task<IReadOnlyList<ResearchTrailEntry>> GetTrailAsync(Guid sessionId);
}

/// <summary>
/// Implementation of research trail service.
/// Note: Currently stores in memory. Database persistence will be added when repository is wired.
/// </summary>
public class ResearchTrailService : IResearchTrailService
{
    private readonly ILogger<ResearchTrailService> _logger;
    private readonly Dictionary<Guid, List<ResearchTrailEntry>> _trails = new();
    private readonly object _lock = new();

    public ResearchTrailService(ILogger<ResearchTrailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task LogSessionCreatedAsync(ResearchSession session)
    {
        var entry = CreateEntry(
            session.Id,
            null,
            ResearchTrailEventType.SessionCreated,
            null,
            $"Research session created for query: {Truncate(session.Query, 100)}");

        entry.InputJson = JsonSerializer.Serialize(new
        {
            query = session.Query,
            mode = session.Mode.ToString(),
            teamId = session.ResearchTeamId
        });

        AddEntry(entry);

        _logger.LogDebug("Trail: Session {SessionId} created", session.Id);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogPhaseChangedAsync(Guid sessionId, ResearchPhase newPhase)
    {
        var entry = CreateEntry(
            sessionId,
            null,
            ResearchTrailEventType.PhaseChanged,
            null,
            $"Phase changed to: {newPhase}");

        entry.OutputJson = JsonSerializer.Serialize(new { phase = newPhase.ToString() });

        AddEntry(entry);

        _logger.LogDebug("Trail: Session {SessionId} phase -> {Phase}", sessionId, newPhase);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogAgentStartedAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string description)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.AgentStarted,
            role,
            $"{role} started: {description}");

        AddEntry(entry);

        _logger.LogDebug("Trail: {Role} started in session {SessionId}", role, sessionId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogAgentCompletedAsync(Guid sessionId, Guid? submissionId, ResearchRole role, long durationMs, object? output = null)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.AgentCompleted,
            role,
            $"{role} completed in {durationMs}ms");

        entry.DurationMs = durationMs;
        if (output != null)
        {
            entry.OutputJson = JsonSerializer.Serialize(output);
        }

        AddEntry(entry);

        _logger.LogDebug("Trail: {Role} completed in session {SessionId} ({Duration}ms)", role, sessionId, durationMs);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogToolCallAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string toolName, object? input, object? output, long durationMs)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.ToolCall,
            role,
            $"{role} called tool: {toolName}");

        entry.DurationMs = durationMs;
        if (input != null)
        {
            entry.InputJson = JsonSerializer.Serialize(new { tool = toolName, input });
        }
        if (output != null)
        {
            entry.OutputJson = JsonSerializer.Serialize(output);
        }

        AddEntry(entry);

        _logger.LogDebug("Trail: {Role} tool call {Tool} in session {SessionId}", role, toolName, sessionId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogRagQueryAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string query, int resultCount, long durationMs)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.RagQuery,
            role,
            $"{role} RAG query: {Truncate(query, 50)} -> {resultCount} results");

        entry.DurationMs = durationMs;
        entry.InputJson = JsonSerializer.Serialize(new { query });
        entry.OutputJson = JsonSerializer.Serialize(new { resultCount });

        AddEntry(entry);

        _logger.LogDebug("Trail: {Role} RAG query in session {SessionId} ({Results} results)", role, sessionId, resultCount);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogWebSearchAsync(Guid sessionId, Guid? submissionId, ResearchRole role, string query, int resultCount, long durationMs)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.WebSearch,
            role,
            $"{role} web search: {Truncate(query, 50)} -> {resultCount} results");

        entry.DurationMs = durationMs;
        entry.InputJson = JsonSerializer.Serialize(new { query });
        entry.OutputJson = JsonSerializer.Serialize(new { resultCount });

        AddEntry(entry);

        _logger.LogDebug("Trail: {Role} web search in session {SessionId} ({Results} results)", role, sessionId, resultCount);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogPeerReviewSubmittedAsync(Guid sessionId, PeerReview review)
    {
        var entry = CreateEntry(
            sessionId,
            review.SubmissionId,
            ResearchTrailEventType.PeerReviewSubmitted,
            review.ReviewerRole,
            $"{review.ReviewerRole} reviewed submission with score {review.WeightedAverage:F1}");

        entry.OutputJson = JsonSerializer.Serialize(new
        {
            submissionId = review.SubmissionId,
            score = review.WeightedAverage,
            endorses = review.Endorses
        });

        AddEntry(entry);

        _logger.LogDebug("Trail: Peer review submitted in session {SessionId} (score: {Score})", sessionId, review.WeightedAverage);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogReportGeneratedAsync(Guid sessionId, ResearchReport report)
    {
        var entry = CreateEntry(
            sessionId,
            null,
            ResearchTrailEventType.ReportGenerated,
            ResearchRole.Chairman,
            $"Report generated: {report.Title}");

        entry.OutputJson = JsonSerializer.Serialize(new
        {
            reportId = report.Id,
            title = report.Title,
            findingCount = report.Findings?.Count ?? 0,
            tokensUsed = report.TokensUsed
        });

        AddEntry(entry);

        _logger.LogDebug("Trail: Report generated for session {SessionId}", sessionId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogSessionCompletedAsync(ResearchSession session)
    {
        var entry = CreateEntry(
            session.Id,
            null,
            ResearchTrailEventType.SessionCompleted,
            null,
            $"Research completed: {session.Status}");

        var durationMs = session.CompletedAt.HasValue && session.StartedAt.HasValue
            ? (long)(session.CompletedAt.Value - session.StartedAt.Value).TotalMilliseconds
            : 0;

        entry.DurationMs = durationMs;
        entry.OutputJson = JsonSerializer.Serialize(new
        {
            status = session.Status.ToString(),
            submissionCount = session.Submissions?.Count ?? 0,
            reviewCount = session.PeerReviews?.Count ?? 0
        });

        AddEntry(entry);

        _logger.LogDebug("Trail: Session {SessionId} completed with status {Status}", session.Id, session.Status);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogErrorAsync(Guid sessionId, Guid? submissionId, ResearchRole? role, string errorMessage, Exception? exception = null)
    {
        var entry = CreateEntry(
            sessionId,
            submissionId,
            ResearchTrailEventType.Error,
            role,
            $"Error: {errorMessage}");

        entry.OutputJson = JsonSerializer.Serialize(new
        {
            error = errorMessage,
            exceptionType = exception?.GetType().Name,
            stackTrace = exception?.StackTrace
        });

        AddEntry(entry);

        _logger.LogError(exception, "Trail: Error in session {SessionId}: {Error}", sessionId, errorMessage);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ResearchTrailEntry>> GetTrailAsync(Guid sessionId)
    {
        lock (_lock)
        {
            if (_trails.TryGetValue(sessionId, out var entries))
            {
                return Task.FromResult<IReadOnlyList<ResearchTrailEntry>>(entries.OrderBy(e => e.Timestamp).ToList());
            }
            return Task.FromResult<IReadOnlyList<ResearchTrailEntry>>(Array.Empty<ResearchTrailEntry>());
        }
    }

    private static ResearchTrailEntry CreateEntry(
        Guid sessionId,
        Guid? submissionId,
        ResearchTrailEventType eventType,
        ResearchRole? role,
        string description)
    {
        return new ResearchTrailEntry
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            SubmissionId = submissionId,
            EventType = eventType,
            AgentRole = role,
            Description = description,
            Timestamp = DateTime.UtcNow
        };
    }

    private void AddEntry(ResearchTrailEntry entry)
    {
        lock (_lock)
        {
            if (!_trails.ContainsKey(entry.SessionId))
            {
                _trails[entry.SessionId] = new List<ResearchTrailEntry>();
            }
            _trails[entry.SessionId].Add(entry);
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..maxLength] + "...";
    }
}

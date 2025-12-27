using Aesir.Common.Models;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Orchestrates the complete research workflow from start to finish.
/// </summary>
public interface IResearchOrchestrator
{
    /// <summary>
    /// Starts a new research session.
    /// </summary>
    /// <param name="query">The research query.</param>
    /// <param name="teamId">The research team ID to use.</param>
    /// <param name="mode">The research mode.</param>
    /// <param name="documentCollectionIds">Optional document collection IDs for RAG.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created research session.</returns>
    Task<ResearchSession> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchMode mode = ResearchMode.Standard,
        IReadOnlyList<Guid>? documentCollectionIds = null,
        string userId = "default",
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits clarification answers and continues the research.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="answers">The answers to the clarification questions.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubmitClarificationAnswersAsync(
        Guid sessionId,
        IReadOnlyDictionary<string, string> answers,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a research session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The research session with current status.</returns>
    Task<ResearchSession?> GetSessionStatusAsync(Guid sessionId);

    /// <summary>
    /// Cancels an in-progress research session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    Task CancelResearchAsync(Guid sessionId);
}

/// <summary>
/// Implementation of the research orchestrator.
/// Note: Full integration with external services will be added in later phases.
/// </summary>
public class ResearchOrchestrator : IResearchOrchestrator
{
    private readonly ILogger<ResearchOrchestrator> _logger;
    private readonly IResearchSessionRepository _sessionRepository;
    private readonly IResearchTeamRepository _teamRepository;
    private readonly IResearchAgentFactory _agentFactory;
    private readonly IClarificationService _clarificationService;
    private readonly IResearchPhaseExecutor _phaseExecutor;

    public ResearchOrchestrator(
        ILogger<ResearchOrchestrator> logger,
        IResearchSessionRepository sessionRepository,
        IResearchTeamRepository teamRepository,
        IResearchAgentFactory agentFactory,
        IClarificationService clarificationService,
        IResearchPhaseExecutor phaseExecutor)
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _agentFactory = agentFactory;
        _clarificationService = clarificationService;
        _phaseExecutor = phaseExecutor;
    }

    /// <inheritdoc />
    public async Task<ResearchSession> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchMode mode = ResearchMode.Standard,
        IReadOnlyList<Guid>? documentCollectionIds = null,
        string userId = "default",
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting research session for team {TeamId}", teamId);

        // Get the team configuration
        var team = await _teamRepository.GetByIdAsync(teamId);
        if (team == null)
        {
            throw new KeyNotFoundException($"Research team {teamId} not found");
        }

        // TODO: Resolve base agents when integration is complete
        // For now, create agents with minimal config
        var agentDict = new Dictionary<Guid, AesirAgentBase>();
        foreach (var member in team.Members ?? [])
        {
            // Create stub agent - will be replaced with actual resolution
            agentDict[member.AgentId] = new AesirAgentBase
            {
                Id = member.AgentId,
                Name = $"Agent-{member.Role}",
                ChatModel = "gpt-4",
                ChatTemperature = 0.7
            };
        }

        // Create research agents
        var researchAgents = _agentFactory.CreateAgentsForTeam(team, agentDict);

        // Find the Chairman agent
        var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);
        if (chairman == null && mode != ResearchMode.Quick)
        {
            throw new InvalidOperationException("Team must have a Chairman agent for Standard/Deep mode");
        }

        // Create the session
        var session = new ResearchSession
        {
            Id = Guid.NewGuid(),
            ResearchTeamId = teamId,
            UserId = userId,
            Query = query,
            Mode = mode,
            Status = ResearchStatus.Created,
            CurrentPhase = ResearchPhase.Clarification,
            DocumentCollectionIds = documentCollectionIds?.ToList() ?? [],
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session);

        // Generate clarification questions (if Chairman exists)
        if (chairman != null)
        {
            var questions = await _clarificationService.GenerateClarificationQuestionsAsync(
                query,
                chairman,
                cancellationToken);

            if (questions.Count > 0)
            {
                session.ClarificationQuestions = questions.ToList();
                session.Status = ResearchStatus.AwaitingClarification;
                session.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);

                _logger.LogInformation("Session {SessionId} awaiting {Count} clarification answers",
                    session.Id, questions.Count);

                return session;
            }
        }

        // No clarification needed, proceed directly to research
        session.RefinedQuery = query;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        await ExecuteResearchWorkflowAsync(session, researchAgents, progressCallback, cancellationToken);

        return session;
    }

    /// <inheritdoc />
    public async Task SubmitClarificationAnswersAsync(
        Guid sessionId,
        IReadOnlyDictionary<string, string> answers,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Research session {sessionId} not found");
        }

        if (session.Status != ResearchStatus.AwaitingClarification)
        {
            throw new InvalidOperationException($"Session is not awaiting clarification (status: {session.Status})");
        }

        _logger.LogInformation("Processing clarification answers for session {SessionId}", sessionId);

        // Store answers
        session.ClarificationAnswers = new Dictionary<string, string>(answers);
        session.UpdatedAt = DateTime.UtcNow;

        // Get team and create research agents
        var team = await _teamRepository.GetByIdAsync(session.ResearchTeamId ?? Guid.Empty);
        if (team == null)
        {
            throw new KeyNotFoundException($"Research team {session.ResearchTeamId} not found");
        }

        // TODO: Resolve base agents when integration is complete
        var agentDict = new Dictionary<Guid, AesirAgentBase>();
        foreach (var member in team.Members ?? [])
        {
            agentDict[member.AgentId] = new AesirAgentBase
            {
                Id = member.AgentId,
                Name = $"Agent-{member.Role}",
                ChatModel = "gpt-4",
                ChatTemperature = 0.7
            };
        }

        var researchAgents = _agentFactory.CreateAgentsForTeam(team, agentDict);
        var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);

        // Refine the query
        if (chairman != null && session.ClarificationQuestions?.Count > 0)
        {
            session.RefinedQuery = await _clarificationService.RefineQueryAsync(
                session.Query,
                session.ClarificationQuestions,
                answers,
                chairman,
                cancellationToken);
        }
        else
        {
            session.RefinedQuery = session.Query;
        }

        await _sessionRepository.UpdateAsync(session);

        // Continue with research workflow
        await ExecuteResearchWorkflowAsync(session, researchAgents, progressCallback, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ResearchSession?> GetSessionStatusAsync(Guid sessionId)
    {
        return await _sessionRepository.GetByIdAsync(sessionId);
    }

    /// <inheritdoc />
    public async Task CancelResearchAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new KeyNotFoundException($"Research session {sessionId} not found");
        }

        session.Status = ResearchStatus.Cancelled;
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session);

        _logger.LogInformation("Research session {SessionId} cancelled", sessionId);
    }

    private async Task ExecuteResearchWorkflowAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> researchAgents,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var nonChairmanAgents = researchAgents.Where(a => !a.IsChairman).ToList();

            // Phase 1: Planning
            session.Status = ResearchStatus.Planning;
            session.CurrentPhase = ResearchPhase.Planning;
            session.StartedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            var plans = await _phaseExecutor.ExecutePlanningPhaseAsync(
                session,
                nonChairmanAgents,
                session.RefinedQuery ?? session.Query,
                progressCallback,
                cancellationToken);

            // Phase 2: Research
            session.Status = ResearchStatus.Researching;
            session.CurrentPhase = ResearchPhase.Research;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            var submissions = await _phaseExecutor.ExecuteResearchPhaseAsync(
                session,
                nonChairmanAgents,
                session.RefinedQuery ?? session.Query,
                plans,
                progressCallback,
                cancellationToken);

            // Store submissions
            session.Submissions = submissions;

            // Mark as complete (peer review and synthesis will be added in later phases)
            session.Status = ResearchStatus.Completed;
            session.CurrentPhase = ResearchPhase.Synthesis;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            _logger.LogInformation("Research session {SessionId} completed with {Count} submissions",
                session.Id, submissions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research workflow failed for session {SessionId}", session.Id);

            session.Status = ResearchStatus.Failed;
            session.ErrorMessage = ex.Message;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            throw;
        }
    }
}

using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
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
/// </summary>
public class ResearchOrchestrator : IResearchOrchestrator
{
    private readonly ILogger<ResearchOrchestrator> _logger;
    private readonly IResearchSessionRepository _sessionRepository;
    private readonly IResearchTeamRepository _teamRepository;
    private readonly IResearchAgentFactory _agentFactory;
    private readonly IClarificationService _clarificationService;
    private readonly IResearchPhaseExecutor _phaseExecutor;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly IConfigurationService _configurationService;

    public ResearchOrchestrator(
        ILogger<ResearchOrchestrator> logger,
        IResearchSessionRepository sessionRepository,
        IResearchTeamRepository teamRepository,
        IResearchAgentFactory agentFactory,
        IClarificationService clarificationService,
        IResearchPhaseExecutor phaseExecutor,
        IResearchProgressBroadcaster progressBroadcaster,
        IConfigurationService configurationService)
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _agentFactory = agentFactory;
        _clarificationService = clarificationService;
        _phaseExecutor = phaseExecutor;
        _progressBroadcaster = progressBroadcaster;
        _configurationService = configurationService;
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

        // Resolve base agents from configuration
        var agentDict = await ResolveBaseAgentsAsync(team);

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
                session.Id,
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

        // Resolve base agents from configuration
        var agentDict = await ResolveBaseAgentsAsync(team);

        var researchAgents = _agentFactory.CreateAgentsForTeam(team, agentDict);
        var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);

        // Refine the query
        if (chairman != null && session.ClarificationQuestions?.Count > 0)
        {
            session.RefinedQuery = await _clarificationService.RefineQueryAsync(
                session.Id,
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
            // Set the session ID for broadcasting progress updates
            _progressBroadcaster.SetCurrentSession(session.Id);

            var nonChairmanAgents = researchAgents.Where(a => !a.IsChairman).ToList();

            // Phase 1: Planning
            session.Status = ResearchStatus.Planning;
            session.CurrentPhase = ResearchPhase.Planning;
            session.StartedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            await _progressBroadcaster.BroadcastStatusChangeAsync(
                session.Id, session.Status, session.CurrentPhase, "Starting planning phase...");

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
            await _progressBroadcaster.BroadcastStatusChangeAsync(
                session.Id, session.Status, session.CurrentPhase, "Agents are conducting research...");

            var submissions = await _phaseExecutor.ExecuteResearchPhaseAsync(
                session,
                nonChairmanAgents,
                session.RefinedQuery ?? session.Query,
                plans,
                progressCallback,
                cancellationToken);

            // Store submissions
            session.Submissions = submissions;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            // Phase 3: Anonymization
            session.Status = ResearchStatus.Anonymizing;
            session.CurrentPhase = ResearchPhase.Anonymization;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            await _progressBroadcaster.BroadcastStatusChangeAsync(
                session.Id, session.Status, session.CurrentPhase, "Anonymizing submissions for peer review...");

            var anonymizedSubmissions = await _phaseExecutor.ExecuteAnonymizationPhaseAsync(
                session,
                submissions,
                progressCallback,
                cancellationToken);

            // Phase 4: Peer Review
            session.Status = ResearchStatus.PeerReviewing;
            session.CurrentPhase = ResearchPhase.PeerReview;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            await _progressBroadcaster.BroadcastStatusChangeAsync(
                session.Id, session.Status, session.CurrentPhase, "Agents are reviewing each other's work...");

            var peerReviews = await _phaseExecutor.ExecutePeerReviewPhaseAsync(
                session,
                nonChairmanAgents,
                anonymizedSubmissions,
                progressCallback,
                cancellationToken);

            // Store peer reviews
            session.PeerReviews = peerReviews;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            // Phase 5: Synthesis
            session.Status = ResearchStatus.Synthesizing;
            session.CurrentPhase = ResearchPhase.Synthesis;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            await _progressBroadcaster.BroadcastStatusChangeAsync(
                session.Id, session.Status, session.CurrentPhase, "Chairman is synthesizing the final report...");

            // Get Chairman agent for synthesis
            var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);
            if (chairman == null)
            {
                // Create a default Chairman if none configured
                chairman = new ResearchAgent
                {
                    TeamMemberId = Guid.Empty,
                    Role = ResearchRole.Chairman,
                    RoleName = "Chairman",
                    BaseAgentId = Guid.Empty,
                    Temperature = 0.5,
                    Persona = "You are a research synthesis expert."
                };
            }

            var report = await _phaseExecutor.ExecuteSynthesisPhaseAsync(
                session,
                chairman,
                progressCallback,
                cancellationToken);

            // Store report
            session.Report = report;

            // Mark as complete
            session.Status = ResearchStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            _logger.LogInformation(
                "Research session {SessionId} completed with {SubmissionCount} submissions, {ReviewCount} reviews, and report '{Title}'",
                session.Id, submissions.Count, peerReviews.Count, report.Title);

            // Broadcast completion event
            await _progressBroadcaster.BroadcastCompletionAsync(session.Id, report.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research workflow failed for session {SessionId}", session.Id);

            session.Status = ResearchStatus.Failed;
            session.ErrorMessage = ex.Message;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);

            // Broadcast error event
            await _progressBroadcaster.BroadcastErrorAsync(session.Id, ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Resolves base agents from configuration for each team member.
    /// </summary>
    /// <param name="team">The research team.</param>
    /// <returns>Dictionary mapping agent IDs to their base agent configurations.</returns>
    private async Task<Dictionary<Guid, AesirAgentBase>> ResolveBaseAgentsAsync(ResearchTeam team)
    {
        var agentDict = new Dictionary<Guid, AesirAgentBase>();

        foreach (var member in team.Members ?? [])
        {
            try
            {
                var baseAgent = await _configurationService.GetAgentAsync(member.AgentId);
                agentDict[member.AgentId] = baseAgent;
                _logger.LogDebug("Resolved agent {AgentId} ({Name}) for role {Role}",
                    member.AgentId, baseAgent.Name, member.Role);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve agent {AgentId} for role {Role}, using fallback",
                    member.AgentId, member.Role);

                // Fallback to basic configuration
                agentDict[member.AgentId] = new AesirAgentBase
                {
                    Id = member.AgentId,
                    Name = $"Agent-{member.Role}",
                    ChatModel = "gpt-4",
                    ChatTemperature = 0.7
                };
            }
        }

        _logger.LogInformation("Resolved {Count} agents for research team {TeamId}",
            agentDict.Count, team.Id);

        return agentDict;
    }
}

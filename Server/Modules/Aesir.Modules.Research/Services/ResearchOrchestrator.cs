using System.Collections.Concurrent;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    /// <param name="chatSessionId">Optional ChatSession ID to link research to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created research session.</returns>
    Task<ResearchSession> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchMode mode = ResearchMode.Standard,
        IReadOnlyList<Guid>? documentCollectionIds = null,
        string userId = "default",
        Guid? chatSessionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits clarification answers and continues the research.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="answers">The answers to the clarification questions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SubmitClarificationAnswersAsync(
        Guid sessionId,
        IReadOnlyDictionary<string, string> answers,
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
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly IConfigurationService _configurationService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHostApplicationLifetime _applicationLifetime;

    /// <summary>
    /// Stores CancellationTokenSources for active research sessions.
    /// Enables explicit cancellation via CancelResearchAsync and cleanup on completion.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sessionCancellationTokens = new();

    public ResearchOrchestrator(
        ILogger<ResearchOrchestrator> logger,
        IResearchSessionRepository sessionRepository,
        IResearchTeamRepository teamRepository,
        IResearchAgentFactory agentFactory,
        IClarificationService clarificationService,
        IResearchProgressBroadcaster progressBroadcaster,
        IConfigurationService configurationService,
        IChatHistoryService chatHistoryService,
        IServiceScopeFactory serviceScopeFactory,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
        _teamRepository = teamRepository;
        _agentFactory = agentFactory;
        _clarificationService = clarificationService;
        _progressBroadcaster = progressBroadcaster;
        _configurationService = configurationService;
        _chatHistoryService = chatHistoryService;
        _serviceScopeFactory = serviceScopeFactory;
        _applicationLifetime = applicationLifetime;
    }

    /// <inheritdoc />
    public async Task<ResearchSession> StartResearchAsync(
        string query,
        Guid teamId,
        ResearchMode mode = ResearchMode.Standard,
        IReadOnlyList<Guid>? documentCollectionIds = null,
        string userId = "default",
        Guid? chatSessionId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("=== [RESEARCH] StartResearchAsync ENTRY ===");
        _logger.LogDebug("[RESEARCH] Query: {Query}", query);
        _logger.LogDebug("[RESEARCH] TeamId: {TeamId}", teamId);
        _logger.LogDebug("[RESEARCH] Mode: {Mode}", mode);
        _logger.LogDebug("[RESEARCH] UserId: {UserId}", userId);
        _logger.LogDebug("[RESEARCH] ChatSessionId: {ChatSessionId}", chatSessionId);
        _logger.LogDebug("[RESEARCH] DocumentCollectionIds: {Ids}", documentCollectionIds != null ? string.Join(",", documentCollectionIds) : "null");
        _logger.LogInformation("Starting research session for team {TeamId}", teamId);

        // Get the team configuration
        _logger.LogDebug("[RESEARCH] Loading team from repository...");
        var team = await _teamRepository.GetByIdAsync(teamId).ConfigureAwait(false);
        if (team == null)
        {
            _logger.LogError("[RESEARCH] Team NOT FOUND: {TeamId}", teamId);
            throw new KeyNotFoundException($"Research team {teamId} not found");
        }
        _logger.LogDebug("[RESEARCH] Team loaded: {TeamName}, Members: {MemberCount}", team.Name, team.Members?.Count ?? 0);

        // Resolve base agents from configuration
        _logger.LogDebug("[RESEARCH] Resolving base agents from configuration...");
        var agentDict = await ResolveBaseAgentsAsync(team).ConfigureAwait(false);
        _logger.LogDebug("[RESEARCH] Resolved {AgentCount} base agents", agentDict.Count);

        // Create research agents
        _logger.LogDebug("[RESEARCH] Creating research agents from team members...");
        var researchAgents = _agentFactory.CreateAgentsForTeam(team, agentDict);
        _logger.LogDebug("[RESEARCH] Created {AgentCount} research agents:", researchAgents.Count);
        foreach (var agent in researchAgents)
        {
            _logger.LogDebug("[RESEARCH]   - {Role} ({RoleName}): BaseAgentId={BaseAgentId}, InferenceEngineId={InferenceEngineId}, Model={Model}",
                agent.Role, agent.RoleName, agent.BaseAgentId, agent.InferenceEngineId, agent.Model);
        }

        // Find the Chairman agent
        var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);
        _logger.LogDebug("[RESEARCH] Chairman agent: {ChairmanFound}", chairman != null ? $"Found ({chairman.RoleName})" : "NOT FOUND");
        if (chairman == null && mode != ResearchMode.Quick)
        {
            _logger.LogError("[RESEARCH] No Chairman agent found and mode is not Quick!");
            throw new InvalidOperationException("Team must have a Chairman agent for Standard/Deep mode");
        }

        // Create the session
        // NOTE: ChatSessionId is initially set to null because the FK constraint requires
        // the chat session to exist before we can reference it. We create the chat session
        // first, then update the research session with the chat_session_id.
        _logger.LogDebug("[RESEARCH] Creating research session...");
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
            ChatSessionId = null, // Set after chat session is created (FK constraint)
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sessionRepository.AddAsync(session).ConfigureAwait(false);
        _logger.LogDebug("[RESEARCH] Session created with ID: {SessionId}", session.Id);

        // Note: Session ID is now passed explicitly to each broadcast call (stateless broadcaster)

        // Initialize ChatSession immediately so it appears in chat history
        // This creates the chat session BEFORE we link it to the research session
        // (required by FK constraint on chat_session_id)
        var initializedChatSessionId = await InitializeChatSessionAsync(session, query, userId).ConfigureAwait(false);
        session.ChatSessionId = initializedChatSessionId;
        await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
        _logger.LogDebug("[RESEARCH] Session updated with ChatSessionId: {ChatSessionId}", initializedChatSessionId);

        // Generate clarification questions (if Chairman exists)
        if (chairman != null)
        {
            _logger.LogDebug("[RESEARCH] Generating clarification questions...");
            var questions = await _clarificationService.GenerateClarificationQuestionsAsync(
                session.Id,
                query,
                chairman,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH] Generated {QuestionCount} clarification questions", questions.Count);

            if (questions.Count > 0)
            {
                session.ClarificationQuestions = questions.ToList();
                session.Status = ResearchStatus.AwaitingClarification;
                session.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

                _logger.LogInformation("Session {SessionId} awaiting {Count} clarification answers",
                    session.Id, questions.Count);
                _logger.LogDebug("[RESEARCH] === RETURNING - Awaiting Clarification ===");

                return session;
            }
        }
        else
        {
            _logger.LogDebug("[RESEARCH] No Chairman, skipping clarification phase");
        }

        // No clarification needed, proceed directly to research
        _logger.LogDebug("[RESEARCH] No clarification needed, proceeding to research workflow");
        session.RefinedQuery = query;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

        // Fire-and-forget the workflow - don't block the HTTP response
        // The workflow can take minutes; client will receive updates via SignalR
        // Create a new scope for the background task to avoid using disposed scoped services
        _logger.LogDebug("[RESEARCH] === STARTING ExecuteResearchWorkflowAsync (fire-and-forget) ===");

        // Create a CancellationTokenSource for this session that:
        // 1. Cancels when the application is stopping (graceful shutdown)
        // 2. Can be explicitly cancelled via CancelResearchAsync
        // NOTE: We do NOT link to the HTTP request's cancellationToken because
        // the workflow runs in background after the HTTP response is sent
        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(
            _applicationLifetime.ApplicationStopping);
        _sessionCancellationTokens[session.Id] = sessionCts;

        // Task.Run uses CancellationToken.None so it always starts
        // The workflow itself uses the session-specific token
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for the background task
                using var scope = _serviceScopeFactory.CreateScope();
                var phaseExecutor = scope.ServiceProvider.GetRequiredService<IResearchPhaseExecutor>();

                // Note: Session ID is now passed explicitly to each broadcast call (stateless broadcaster)
                await ExecuteResearchWorkflowAsync(session, researchAgents, phaseExecutor, sessionCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[RESEARCH] Research session {SessionId} was cancelled", session.Id);
                // Cancellation is expected - update session status if not already done
                var currentSession = await _sessionRepository.GetByIdAsync(session.Id).ConfigureAwait(false);
                if (currentSession != null && currentSession.Status != ResearchStatus.Cancelled)
                {
                    currentSession.Status = ResearchStatus.Cancelled;
                    currentSession.CompletedAt = DateTime.UtcNow;
                    currentSession.UpdatedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(currentSession).ConfigureAwait(false);
                    await _progressBroadcaster.BroadcastErrorAsync(session.Id, "Research was cancelled").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RESEARCH] Background workflow failed for session {SessionId}", session.Id);
                // Error is already handled and broadcast within ExecuteResearchWorkflowAsync
            }
            finally
            {
                // Cleanup: remove CTS from dictionary and dispose
                _sessionCancellationTokens.TryRemove(session.Id, out _);
                sessionCts.Dispose();
            }
        }, CancellationToken.None);

        _logger.LogDebug("[RESEARCH] === StartResearchAsync COMPLETE (workflow running in background) ===");
        return session;
    }

    /// <inheritdoc />
    public async Task SubmitClarificationAnswersAsync(
        Guid sessionId,
        IReadOnlyDictionary<string, string> answers,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new KeyNotFoundException($"Research session {sessionId} not found");
        }

        if (session.Status != ResearchStatus.AwaitingClarification)
        {
            throw new InvalidOperationException($"Session is not awaiting clarification (status: {session.Status})");
        }

        _logger.LogInformation("Processing clarification answers for session {SessionId}", sessionId);

        // Note: Session ID is now passed explicitly to each broadcast call (stateless broadcaster)

        // Store answers
        session.ClarificationAnswers = new Dictionary<string, string>(answers);
        session.UpdatedAt = DateTime.UtcNow;

        // Get team and create research agents
        var team = await _teamRepository.GetByIdAsync(session.ResearchTeamId ?? Guid.Empty).ConfigureAwait(false);
        if (team == null)
        {
            throw new KeyNotFoundException($"Research team {session.ResearchTeamId} not found");
        }

        // Resolve base agents from configuration
        var agentDict = await ResolveBaseAgentsAsync(team).ConfigureAwait(false);

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
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            session.RefinedQuery = session.Query;
        }

        await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

        // Fire-and-forget the workflow - don't block the HTTP response
        // The workflow can take minutes; client will receive updates via SignalR
        // Create a new scope for the background task to avoid using disposed scoped services
        _logger.LogDebug("[RESEARCH] === STARTING ExecuteResearchWorkflowAsync (fire-and-forget) ===");

        // Create a CancellationTokenSource for this session that:
        // 1. Cancels when the application is stopping (graceful shutdown)
        // 2. Can be explicitly cancelled via CancelResearchAsync
        // NOTE: We do NOT link to the HTTP request's cancellationToken because
        // the workflow runs in background after the HTTP response is sent
        var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(
            _applicationLifetime.ApplicationStopping);
        _sessionCancellationTokens[session.Id] = sessionCts;

        // Task.Run uses CancellationToken.None so it always starts
        // The workflow itself uses the session-specific token
        _ = Task.Run(async () =>
        {
            try
            {
                // Create a new scope for the background task
                using var scope = _serviceScopeFactory.CreateScope();
                var phaseExecutor = scope.ServiceProvider.GetRequiredService<IResearchPhaseExecutor>();

                // Note: Session ID is now passed explicitly to each broadcast call (stateless broadcaster)
                await ExecuteResearchWorkflowAsync(session, researchAgents, phaseExecutor, sessionCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[RESEARCH] Research session {SessionId} was cancelled", session.Id);
                // Cancellation is expected - update session status if not already done
                var currentSession = await _sessionRepository.GetByIdAsync(session.Id).ConfigureAwait(false);
                if (currentSession != null && currentSession.Status != ResearchStatus.Cancelled)
                {
                    currentSession.Status = ResearchStatus.Cancelled;
                    currentSession.CompletedAt = DateTime.UtcNow;
                    currentSession.UpdatedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(currentSession).ConfigureAwait(false);
                    await _progressBroadcaster.BroadcastErrorAsync(session.Id, "Research was cancelled").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RESEARCH] Background workflow failed for session {SessionId}", session.Id);
                // Error is already handled and broadcast within ExecuteResearchWorkflowAsync
            }
            finally
            {
                // Cleanup: remove CTS from dictionary and dispose
                _sessionCancellationTokens.TryRemove(session.Id, out _);
                sessionCts.Dispose();
            }
        }, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ResearchSession?> GetSessionStatusAsync(Guid sessionId)
    {
        return await _sessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CancelResearchAsync(Guid sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new KeyNotFoundException($"Research session {sessionId} not found");
        }

        // Cancel the running workflow if it exists
        if (_sessionCancellationTokens.TryRemove(sessionId, out var cts))
        {
            _logger.LogInformation("Cancelling active research workflow for session {SessionId}", sessionId);
            await cts.CancelAsync().ConfigureAwait(false);
            // Note: The CTS will be disposed in the finally block of the background task
        }

        session.Status = ResearchStatus.Cancelled;
        session.CompletedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

        _logger.LogInformation("Research session {SessionId} cancelled", sessionId);
    }

    private async Task ExecuteResearchWorkflowAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> researchAgents,
        IResearchPhaseExecutor phaseExecutor,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[RESEARCH-WORKFLOW] === ExecuteResearchWorkflowAsync START ===");
        _logger.LogDebug("[RESEARCH-WORKFLOW] SessionId: {SessionId}", session.Id);
        _logger.LogDebug("[RESEARCH-WORKFLOW] Total agents: {AgentCount}", researchAgents.Count);

        try
        {
            // Note: Session ID is now passed explicitly to each broadcast call (stateless broadcaster)

            var chairmanAgent = researchAgents.FirstOrDefault(a => a.IsChairman);
            var nonChairmanAgents = researchAgents.Where(a => !a.IsChairman).ToList();
            _logger.LogDebug("[RESEARCH-WORKFLOW] Chairman agent: {Role}", chairmanAgent?.Role ?? ResearchRole.Chairman);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Non-Chairman agents: {Count}", nonChairmanAgents.Count);
            foreach (var agent in nonChairmanAgents)
            {
                _logger.LogDebug("[RESEARCH-WORKFLOW]   Agent: {Role}, InferenceEngineId={InferenceEngineId}",
                    agent.Role, agent.InferenceEngineId);
            }

            if (chairmanAgent == null)
            {
                throw new InvalidOperationException("Research session must have a Chairman agent");
            }

            // Phase 1: Planning
            _logger.LogDebug("[RESEARCH-WORKFLOW] === PHASE 1: PLANNING ===");
            session.Status = ResearchStatus.Planning;
            session.CurrentPhase = ResearchPhase.Planning;
            session.StartedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting status change: Planning");
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id, ResearchPhase.Planning, 0, "Starting planning phase...").ConfigureAwait(false);

            _logger.LogDebug("[RESEARCH-WORKFLOW] Calling ExecutePlanningPhaseAsync with Chairman unified planning...");
            var plans = await phaseExecutor.ExecutePlanningPhaseAsync(
                session,
                chairmanAgent,
                nonChairmanAgents,
                session.RefinedQuery ?? session.Query,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Planning phase completed: {PlanCount} plans", plans.Count);

            // Phase 2: Research
            _logger.LogDebug("[RESEARCH-WORKFLOW] === PHASE 2: RESEARCH ===");
            session.Status = ResearchStatus.Researching;
            session.CurrentPhase = ResearchPhase.Research;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting status change: Researching");
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id, ResearchPhase.Research, 0, "Agents are conducting research...").ConfigureAwait(false);

            _logger.LogDebug("[RESEARCH-WORKFLOW] Calling ExecuteResearchPhaseAsync...");
            var submissions = await phaseExecutor.ExecuteResearchPhaseAsync(
                session,
                nonChairmanAgents,
                session.RefinedQuery ?? session.Query,
                plans,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Research phase completed: {SubmissionCount} submissions", submissions.Count);
            foreach (var sub in submissions)
            {
                _logger.LogDebug("[RESEARCH-WORKFLOW]   Submission: {Role}, Status={Status}, ContentLength={Length}",
                    sub.Role, sub.Status, sub.Content?.Length ?? 0);
            }

            // Persist submissions to database
            foreach (var submission in submissions)
            {
                await _sessionRepository.AddSubmissionAsync(submission).ConfigureAwait(false);
            }
            _logger.LogDebug("[RESEARCH-WORKFLOW] Persisted {Count} submissions to database", submissions.Count);

            // Store submissions in memory for subsequent phases
            session.Submissions = submissions;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

            // Phase 3: Anonymization
            _logger.LogDebug("[RESEARCH-WORKFLOW] === PHASE 3: ANONYMIZATION ===");
            session.Status = ResearchStatus.Anonymizing;
            session.CurrentPhase = ResearchPhase.Anonymization;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting status change: Anonymizing");
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id, ResearchPhase.Anonymization, 0, "Anonymizing submissions for peer review...").ConfigureAwait(false);

            _logger.LogDebug("[RESEARCH-WORKFLOW] Calling ExecuteAnonymizationPhaseAsync...");
            var anonymizedSubmissions = await phaseExecutor.ExecuteAnonymizationPhaseAsync(
                session,
                submissions,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Anonymization phase completed: {AnonymizedCount} anonymized", anonymizedSubmissions.Count);

            // Update submissions with AnonymizedId
            foreach (var submission in submissions)
            {
                await _sessionRepository.UpdateSubmissionAsync(submission).ConfigureAwait(false);
            }
            _logger.LogDebug("[RESEARCH-WORKFLOW] Updated {Count} submissions with anonymized IDs", submissions.Count);

            // Phase 4: Peer Review
            _logger.LogDebug("[RESEARCH-WORKFLOW] === PHASE 4: PEER REVIEW ===");
            session.Status = ResearchStatus.PeerReviewing;
            session.CurrentPhase = ResearchPhase.PeerReview;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting status change: PeerReviewing");
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id, ResearchPhase.PeerReview, 0, "Agents are reviewing each other's work...").ConfigureAwait(false);

            _logger.LogDebug("[RESEARCH-WORKFLOW] Calling ExecutePeerReviewPhaseAsync...");
            var peerReviews = await phaseExecutor.ExecutePeerReviewPhaseAsync(
                session,
                nonChairmanAgents,
                anonymizedSubmissions,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Peer review phase completed: {ReviewCount} reviews", peerReviews.Count);

            // Persist peer reviews to database
            foreach (var review in peerReviews)
            {
                await _sessionRepository.AddPeerReviewAsync(review).ConfigureAwait(false);
            }
            _logger.LogDebug("[RESEARCH-WORKFLOW] Persisted {Count} peer reviews to database", peerReviews.Count);

            // Store peer reviews in memory for subsequent phases
            session.PeerReviews = peerReviews;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

            // Phase 5: Synthesis
            _logger.LogDebug("[RESEARCH-WORKFLOW] === PHASE 5: SYNTHESIS ===");
            session.Status = ResearchStatus.Synthesizing;
            session.CurrentPhase = ResearchPhase.Synthesis;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting status change: Synthesizing");
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id, ResearchPhase.Synthesis, 0, "Chairman is synthesizing the final report...").ConfigureAwait(false);

            // Get Chairman agent for synthesis
            var chairman = researchAgents.FirstOrDefault(a => a.IsChairman);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Chairman for synthesis: {ChairmanFound}", chairman != null ? "Found" : "Creating default");
            if (chairman == null)
            {
                // Create a default Chairman if none configured
                _logger.LogWarning("[RESEARCH-WORKFLOW] No Chairman agent configured, using default");
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
            else
            {
                _logger.LogDebug("[RESEARCH-WORKFLOW] Chairman InferenceEngineId: {EngineId}", chairman.InferenceEngineId);
            }

            _logger.LogDebug("[RESEARCH-WORKFLOW] Calling ExecuteSynthesisPhaseAsync...");
            var report = await phaseExecutor.ExecuteSynthesisPhaseAsync(
                session,
                chairman,
                cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Synthesis phase completed: Title={Title}, SummaryLength={Length}",
                report.Title, report.ExecutiveSummary?.Length ?? 0);

            // Save report to database
            report.SessionId = session.Id;
            await _sessionRepository.UpsertReportAsync(report).ConfigureAwait(false);
            _logger.LogDebug("[RESEARCH-WORKFLOW] Report saved to database: {ReportId}", report.Id);

            // Store report reference in session
            session.Report = report;

            // Mark as complete
            _logger.LogDebug("[RESEARCH-WORKFLOW] Marking session as completed");
            session.Status = ResearchStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

            _logger.LogInformation(
                "Research session {SessionId} completed with {SubmissionCount} submissions, {ReviewCount} reviews, and report '{Title}'",
                session.Id, submissions.Count, peerReviews.Count, report.Title);

            // Broadcast completion event
            _logger.LogDebug("[RESEARCH-WORKFLOW] Broadcasting completion");
            await _progressBroadcaster.BroadcastCompletionAsync(session.Id, report.Id).ConfigureAwait(false);

            // Update ChatSession with synthesized title and add the research report
            if (session.ChatSessionId.HasValue)
            {
                // Update the placeholder title with the final synthesized title
                await UpdateChatSessionTitleAsync(session.ChatSessionId.Value, report.Title).ConfigureAwait(false);

                // Add the research report as a message
                await AddReportToChatSessionAsync(session, report).ConfigureAwait(false);
            }

            _logger.LogDebug("[RESEARCH-WORKFLOW] === ExecuteResearchWorkflowAsync COMPLETE ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Research workflow failed for session {SessionId}", session.Id);

            session.Status = ResearchStatus.Failed;
            session.ErrorMessage = ex.Message;
            session.CompletedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session).ConfigureAwait(false);

            // Broadcast error event
            await _progressBroadcaster.BroadcastErrorAsync(session.Id, ex.Message).ConfigureAwait(false);

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
        _logger.LogDebug("[RESEARCH-RESOLVE] === ResolveBaseAgentsAsync START ===");
        _logger.LogDebug("[RESEARCH-RESOLVE] Team: {TeamName} ({TeamId})", team.Name, team.Id);
        _logger.LogDebug("[RESEARCH-RESOLVE] Members to resolve: {MemberCount}", team.Members?.Count ?? 0);

        var agentDict = new Dictionary<Guid, AesirAgentBase>();

        foreach (var member in team.Members ?? [])
        {
            _logger.LogDebug("[RESEARCH-RESOLVE] Processing member: Role={Role}, AgentId={AgentId}",
                member.Role, member.AgentId);

            try
            {
                var baseAgent = await _configurationService.GetAgentAsync(member.AgentId).ConfigureAwait(false);
                agentDict[member.AgentId] = baseAgent;
                _logger.LogDebug("[RESEARCH-RESOLVE] SUCCESS - Agent {AgentId} resolved:", member.AgentId);
                _logger.LogDebug("[RESEARCH-RESOLVE]   Name: {Name}", baseAgent.Name);
                _logger.LogDebug("[RESEARCH-RESOLVE]   ChatModel: {ChatModel}", baseAgent.ChatModel);
                _logger.LogDebug("[RESEARCH-RESOLVE]   ChatInferenceEngineId: {EngineId}", baseAgent.ChatInferenceEngineId);
                _logger.LogDebug("[RESEARCH-RESOLVE]   ChatPromptPersona: {Persona}", baseAgent.ChatPromptPersona);
                _logger.LogDebug("[RESEARCH-RESOLVE]   CustomPromptLength: {Length}", baseAgent.ChatCustomPromptContent?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[RESEARCH-RESOLVE] FAILED to resolve agent {AgentId} for role {Role}, using fallback",
                    member.AgentId, member.Role);

                // Fallback to basic configuration
                agentDict[member.AgentId] = new AesirAgentBase
                {
                    Id = member.AgentId,
                    Name = $"Agent-{member.Role}",
                    ChatModel = "gpt-4",
                    ChatTemperature = 0.7
                };
                _logger.LogDebug("[RESEARCH-RESOLVE] Created fallback agent for {Role}", member.Role);
            }
        }

        _logger.LogInformation("[RESEARCH-RESOLVE] Resolved {Count} agents for research team {TeamId}",
            agentDict.Count, team.Id);
        _logger.LogDebug("[RESEARCH-RESOLVE] === ResolveBaseAgentsAsync COMPLETE ===");

        return agentDict;
    }

    /// <summary>
    /// Generates a placeholder title from the research query.
    /// </summary>
    private static string GenerateInitialTitle(string query)
    {
        var cleanQuery = query.Trim();
        if (cleanQuery.Length > 50)
            cleanQuery = cleanQuery[..47] + "...";
        return $"Research: {cleanQuery}";
    }

    /// <summary>
    /// Initializes a ChatSession for a research session immediately.
    /// This ensures the user's query is persisted and the session appears in chat history right away.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="query">The original research query.</param>
    /// <param name="userId">The user ID.</param>
    /// <returns>The ID of the created/updated ChatSession.</returns>
    private async Task<Guid> InitializeChatSessionAsync(ResearchSession session, string query, string userId)
    {
        var chatSessionId = session.ChatSessionId ?? Guid.NewGuid();

        _logger.LogDebug("[RESEARCH-CHAT] Initializing ChatSession {ChatSessionId} for research session {SessionId}",
            chatSessionId, session.Id);

        // Generate placeholder title from query
        var title = GenerateInitialTitle(query);

        // Create user message with original query
        var userMessage = AesirChatMessage.NewUserMessage(query);

        // Create ChatSession with user's query as first message
        var chatSession = new ResearchChatSession
        {
            Id = chatSessionId,
            UserId = userId,
            Title = title,
            Conversation = new AesirConversation
            {
                Id = Guid.NewGuid().ToString(),
                Messages = [userMessage]
            },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Persist immediately so it appears in chat history
        await _chatHistoryService.UpsertChatSessionAsync(chatSession).ConfigureAwait(false);

        _logger.LogInformation("[RESEARCH-CHAT] ChatSession {ChatSessionId} created with title '{Title}'",
            chatSessionId, title);

        return chatSessionId;
    }

    /// <summary>
    /// Updates the ChatSession title with the final synthesized title from the report.
    /// </summary>
    /// <param name="chatSessionId">The ChatSession ID.</param>
    /// <param name="newTitle">The new title from the research report.</param>
    private async Task UpdateChatSessionTitleAsync(Guid chatSessionId, string newTitle)
    {
        try
        {
            var chatSession = await _chatHistoryService.GetChatSessionAsync(chatSessionId).ConfigureAwait(false);
            if (chatSession != null)
            {
                chatSession.Title = newTitle;
                chatSession.UpdatedAt = DateTimeOffset.UtcNow;
                await _chatHistoryService.UpsertChatSessionAsync(chatSession).ConfigureAwait(false);

                _logger.LogDebug("[RESEARCH-CHAT] Updated ChatSession {ChatSessionId} title to '{Title}'",
                    chatSessionId, newTitle);
            }
            else
            {
                _logger.LogWarning("[RESEARCH-CHAT] ChatSession {ChatSessionId} not found when updating title",
                    chatSessionId);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the research if title update fails
            _logger.LogError(ex, "[RESEARCH-CHAT] Failed to update ChatSession {ChatSessionId} title", chatSessionId);
        }
    }

    /// <summary>
    /// Adds the research report as an assistant message to the linked ChatSession.
    /// This enables:
    /// - Persistence in chat history sidebar
    /// - Ability to continue conversation after research completes
    /// - Normal agents can see the research report as previous message
    /// </summary>
    private async Task AddReportToChatSessionAsync(ResearchSession session, ResearchReport report)
    {
        if (!session.ChatSessionId.HasValue)
        {
            _logger.LogDebug("[RESEARCH-CHAT] No ChatSessionId, skipping ChatSession update");
            return;
        }

        try
        {
            _logger.LogDebug("[RESEARCH-CHAT] Adding report to ChatSession {ChatSessionId}", session.ChatSessionId);

            // Get the existing ChatSession - it should already exist from InitializeChatSessionAsync
            var chatSession = await _chatHistoryService.GetChatSessionAsync(session.ChatSessionId.Value).ConfigureAwait(false);
            if (chatSession == null)
            {
                // This shouldn't happen - ChatSession should have been created at research start
                _logger.LogWarning("[RESEARCH-CHAT] ChatSession {ChatSessionId} not found when adding report - " +
                    "this indicates the session wasn't properly initialized", session.ChatSessionId);
                return;
            }

            // Create research team message with the report content
            var reportMessage = AesirChatMessage.NewResearchTeamMessage(
                report.FullMarkdown ?? report.ExecutiveSummary ?? "Research completed.",
                session.Id,
                session.ResearchTeamId ?? Guid.Empty,
                "Research Team");

            // Add to conversation
            chatSession.Conversation ??= new AesirConversation { Id = session.ChatSessionId.Value.ToString() };
            chatSession.Conversation.Messages ??= new List<AesirChatMessage>();
            chatSession.Conversation.Messages.Add(reportMessage);
            chatSession.UpdatedAt = DateTimeOffset.UtcNow;

            // Persist the updated ChatSession
            await _chatHistoryService.UpsertChatSessionAsync(chatSession).ConfigureAwait(false);

            _logger.LogInformation("[RESEARCH-CHAT] Successfully added report to ChatSession {ChatSessionId}",
                session.ChatSessionId);
        }
        catch (Exception ex)
        {
            // Don't fail the research session if ChatSession update fails
            _logger.LogError(ex, "[RESEARCH-CHAT] Failed to add report to ChatSession {ChatSessionId}",
                session.ChatSessionId);
        }
    }
}

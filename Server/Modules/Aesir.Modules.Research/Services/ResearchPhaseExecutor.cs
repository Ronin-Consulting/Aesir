using System.Text;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Constants;
using Aesir.Modules.Research.Execution;
using Aesir.Modules.Research.Hubs;
using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Executes research phases (planning, research, anonymization, peer review) for agents.
/// </summary>
public interface IResearchPhaseExecutor
{
    /// <summary>
    /// Executes the planning phase using Chairman's unified planning.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="chairman">The Chairman agent.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="refinedQuery">The refined research query.</param>
    /// <param name="priorHistory">Optional prior conversation history to provide context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of agent plans keyed by team member ID.</returns>
    Task<Dictionary<Guid, string>> ExecutePlanningPhaseAsync(
        ResearchSession session,
        ResearchAgent chairman,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        IReadOnlyList<AesirChatMessage>? priorHistory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the research phase for all agents in parallel.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="refinedQuery">The refined research query.</param>
    /// <param name="agentPlans">The planning phase results.</param>
    /// <param name="priorHistory">Optional prior conversation history to provide context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of research submissions.</returns>
    Task<List<ResearchSubmission>> ExecuteResearchPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Dictionary<Guid, string> agentPlans,
        IReadOnlyList<AesirChatMessage>? priorHistory = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the anonymization phase to prepare submissions for peer review.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="submissions">The research submissions to anonymize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of anonymized submissions keyed by anonymous ID.</returns>
    Task<Dictionary<string, AnonymizedSubmission>> ExecuteAnonymizationPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the peer review phase where agents review each other's submissions.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="anonymizedSubmissions">The anonymized submissions to review.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of peer reviews with scores.</returns>
    Task<List<PeerReview>> ExecutePeerReviewPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the synthesis phase where the Chairman generates the final report.
    /// </summary>
    /// <param name="session">The research session with submissions and reviews.</param>
    /// <param name="chairmanAgent">The Chairman agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated research report.</returns>
    Task<ResearchReport> ExecuteSynthesisPhaseAsync(
        ResearchSession session,
        ResearchAgent chairmanAgent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Progress update for research phases.
/// </summary>
public class ResearchPhaseProgress
{
    /// <summary>
    /// The current phase.
    /// </summary>
    public ResearchPhase Phase { get; set; }

    /// <summary>
    /// The agent that made progress.
    /// </summary>
    public ResearchRole? AgentRole { get; set; }

    /// <summary>
    /// The team member ID.
    /// </summary>
    public Guid? TeamMemberId { get; set; }

    /// <summary>
    /// Progress message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Completion percentage (0-100).
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Whether this agent has completed.
    /// </summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// Implementation of the research phase executor.
/// Uses IChatService with streaming to perform real-time LLM-based research,
/// broadcasting progress updates via SignalR.
/// </summary>
public class ResearchPhaseExecutor : IResearchPhaseExecutor
{
    private readonly ILogger<ResearchPhaseExecutor> _logger;
    private readonly IChatServiceResolver _chatServiceResolver;
    private readonly IChatRequestBuilder _chatRequestBuilder;
    private readonly IHubContext<ResearchHub> _hubContext;
    private readonly IAnonymizationService _anonymizationService;
    private readonly IPeerReviewService _peerReviewService;
    private readonly IReportGeneratorService _reportGeneratorService;
    private readonly IScoringCalculator _scoringCalculator;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly IChairmanPlanningService _chairmanPlanningService;
    private readonly IPhaseExecutionStrategyFactory _strategyFactory;

    public ResearchPhaseExecutor(
        ILogger<ResearchPhaseExecutor> logger,
        IChatServiceResolver chatServiceResolver,
        IChatRequestBuilder chatRequestBuilder,
        IHubContext<ResearchHub> hubContext,
        IAnonymizationService anonymizationService,
        IPeerReviewService peerReviewService,
        IReportGeneratorService reportGeneratorService,
        IScoringCalculator scoringCalculator,
        IResearchProgressBroadcaster progressBroadcaster,
        IChairmanPlanningService chairmanPlanningService,
        IPhaseExecutionStrategyFactory strategyFactory)
    {
        _logger = logger;
        _chatServiceResolver = chatServiceResolver;
        _chatRequestBuilder = chatRequestBuilder;
        _hubContext = hubContext;
        _anonymizationService = anonymizationService;
        _peerReviewService = peerReviewService;
        _reportGeneratorService = reportGeneratorService;
        _scoringCalculator = scoringCalculator;
        _progressBroadcaster = progressBroadcaster;
        _chairmanPlanningService = chairmanPlanningService;
        _strategyFactory = strategyFactory;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, string>> ExecutePlanningPhaseAsync(
        ResearchSession session,
        ResearchAgent chairman,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        IReadOnlyList<AesirChatMessage>? priorHistory = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting planning phase for session {SessionId} - Chairman creating unified plan for {Count} agents",
            session.Id, agents.Count);

        // Chairman creates ONE unified plan for all agents
        var plans = await _chairmanPlanningService.CreateUnifiedPlanAsync(
            session, chairman, agents, refinedQuery, priorHistory, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Planning phase complete. Chairman created {Count} agent plans",
            plans.Count);

        return plans;
    }

    /// <inheritdoc />
    public async Task<List<ResearchSubmission>> ExecuteResearchPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Dictionary<Guid, string> agentPlans,
        IReadOnlyList<AesirChatMessage>? priorHistory = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting research phase for session {SessionId} with {Count} agents (parallel execution: max 2 concurrent)",
            session.Id, agents.Count);

        var researchAgents = agents.Where(a => !a.IsChairman).ToList();

        // Prepare inputs: pair each agent with their plan
        var inputs = researchAgents
            .Select(agent => (Agent: agent, Plan: agentPlans.TryGetValue(agent.TeamMemberId, out var p) ? p : ""))
            .ToList();

        // Get the research execution strategy
        var strategy = _strategyFactory.CreateResearchStrategy();

        // Execute research phase with agent tracking for real-time progress
        var results = await strategy.ExecutePhaseWithAgentTrackingAsync(
            session,
            inputs,
            async (sess, input, ct) =>
            {
                var submission = await ExecuteAgentResearchAsync(
                    sess, input.Agent, refinedQuery, input.Plan, priorHistory, ct).ConfigureAwait(false);
                return submission ?? CreateFailedSubmission(sess, input.Agent, input.Plan);
            },
            input => new Contracts.ActiveAgentInfo
            {
                TeamMemberId = input.Agent.TeamMemberId,
                Role = input.Agent.Role,
                RoleName = input.Agent.RoleName,
                Activity = $"{input.Agent.RoleName} is researching...",
                AgentProgressPercent = null
            },
            cancellationToken).ConfigureAwait(false);

        // Final completion broadcast (strategy already sends 100% but we add the summary message)
        await _progressBroadcaster.BroadcastProgressAsync(
            session.Id,
            ResearchPhase.Research,
            100,
            $"Research complete. {results.Count(r => r.Status == SubmissionStatus.Completed)} of {results.Count} submissions successful.").ConfigureAwait(false);

        _logger.LogInformation(
            "Research phase complete. {Successful}/{Total} submissions successful",
            results.Count(r => r.Status == SubmissionStatus.Completed), results.Count);

        return results.ToList();
    }

    /// <summary>
    /// Executes research for a single agent using non-streaming chat completion.
    /// </summary>
    private async Task<ResearchSubmission?> ExecuteAgentResearchAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        string plan,
        IReadOnlyList<AesirChatMessage>? priorHistory,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Agent {Role} starting research for session {SessionId}",
            agent.Role, session.Id);

        // Get the chat service for this agent's inference engine
        var chatService = _chatServiceResolver.GetChatService(agent.InferenceEngineId);
        if (chatService == null)
        {
            _logger.LogWarning(
                "No chat service found for agent {Role} with inference engine {EngineId}",
                agent.Role, agent.InferenceEngineId);
            return CreateFailedSubmission(session, agent, plan);
        }

        try
        {
            // Agent tracking is handled by the execution strategy (ParallelPhaseExecutionStrategy)
            // which broadcasts active agents via IResearchProgressBroadcaster

            // Build context from plan and any clarification answers
            var refinedContext = BuildResearchContext(session, plan);

            // Build the research prompt
            var researchPrompt = agent.ResearchPrompt?
                .Replace("{{QUERY}}", refinedQuery)
                .Replace("{{REFINED_CONTEXT}}", refinedContext)
                ?? $"Research the following query: {refinedQuery}\n\nContext:\n{refinedContext}";

            // Create the chat request using the builder
            var request = await _chatRequestBuilder.BuildAsync(
                agent,
                agent.Persona,
                researchPrompt,
                new ChatRequestOptions
                {
                    IncludeTools = true,
                    User = "research-orchestrator",
                    Title = $"Research: {agent.RoleName}",
                    PriorConversationHistory = priorHistory,
                    ChatSessionId = session.ChatSessionId
                }).ConfigureAwait(false);

            // Execute non-streaming LLM call
            var result = await chatService.ChatCompletionsAsync(request).ConfigureAwait(false);

            // Extract the assistant's response from the conversation
            var content = ExtractAssistantResponse(result);

            // Create submission
            var submission = new ResearchSubmission
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                AgentId = agent.BaseAgentId,
                Role = agent.Role,
                Plan = plan,
                Content = content,
                Status = string.IsNullOrEmpty(content) ? SubmissionStatus.Failed : SubmissionStatus.Completed,
                Sources = [],
                ToolCalls = [],
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Agent {Role} completed research: {Status}, {Length} characters",
                agent.Role, submission.Status, content.Length);

            return submission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during research for agent {Role}", agent.Role);

            // Error handling is done by the execution strategy which will:
            // 1. Update the agent's activity to show the error
            // 2. Broadcast the updated active agents list
            // 3. Continue with other agents (StopOnFirstError = false)

            return null;
        }
    }

    /// <summary>
    /// Extracts the assistant's response content from a chat result.
    /// </summary>
    private static string ExtractAssistantResponse(AesirChatResult result)
    {
        // Get the last assistant message from the conversation
        var assistantMessage = result.AesirConversation?.Messages?
            .LastOrDefault(m => m.Role == "assistant");

        return assistantMessage?.Content ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, AnonymizedSubmission>> ExecuteAnonymizationPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        CancellationToken cancellationToken = default)
    {
        var representativeRole = submissions.FirstOrDefault()?.Role ?? ResearchRole.DeepDiver;

        return await ExecutePhaseWithProgressAsync(
            session.Id,
            ResearchPhase.Anonymization,
            representativeRole,
            "Anonymizing submissions for peer review...",
            async () =>
            {
                var anonymizationResult = await _anonymizationService.AnonymizeSubmissionsAsync(submissions).ConfigureAwait(false);

                // Update submission records with anonymized IDs
                foreach (var (anonymizedId, submission) in anonymizationResult.Submissions)
                {
                    var original = submissions.FirstOrDefault(s => s.Id == submission.OriginalSubmissionId);
                    if (original != null)
                    {
                        original.AnonymizedId = anonymizedId;
                    }
                }

                // Convert to mutable dictionary for downstream compatibility
                var anonymized = new Dictionary<string, AnonymizedSubmission>(anonymizationResult.Submissions);
                return (anonymized, $"Anonymization complete. {anonymized.Count} submissions ready for review.");
            }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<PeerReview>> ExecutePeerReviewPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting peer review phase for session {SessionId}", session.Id);

        // Get the first non-chairman agent for representative role
        var reviewingAgents = agents.Where(a => !a.IsChairman).ToList();
        var firstAgent = reviewingAgents.FirstOrDefault();

        // Report start
        if (firstAgent != null)
        {
            await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = firstAgent.Role,
                Message = "Starting peer review process...",
                PercentComplete = ResearchProgressMilestones.PhaseStart
            }).ConfigureAwait(false);
        }

        // Conduct peer reviews
        var reviews = await _peerReviewService.ConductPeerReviewsAsync(
            session,
            agents,
            anonymizedSubmissions,
            cancellationToken).ConfigureAwait(false);

        // Report completion
        var lastAgent = reviewingAgents.LastOrDefault();
        if (lastAgent != null)
        {
            await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = lastAgent.Role,
                Message = $"Peer review complete. {reviews.Count} reviews generated.",
                PercentComplete = ResearchProgressMilestones.PhaseComplete,
                IsComplete = true
            }).ConfigureAwait(false);
        }

        _logger.LogInformation("Peer review phase complete. {Count} reviews generated", reviews.Count);

        return reviews;
    }

    /// <inheritdoc />
    public async Task<ResearchReport> ExecuteSynthesisPhaseAsync(
        ResearchSession session,
        ResearchAgent chairmanAgent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting synthesis phase for session {SessionId}", session.Id);

        // Report start
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = ResearchRole.Chairman,
            Message = "Chairman is synthesizing the final report...",
            PercentComplete = ResearchProgressMilestones.PhaseStart
        }).ConfigureAwait(false);

        // Calculate submission scores from peer reviews
        var submissionScores = CalculateSubmissionScores(session);

        // Report progress
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = ResearchRole.Chairman,
            Message = "Analyzing peer review scores...",
            PercentComplete = ResearchProgressMilestones.PromptBuilt
        }).ConfigureAwait(false);

        // Generate the report
        var report = await _reportGeneratorService.GenerateReportAsync(
            session,
            submissionScores,
            chairmanAgent,
            cancellationToken).ConfigureAwait(false);

        // Report completion
        await _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
        {
            Phase = ResearchPhase.Synthesis,
            AgentRole = ResearchRole.Chairman,
            Message = $"Report generated: {report.Title}",
            PercentComplete = ResearchProgressMilestones.PhaseComplete,
            IsComplete = true
        }).ConfigureAwait(false);

        _logger.LogInformation(
            "Synthesis phase complete. Report '{Title}' generated with {FindingCount} findings",
            report.Title, report.Findings?.Count ?? 0);

        return report;
    }

    private List<SubmissionScore> CalculateSubmissionScores(ResearchSession session)
    {
        var scores = new List<SubmissionScore>();
        var submissions = session.Submissions ?? [];
        var peerReviews = session.PeerReviews ?? [];

        foreach (var submission in submissions)
        {
            var reviewsForSubmission = peerReviews
                .Where(r => r.SubmissionId == submission.Id)
                .ToList();

            if (reviewsForSubmission.Count > 0)
            {
                var score = _scoringCalculator.CalculateAggregateScore(reviewsForSubmission);
                score.SubmissionId = submission.Id;
                scores.Add(score);
            }
            else
            {
                // No reviews - assign default score
                scores.Add(new SubmissionScore
                {
                    SubmissionId = submission.Id,
                    AverageScore = 5.0,
                    MedianScore = 5.0,
                    MinScore = 5.0,
                    MaxScore = 5.0,
                    StandardDeviation = 0,
                    ReviewCount = 0,
                    EndorsementCount = 0,
                    Confidence = ConfidenceLevel.Low
                });
            }
        }

        return scores;
    }

    /// <summary>
    /// Builds the research context from the session's plan and clarification answers.
    /// </summary>
    private static string BuildResearchContext(ResearchSession session, string plan)
    {
        var context = new StringBuilder();

        // Add the research plan
        if (!string.IsNullOrEmpty(plan))
        {
            context.AppendLine("## Your Research Plan");
            context.AppendLine(plan);
            context.AppendLine();
        }

        // Add any clarification answers
        if (session.ClarificationAnswers?.Count > 0)
        {
            context.AppendLine("## Clarifications Provided by User");
            foreach (var (question, answer) in session.ClarificationAnswers)
            {
                context.AppendLine($"Q: {question}");
                context.AppendLine($"A: {answer}");
                context.AppendLine();
            }
        }

        return context.ToString();
    }

    /// <summary>
    /// Creates a failed submission record when LLM call fails.
    /// </summary>
    private static ResearchSubmission CreateFailedSubmission(
        ResearchSession session,
        ResearchAgent agent,
        string plan)
    {
        return new ResearchSubmission
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            AgentId = agent.BaseAgentId,
            Role = agent.Role,
            Plan = plan,
            Content = string.Empty,
            Status = SubmissionStatus.Failed,
            Sources = [],
            ToolCalls = [],
            CreatedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Helper method to execute a phase with automatic progress reporting.
    /// Eliminates duplication of start/complete progress broadcast pattern.
    /// </summary>
    private async Task<TResult> ExecutePhaseWithProgressAsync<TResult>(
        Guid sessionId,
        ResearchPhase phase,
        ResearchRole role,
        string startMessage,
        Func<Task<(TResult Result, string CompleteMessage)>> phaseWork)
    {
        // Report phase start
        await _progressBroadcaster.BroadcastProgressAsync(sessionId, new ResearchPhaseProgress
        {
            Phase = phase,
            AgentRole = role,
            Message = startMessage,
            PercentComplete = ResearchProgressMilestones.PhaseStart
        }).ConfigureAwait(false);

        // Execute phase work
        var (result, completeMessage) = await phaseWork().ConfigureAwait(false);

        // Report phase completion
        await _progressBroadcaster.BroadcastProgressAsync(sessionId, new ResearchPhaseProgress
        {
            Phase = phase,
            AgentRole = role,
            Message = completeMessage,
            PercentComplete = ResearchProgressMilestones.PhaseComplete,
            IsComplete = true
        }).ConfigureAwait(false);

        return result;
    }
}

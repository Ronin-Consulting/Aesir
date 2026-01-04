using System.Text;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Hubs;
using Aesir.Modules.Research.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Executes research phases (planning, research, anonymization, peer review) for agents.
/// </summary>
public interface IResearchPhaseExecutor
{
    /// <summary>
    /// Executes the planning phase for all research agents.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="refinedQuery">The refined research query.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of agent plans keyed by team member ID.</returns>
    Task<Dictionary<Guid, string>> ExecutePlanningPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the research phase for all agents in parallel.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="refinedQuery">The refined research query.</param>
    /// <param name="agentPlans">The planning phase results.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of research submissions.</returns>
    Task<List<ResearchSubmission>> ExecuteResearchPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Dictionary<Guid, string> agentPlans,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the anonymization phase to prepare submissions for peer review.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="submissions">The research submissions to anonymize.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of anonymized submissions keyed by anonymous ID.</returns>
    Task<Dictionary<string, AnonymizedSubmission>> ExecuteAnonymizationPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchSubmission> submissions,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the peer review phase where agents review each other's submissions.
    /// </summary>
    /// <param name="session">The research session.</param>
    /// <param name="agents">The research agents (excluding Chairman).</param>
    /// <param name="anonymizedSubmissions">The anonymized submissions to review.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of peer reviews with scores.</returns>
    Task<List<PeerReview>> ExecutePeerReviewPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the synthesis phase where the Chairman generates the final report.
    /// </summary>
    /// <param name="session">The research session with submissions and reviews.</param>
    /// <param name="chairmanAgent">The Chairman agent.</param>
    /// <param name="progressCallback">Optional callback for progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated research report.</returns>
    Task<ResearchReport> ExecuteSynthesisPhaseAsync(
        ResearchSession session,
        ResearchAgent chairmanAgent,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ResearchHub> _hubContext;
    private readonly IConfigurationService _configurationService;
    private readonly IAnonymizationService _anonymizationService;
    private readonly IPeerReviewService _peerReviewService;
    private readonly IReportGeneratorService _reportGeneratorService;
    private readonly IScoringCalculator _scoringCalculator;

    public ResearchPhaseExecutor(
        ILogger<ResearchPhaseExecutor> logger,
        IServiceScopeFactory scopeFactory,
        IHubContext<ResearchHub> hubContext,
        IConfigurationService configurationService,
        IAnonymizationService anonymizationService,
        IPeerReviewService peerReviewService,
        IReportGeneratorService reportGeneratorService,
        IScoringCalculator scoringCalculator)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _configurationService = configurationService;
        _anonymizationService = anonymizationService;
        _peerReviewService = peerReviewService;
        _reportGeneratorService = reportGeneratorService;
        _scoringCalculator = scoringCalculator;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, string>> ExecutePlanningPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting planning phase for session {SessionId} with {Count} agents (sequential, non-streaming)",
            session.Id, agents.Count);

        var plans = new Dictionary<Guid, string>();
        var researchAgents = agents.Where(a => !a.IsChairman).ToList();
        var completedCount = 0;
        var totalCount = researchAgents.Count;

        // Broadcast phase start with 0%
        var firstAgent = researchAgents.FirstOrDefault();
        if (progressCallback != null && firstAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Planning,
                AgentRole = firstAgent.Role,
                Message = $"Starting planning phase with {totalCount} agents...",
                PercentComplete = 0
            });
        }

        // Execute agents sequentially to avoid overloading the inference engine
        ResearchAgent? lastAgent = null;
        foreach (var agent in researchAgents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastAgent = agent;

            var (teamMemberId, plan) = await ExecuteAgentPlanningAsync(
                session, agent, refinedQuery, progressCallback, cancellationToken);

            if (!string.IsNullOrEmpty(plan))
            {
                plans[teamMemberId] = plan;
            }

            // Update progress after each agent completes
            completedCount++;
            if (progressCallback != null)
            {
                var percentComplete = (completedCount * 100) / totalCount;
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Planning,
                    AgentRole = agent.Role,
                    Message = $"Planning: {completedCount} of {totalCount} agents completed",
                    PercentComplete = percentComplete
                });
            }
        }

        // Broadcast phase completion with 100%
        if (progressCallback != null && lastAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Planning,
                AgentRole = lastAgent.Role,
                Message = $"Planning complete. {plans.Count} of {totalCount} agents created plans.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Planning phase complete. {Count} plans generated", plans.Count);

        return plans;
    }

    /// <inheritdoc />
    public async Task<List<ResearchSubmission>> ExecuteResearchPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        string refinedQuery,
        Dictionary<Guid, string> agentPlans,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting research phase for session {SessionId} with {Count} agents (sequential, non-streaming)",
            session.Id, agents.Count);

        var submissions = new List<ResearchSubmission>();
        var researchAgents = agents.Where(a => !a.IsChairman).ToList();
        var completedCount = 0;
        var totalCount = researchAgents.Count;

        // Broadcast phase start with 0%
        var firstAgent = researchAgents.FirstOrDefault();
        if (progressCallback != null && firstAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Research,
                AgentRole = firstAgent.Role,
                Message = $"Starting research phase with {totalCount} agents...",
                PercentComplete = 0
            });
        }

        // Execute agents sequentially to avoid overloading the inference engine
        ResearchAgent? lastAgent = null;
        foreach (var agent in researchAgents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastAgent = agent;

            var plan = agentPlans.TryGetValue(agent.TeamMemberId, out var p) ? p : "";

            var submission = await ExecuteAgentResearchAsync(
                session, agent, refinedQuery, plan, progressCallback, cancellationToken);

            if (submission != null)
            {
                submissions.Add(submission);
            }

            // Update progress after each agent completes
            completedCount++;
            if (progressCallback != null)
            {
                var percentComplete = (completedCount * 100) / totalCount;
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Research,
                    AgentRole = agent.Role,
                    Message = $"Research: {completedCount} of {totalCount} agents completed",
                    PercentComplete = percentComplete
                });
            }
        }

        // Broadcast phase completion with 100%
        if (progressCallback != null && lastAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Research,
                AgentRole = lastAgent.Role,
                Message = $"Research complete. {submissions.Count} of {totalCount} submissions created.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Research phase complete. {Count} submissions created", submissions.Count);

        return submissions;
    }

    /// <summary>
    /// Executes planning for a single agent using non-streaming chat completion.
    /// </summary>
    private async Task<(Guid TeamMemberId, string Plan)> ExecuteAgentPlanningAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PHASE-EXEC-PLANNING] === ExecuteAgentPlanningAsync START ===");
        _logger.LogDebug("[PHASE-EXEC-PLANNING] Agent: {Role} ({RoleName})", agent.Role, agent.RoleName);
        _logger.LogDebug("[PHASE-EXEC-PLANNING] SessionId: {SessionId}", session.Id);

        IServiceScope? serviceScope = null;
        try
        {
            // Notify phase change via SignalR (agent activity indicator)
            // NOTE: We don't send progress callback here because agent-level updates
            // with PercentComplete=0 would reset the overall progress bar.
            // Only phase-level progress (after each agent completes) should update PercentComplete.
            await _hubContext.SendAgentPhaseChangedAsync(
                session.Id, agent.TeamMemberId, agent.Role, "planning",
                $"{agent.RoleName} is creating research plan...");

            // Get the chat service for this agent's inference engine
            var (chatService, scope) = GetChatServiceForAgent(agent);
            serviceScope = scope;
            if (chatService == null)
            {
                _logger.LogWarning("[PHASE-EXEC-PLANNING] No chat service found for agent {Role} with inference engine {EngineId}",
                    agent.Role, agent.InferenceEngineId);
                return (agent.TeamMemberId, "");
            }

            // Build the planning prompt
            var planningPrompt = agent.PlanningPrompt?
                .Replace("{{QUERY}}", refinedQuery)
                ?? $"Create a research plan for: {refinedQuery}";

            // Create the chat request
            var request = await CreateChatRequestAsync(agent, planningPrompt);
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Sending non-streaming planning request to LLM for agent {Role}", agent.Role);

            // Execute non-streaming LLM call
            var result = await chatService.ChatCompletionsAsync(request);

            // Extract the assistant's response from the conversation
            var content = ExtractAssistantResponse(result);

            _logger.LogDebug("[PHASE-EXEC-PLANNING] Agent {Role} created plan with {Length} characters",
                agent.Role, content.Length);

            // NOTE: We don't send agent-level completion callback here because
            // PercentComplete=100 for a single agent would incorrectly set overall progress to 100%.
            // The phase-level progress callback (after this method returns) handles overall progress.

            _logger.LogDebug("[PHASE-EXEC-PLANNING] === ExecuteAgentPlanningAsync COMPLETE ===");
            return (agent.TeamMemberId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PHASE-EXEC-PLANNING] Error during planning for agent {Role}", agent.Role);
            return (agent.TeamMemberId, "");
        }
        finally
        {
            serviceScope?.Dispose();
        }
    }

    /// <summary>
    /// Executes research for a single agent using non-streaming chat completion.
    /// </summary>
    private async Task<ResearchSubmission?> ExecuteAgentResearchAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        string plan,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PHASE-EXEC-RESEARCH] === ExecuteAgentResearchAsync START ===");
        _logger.LogDebug("[PHASE-EXEC-RESEARCH] Agent: {Role} ({RoleName})", agent.Role, agent.RoleName);
        _logger.LogDebug("[PHASE-EXEC-RESEARCH] SessionId: {SessionId}", session.Id);

        IServiceScope? serviceScope = null;
        try
        {
            // Notify phase change via SignalR (agent activity indicator)
            // NOTE: We don't send progress callback here because agent-level updates
            // with PercentComplete=0 would reset the overall progress bar.
            // Only phase-level progress (after each agent completes) should update PercentComplete.
            await _hubContext.SendAgentPhaseChangedAsync(
                session.Id, agent.TeamMemberId, agent.Role, "researching",
                $"{agent.RoleName} is researching...");

            // Get the chat service for this agent's inference engine
            var (chatService, scope) = GetChatServiceForAgent(agent);
            serviceScope = scope;
            if (chatService == null)
            {
                _logger.LogWarning("[PHASE-EXEC-RESEARCH] No chat service found for agent {Role} with inference engine {EngineId}",
                    agent.Role, agent.InferenceEngineId);
                return CreateFailedSubmission(session, agent, plan);
            }

            // Build context from plan and any clarification answers
            var refinedContext = BuildResearchContext(session, plan);

            // Build the research prompt
            var researchPrompt = agent.ResearchPrompt?
                .Replace("{{QUERY}}", refinedQuery)
                .Replace("{{REFINED_CONTEXT}}", refinedContext)
                ?? $"Research the following query: {refinedQuery}\n\nContext:\n{refinedContext}";

            // Create the chat request
            var request = await CreateChatRequestAsync(agent, researchPrompt);
            _logger.LogDebug("[PHASE-EXEC-RESEARCH] Sending non-streaming research request to LLM for agent {Role}", agent.Role);

            // Execute non-streaming LLM call
            var result = await chatService.ChatCompletionsAsync(request);

            // Extract the assistant's response from the conversation
            var content = ExtractAssistantResponse(result);

            // Create submission (tool calls are not tracked in non-streaming mode)
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
                ToolCalls = [], // Tool calls handled internally by ChatCompletionsAsync
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _logger.LogDebug("[PHASE-EXEC-RESEARCH] Agent {Role} created submission with {Length} characters",
                agent.Role, content.Length);

            // NOTE: We don't send agent-level completion callback here because
            // PercentComplete=100 for a single agent would incorrectly set overall progress to 100%.
            // The phase-level progress callback (after this method returns) handles overall progress.

            _logger.LogDebug("[PHASE-EXEC-RESEARCH] === ExecuteAgentResearchAsync COMPLETE ===");
            return submission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PHASE-EXEC-RESEARCH] Error during research for agent {Role}", agent.Role);
            return null;
        }
        finally
        {
            serviceScope?.Dispose();
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
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting anonymization phase for session {SessionId} with {Count} submissions",
            session.Id, submissions.Count);

        // Use the first submission's role as representative for this phase
        var representativeRole = submissions.FirstOrDefault()?.Role ?? ResearchRole.DeepDiver;

        // Report start
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Anonymization,
                AgentRole = representativeRole,
                Message = "Anonymizing submissions for peer review...",
                PercentComplete = 0
            });
        }

        // Perform anonymization
        var anonymized = await _anonymizationService.AnonymizeSubmissionsAsync(submissions);

        // Update submission records with anonymized IDs
        foreach (var (anonymizedId, submission) in anonymized)
        {
            var original = submissions.FirstOrDefault(s => s.Id == submission.OriginalSubmissionId);
            if (original != null)
            {
                original.AnonymizedId = anonymizedId;
            }
        }

        // Report completion
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Anonymization,
                AgentRole = representativeRole,
                Message = $"Anonymization complete. {anonymized.Count} submissions ready for review.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Anonymization phase complete. {Count} submissions anonymized", anonymized.Count);

        return anonymized;
    }

    /// <inheritdoc />
    public async Task<List<PeerReview>> ExecutePeerReviewPhaseAsync(
        ResearchSession session,
        IReadOnlyList<ResearchAgent> agents,
        Dictionary<string, AnonymizedSubmission> anonymizedSubmissions,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting peer review phase for session {SessionId}", session.Id);

        // Get the first non-chairman agent for representative role
        var reviewingAgents = agents.Where(a => !a.IsChairman).ToList();
        var firstAgent = reviewingAgents.FirstOrDefault();

        // Report start
        if (progressCallback != null && firstAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = firstAgent.Role,
                Message = "Starting peer review process...",
                PercentComplete = 0
            });
        }

        // Conduct peer reviews
        var reviews = await _peerReviewService.ConductPeerReviewsAsync(
            session,
            agents,
            anonymizedSubmissions,
            progressCallback,
            cancellationToken);

        // Report completion
        var lastAgent = reviewingAgents.LastOrDefault();
        if (progressCallback != null && lastAgent != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
                AgentRole = lastAgent.Role,
                Message = $"Peer review complete. {reviews.Count} reviews generated.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Peer review phase complete. {Count} reviews generated", reviews.Count);

        return reviews;
    }

    /// <inheritdoc />
    public async Task<ResearchReport> ExecuteSynthesisPhaseAsync(
        ResearchSession session,
        ResearchAgent chairmanAgent,
        Func<ResearchPhaseProgress, Task>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting synthesis phase for session {SessionId}", session.Id);

        // Report start
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Synthesis,
                AgentRole = ResearchRole.Chairman,
                Message = "Chairman is synthesizing the final report...",
                PercentComplete = 0
            });
        }

        // Calculate submission scores from peer reviews
        var submissionScores = CalculateSubmissionScores(session);

        // Report progress
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Synthesis,
                AgentRole = ResearchRole.Chairman,
                Message = "Analyzing peer review scores...",
                PercentComplete = 30
            });
        }

        // Generate the report
        var report = await _reportGeneratorService.GenerateReportAsync(
            session,
            submissionScores,
            chairmanAgent,
            cancellationToken);

        // Report completion
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Synthesis,
                AgentRole = ResearchRole.Chairman,
                Message = $"Report generated: {report.Title}",
                PercentComplete = 100,
                IsComplete = true
            });
        }

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
    /// Resolves the IChatService for a research agent based on its inference engine ID.
    /// Creates a new DI scope that must be disposed by the caller when done using the service.
    /// This is necessary because research runs in a background task after the HTTP request scope is disposed.
    /// </summary>
    /// <returns>A tuple containing the chat service and its scope. Both will be null if service cannot be resolved.
    /// The caller MUST dispose the scope when finished using the service.</returns>
    private (IChatService? Service, IServiceScope? Scope) GetChatServiceForAgent(ResearchAgent agent)
    {
        _logger.LogDebug("[PHASE-EXEC] GetChatServiceForAgent called for {Role}", agent.Role);
        _logger.LogDebug("[PHASE-EXEC]   BaseAgentId: {BaseAgentId}", agent.BaseAgentId);
        _logger.LogDebug("[PHASE-EXEC]   InferenceEngineId: {InferenceEngineId}", agent.InferenceEngineId);
        _logger.LogDebug("[PHASE-EXEC]   Model: {Model}", agent.Model);

        if (!agent.InferenceEngineId.HasValue)
        {
            _logger.LogWarning("[PHASE-EXEC] FAILURE: Agent {Role} has no inference engine ID configured", agent.Role);
            _logger.LogWarning("[PHASE-EXEC]   This agent will NOT be able to perform LLM calls!");
            return (null, null);
        }

        // Create a new scope for this operation - this is critical for background tasks
        // where the original HTTP request scope may be disposed
        var scope = _scopeFactory.CreateScope();
        var engineIdKey = agent.InferenceEngineId.Value.ToString();
        _logger.LogDebug("[PHASE-EXEC] Attempting to get keyed service IChatService with key: '{EngineIdKey}'", engineIdKey);

        var chatService = scope.ServiceProvider.GetKeyedService<IChatService>(engineIdKey);

        if (chatService == null)
        {
            _logger.LogWarning("[PHASE-EXEC] FAILURE: No IChatService found for inference engine ID: {EngineId}", agent.InferenceEngineId);
            _logger.LogWarning("[PHASE-EXEC]   Available keyed services might not include this engine!");
            _logger.LogWarning("[PHASE-EXEC]   Check that the inference engine module registered correctly.");
            scope.Dispose();
            return (null, null);
        }

        _logger.LogDebug("[PHASE-EXEC] SUCCESS: IChatService resolved: {ServiceType}", chatService.GetType().Name);
        return (chatService, scope);
    }

    /// <summary>
    /// Creates a chat request configured for a research agent, including tools and thinking settings.
    /// </summary>
    private async Task<AesirChatRequestBase> CreateChatRequestAsync(ResearchAgent agent, string userPrompt)
    {
        var systemMessage = new AesirChatMessage
        {
            Role = "system",
            Content = agent.Persona,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var userMessage = new AesirChatMessage
        {
            Role = "user",
            Content = userPrompt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var conversation = new AesirConversation
        {
            Id = Guid.NewGuid().ToString(),
            Messages = [systemMessage, userMessage]
        };

        // Load tools and thinking settings from the base agent
        var tools = new List<ToolRequest>();
        bool? enableThinking = null;
        ThinkValue? thinkValue = null;

        try
        {
            // Get base agent configuration for tools and thinking settings
            var baseAgent = await _configurationService.GetAgentAsync(agent.BaseAgentId);

            // Get thinking settings from base agent
            enableThinking = baseAgent.AllowThinking;
            thinkValue = baseAgent.ThinkValue;

            // Load tools configured for this agent
            var agentTools = await _configurationService.GetToolsUsedByAgentAsync(agent.BaseAgentId);
            var mcpServers = await _configurationService.GetMcpServersAsync();

            foreach (var tool in agentTools)
            {
                string? mcpServerName = null;

                // Check if this is an MCP server tool
                if (tool.McpServerId.HasValue)
                {
                    var mcpServer = mcpServers.FirstOrDefault(m => m.Id == tool.McpServerId);
                    if (mcpServer != null)
                    {
                        mcpServerName = mcpServer.Name;
                    }
                }

                tools.Add(new ToolRequest
                {
                    ToolName = tool.ToolName ?? "",
                    McpServerName = mcpServerName
                });
            }

            _logger.LogDebug("Loaded {ToolCount} tools for agent {Role}, thinking={EnableThinking}",
                tools.Count, agent.Role, enableThinking);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration for agent {Role}", agent.Role);
        }

        return new AesirChatRequestBase
        {
            Model = agent.Model ?? "gpt-4",
            Temperature = agent.Temperature,
            MaxTokens = agent.MaxTokens ?? 8192,
            Conversation = conversation,
            User = "research-orchestrator",
            Title = $"Research: {agent.RoleName}",
            Tools = tools,
            EnableThinking = enableThinking,
            ThinkValue = thinkValue
        };
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
}

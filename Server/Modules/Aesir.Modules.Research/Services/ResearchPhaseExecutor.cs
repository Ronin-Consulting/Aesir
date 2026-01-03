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
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ResearchHub> _hubContext;
    private readonly IConfigurationService _configurationService;
    private readonly IAnonymizationService _anonymizationService;
    private readonly IPeerReviewService _peerReviewService;
    private readonly IReportGeneratorService _reportGeneratorService;
    private readonly IScoringCalculator _scoringCalculator;

    public ResearchPhaseExecutor(
        ILogger<ResearchPhaseExecutor> logger,
        IServiceProvider serviceProvider,
        IHubContext<ResearchHub> hubContext,
        IConfigurationService configurationService,
        IAnonymizationService anonymizationService,
        IPeerReviewService peerReviewService,
        IReportGeneratorService reportGeneratorService,
        IScoringCalculator scoringCalculator)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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
        _logger.LogInformation("Starting planning phase for session {SessionId} with {Count} agents",
            session.Id, agents.Count);

        var plans = new Dictionary<Guid, string>();
        var researchAgents = agents.Where(a => !a.IsChairman).ToList();
        var completedCount = 0;
        var totalCount = researchAgents.Count;

        // Broadcast phase start with 0%
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Planning,
                Message = $"Starting planning phase with {totalCount} agents...",
                PercentComplete = 0
            });
        }

        // Track completion with progress updates
        var completionLock = new object();
        var tasks = new List<Task<(Guid TeamMemberId, string Plan)>>();

        foreach (var agent in researchAgents)
        {
            tasks.Add(ExecuteAgentPlanningWithProgressAsync(
                session, agent, refinedQuery, progressCallback,
                () =>
                {
                    int completed;
                    lock (completionLock)
                    {
                        completedCount++;
                        completed = completedCount;
                    }
                    return (completed, totalCount);
                },
                cancellationToken));
        }

        // Wait for all planning to complete
        var results = await Task.WhenAll(tasks);

        foreach (var (teamMemberId, plan) in results)
        {
            if (!string.IsNullOrEmpty(plan))
            {
                plans[teamMemberId] = plan;
            }
        }

        // Broadcast phase completion with 100%
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Planning,
                Message = $"Planning complete. {plans.Count} of {totalCount} agents created plans.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Planning phase complete. {Count} plans generated", plans.Count);

        return plans;
    }

    /// <summary>
    /// Wrapper that executes agent planning and broadcasts overall progress on completion.
    /// </summary>
    private async Task<(Guid TeamMemberId, string Plan)> ExecuteAgentPlanningWithProgressAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        Func<(int completed, int total)> getProgress,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAgentPlanningStreamedAsync(
            session, agent, refinedQuery, progressCallback, cancellationToken);

        // Broadcast overall progress after this agent completes
        if (progressCallback != null)
        {
            var (completed, total) = getProgress();
            var percentComplete = (completed * 100) / total;

            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Planning,
                Message = $"Planning: {completed} of {total} agents completed",
                PercentComplete = percentComplete
            });
        }

        return result;
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
        _logger.LogInformation("Starting research phase for session {SessionId} with {Count} agents",
            session.Id, agents.Count);

        var submissions = new List<ResearchSubmission>();
        var researchAgents = agents.Where(a => !a.IsChairman).ToList();
        var completedCount = 0;
        var totalCount = researchAgents.Count;

        // Broadcast phase start with 0%
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Research,
                Message = $"Starting research phase with {totalCount} agents...",
                PercentComplete = 0
            });
        }

        // Track completion with progress updates
        var completionLock = new object();
        var tasks = new List<Task<ResearchSubmission?>>();

        foreach (var agent in researchAgents)
        {
            var plan = agentPlans.TryGetValue(agent.TeamMemberId, out var p) ? p : "";

            tasks.Add(ExecuteAgentResearchWithProgressAsync(
                session, agent, refinedQuery, plan, progressCallback,
                () =>
                {
                    int completed;
                    lock (completionLock)
                    {
                        completedCount++;
                        completed = completedCount;
                    }
                    return (completed, totalCount);
                },
                cancellationToken));
        }

        // Wait for all research to complete
        var results = await Task.WhenAll(tasks);

        foreach (var submission in results)
        {
            if (submission != null)
            {
                submissions.Add(submission);
            }
        }

        // Broadcast phase completion with 100%
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Research,
                Message = $"Research complete. {submissions.Count} of {totalCount} submissions created.",
                PercentComplete = 100,
                IsComplete = true
            });
        }

        _logger.LogInformation("Research phase complete. {Count} submissions created", submissions.Count);

        return submissions;
    }

    /// <summary>
    /// Wrapper that executes agent research and broadcasts overall progress on completion.
    /// </summary>
    private async Task<ResearchSubmission?> ExecuteAgentResearchWithProgressAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        string plan,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        Func<(int completed, int total)> getProgress,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteAgentResearchStreamedAsync(
            session, agent, refinedQuery, plan, progressCallback, cancellationToken);

        // Broadcast overall progress after this agent completes
        if (progressCallback != null)
        {
            var (completed, total) = getProgress();
            var percentComplete = (completed * 100) / total;

            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Research,
                Message = $"Research: {completed} of {total} agents completed",
                PercentComplete = percentComplete
            });
        }

        return result;
    }

    /// <summary>
    /// Executes planning for a single agent using streaming, broadcasting updates via SignalR.
    /// </summary>
    private async Task<(Guid TeamMemberId, string Plan)> ExecuteAgentPlanningStreamedAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PHASE-EXEC-PLANNING] === ExecuteAgentPlanningStreamedAsync START ===");
        _logger.LogDebug("[PHASE-EXEC-PLANNING] Agent: {Role} ({RoleName})", agent.Role, agent.RoleName);
        _logger.LogDebug("[PHASE-EXEC-PLANNING] SessionId: {SessionId}", session.Id);
        _logger.LogDebug("[PHASE-EXEC-PLANNING] Query (first 100 chars): {Query}",
            refinedQuery.Length > 100 ? refinedQuery[..100] + "..." : refinedQuery);

        try
        {
            // Notify phase change
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Sending AgentPhaseChanged via SignalR");
            await _hubContext.SendAgentPhaseChangedAsync(
                session.Id, agent.TeamMemberId, agent.Role, "planning",
                $"{agent.RoleName} is creating research plan...");

            // Report start via callback
            if (progressCallback != null)
            {
                _logger.LogDebug("[PHASE-EXEC-PLANNING] Invoking progress callback (start)");
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Planning,
                    AgentRole = agent.Role,
                    TeamMemberId = agent.TeamMemberId,
                    Message = $"{agent.RoleName} is creating research plan...",
                    PercentComplete = 0
                });
            }

            // Get the chat service for this agent's inference engine
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Getting chat service for agent...");
            var chatService = GetChatServiceForAgent(agent);
            if (chatService == null)
            {
                _logger.LogWarning("[PHASE-EXEC-PLANNING] FAILED: No chat service found for agent {Role} with inference engine {EngineId}",
                    agent.Role, agent.InferenceEngineId);
                _logger.LogWarning("[PHASE-EXEC-PLANNING] RETURNING EMPTY PLAN - Agent cannot make LLM calls!");
                return (agent.TeamMemberId, "");
            }
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Chat service obtained: {ServiceType}", chatService.GetType().Name);

            // Build the planning prompt
            var planningPrompt = agent.PlanningPrompt?
                .Replace("{{QUERY}}", refinedQuery)
                ?? $"Create a research plan for: {refinedQuery}";
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Planning prompt length: {Length} chars", planningPrompt.Length);

            // Create the chat request with tools and thinking enabled
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Creating chat request...");
            var request = await CreateChatRequestAsync(agent, planningPrompt);
            _logger.LogDebug("[PHASE-EXEC-PLANNING] Chat request created:");
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   Model: {Model}", request.Model);
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   Temperature: {Temperature}", request.Temperature);
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   MaxTokens: {MaxTokens}", request.MaxTokens);
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   ToolCount: {ToolCount}", request.Tools?.Count ?? 0);
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   EnableThinking: {EnableThinking}", request.EnableThinking);

            _logger.LogDebug("[PHASE-EXEC-PLANNING] Sending streaming planning request to LLM...");

            // Execute streaming LLM call and broadcast updates
            var (content, toolCalls) = await StreamChatCompletionAsync(
                session, agent, chatService, request, cancellationToken);

            _logger.LogDebug("[PHASE-EXEC-PLANNING] LLM response received:");
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   ContentLength: {Length} chars", content.Length);
            _logger.LogDebug("[PHASE-EXEC-PLANNING]   ToolCallCount: {ToolCount}", toolCalls.Count);

            // Report completion via callback
            if (progressCallback != null)
            {
                _logger.LogDebug("[PHASE-EXEC-PLANNING] Invoking progress callback (complete)");
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Planning,
                    AgentRole = agent.Role,
                    TeamMemberId = agent.TeamMemberId,
                    Message = $"{agent.RoleName} completed research plan",
                    PercentComplete = 100,
                    IsComplete = true
                });
            }

            _logger.LogDebug("[PHASE-EXEC-PLANNING] Agent {Role} created plan with {Length} characters, {ToolCount} tool calls",
                agent.Role, content.Length, toolCalls.Count);
            _logger.LogDebug("[PHASE-EXEC-PLANNING] === ExecuteAgentPlanningStreamedAsync COMPLETE ===");

            return (agent.TeamMemberId, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PHASE-EXEC-PLANNING] ERROR during planning for agent {Role}", agent.Role);
            _logger.LogError("[PHASE-EXEC-PLANNING] Exception type: {ExType}", ex.GetType().Name);
            _logger.LogError("[PHASE-EXEC-PLANNING] Exception message: {ExMessage}", ex.Message);
            return (agent.TeamMemberId, "");
        }
    }

    /// <summary>
    /// Executes research for a single agent using streaming, broadcasting updates via SignalR.
    /// </summary>
    private async Task<ResearchSubmission?> ExecuteAgentResearchStreamedAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        string plan,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            // Notify phase change
            await _hubContext.SendAgentPhaseChangedAsync(
                session.Id, agent.TeamMemberId, agent.Role, "researching",
                $"{agent.RoleName} is researching...");

            // Report start via callback
            if (progressCallback != null)
            {
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Research,
                    AgentRole = agent.Role,
                    TeamMemberId = agent.TeamMemberId,
                    Message = $"{agent.RoleName} is researching...",
                    PercentComplete = 0
                });
            }

            // Get the chat service for this agent's inference engine
            var chatService = GetChatServiceForAgent(agent);
            if (chatService == null)
            {
                _logger.LogWarning("No chat service found for agent {Role} with inference engine {EngineId}",
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

            // Create the chat request with tools and thinking enabled
            var request = await CreateChatRequestAsync(agent, researchPrompt);

            _logger.LogDebug("Sending streaming research request to LLM for agent {Role}", agent.Role);

            // Execute streaming LLM call and broadcast updates
            var (content, toolCalls) = await StreamChatCompletionAsync(
                session, agent, chatService, request, cancellationToken);

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
                ToolCalls = toolCalls,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            // Report completion via callback
            if (progressCallback != null)
            {
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Research,
                    AgentRole = agent.Role,
                    TeamMemberId = agent.TeamMemberId,
                    Message = $"{agent.RoleName} completed research",
                    PercentComplete = 100,
                    IsComplete = true
                });
            }

            _logger.LogDebug("Agent {Role} created submission with {Length} characters, {ToolCount} tool calls",
                agent.Role, content.Length, toolCalls.Count);

            return submission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during research for agent {Role}", agent.Role);
            return null;
        }
    }

    /// <summary>
    /// Streams a chat completion, broadcasting thinking, content, and tool calls via SignalR.
    /// Returns the accumulated content and tool calls.
    /// </summary>
    private async Task<(string Content, List<ResearchToolCall> ToolCalls)> StreamChatCompletionAsync(
        ResearchSession session,
        ResearchAgent agent,
        IChatService chatService,
        AesirChatRequestBase request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[PHASE-EXEC-STREAM] === StreamChatCompletionAsync START ===");
        _logger.LogDebug("[PHASE-EXEC-STREAM] Agent: {Role}, Session: {SessionId}", agent.Role, session.Id);
        _logger.LogDebug("[PHASE-EXEC-STREAM] ChatService type: {ServiceType}", chatService.GetType().FullName);

        var contentBuilder = new StringBuilder();
        var thinkingBuilder = new StringBuilder();
        var toolCalls = new List<ResearchToolCall>();
        var chunkCount = 0;
        var thinkingChunks = 0;
        var contentChunks = 0;
        var toolCallChunks = 0;

        try
        {
            _logger.LogDebug("[PHASE-EXEC-STREAM] Starting to enumerate ChatCompletionsStreamedAsync...");

            await foreach (var chunk in chatService.ChatCompletionsStreamedAsync(request)
                .WithCancellation(cancellationToken))
            {
                chunkCount++;

                // Handle thinking/reasoning content
                if (chunk.IsThinking && chunk.Delta?.Content != null)
                {
                    thinkingChunks++;
                    thinkingBuilder.Append(chunk.Delta.Content);
                    await _hubContext.SendAgentThinkingAsync(
                        session.Id, agent.TeamMemberId, agent.Role, chunk.Delta.Content);
                }
                // Handle tool calls
                else if (chunk.ToolCall != null)
                {
                    toolCallChunks++;
                    if (chunk.EventType == StreamEventType.ToolCallStart)
                    {
                        _logger.LogDebug("[PHASE-EXEC-STREAM] Tool call starting: {ToolName}",
                            chunk.ToolCall.FunctionName);
                        await _hubContext.SendAgentToolCallStartAsync(
                            session.Id, agent.TeamMemberId, agent.Role,
                            chunk.ToolCall.ToolCallId ?? "",
                            chunk.ToolCall.FunctionName ?? "",
                            chunk.ToolCall.PluginName,
                            chunk.ToolCall.Arguments);
                    }
                    else if (chunk.EventType == StreamEventType.ToolCallResult)
                    {
                        _logger.LogDebug("[PHASE-EXEC-STREAM] Tool call result: {ToolName}, Status: {Status}",
                            chunk.ToolCall.FunctionName, chunk.ToolCall.Status);
                        // Convert AesirToolCallInfo to ResearchToolCall
                        toolCalls.Add(new ResearchToolCall
                        {
                            ToolName = chunk.ToolCall.FunctionName ?? "",
                            Input = chunk.ToolCall.Arguments != null
                                ? System.Text.Json.JsonSerializer.Serialize(chunk.ToolCall.Arguments)
                                : null,
                            Output = chunk.ToolCall.Result,
                            DurationMs = chunk.ToolCall.DurationMs,
                            Timestamp = chunk.ToolCall.StartedAt.UtcDateTime
                        });
                        await _hubContext.SendAgentToolCallResultAsync(
                            session.Id, agent.TeamMemberId, agent.Role,
                            chunk.ToolCall.ToolCallId ?? "",
                            chunk.ToolCall.FunctionName ?? "",
                            chunk.ToolCall.Result,
                            chunk.ToolCall.Status == ToolCallStatus.Completed);
                    }
                }
                // Handle regular content
                else if (chunk.Delta?.Content != null)
                {
                    contentChunks++;
                    contentBuilder.Append(chunk.Delta.Content);
                    await _hubContext.SendAgentContentAsync(
                        session.Id, agent.TeamMemberId, agent.Role, chunk.Delta.Content);
                }
            }

            _logger.LogDebug("[PHASE-EXEC-STREAM] Stream enumeration complete:");
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Total chunks: {Total}", chunkCount);
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Thinking chunks: {Thinking}", thinkingChunks);
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Content chunks: {Content}", contentChunks);
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Tool call chunks: {ToolCalls}", toolCallChunks);
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Final content length: {Length} chars", contentBuilder.Length);
            _logger.LogDebug("[PHASE-EXEC-STREAM]   Final thinking length: {Length} chars", thinkingBuilder.Length);
            _logger.LogDebug("[PHASE-EXEC-STREAM] === StreamChatCompletionAsync COMPLETE ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PHASE-EXEC-STREAM] ERROR during stream enumeration");
            _logger.LogError("[PHASE-EXEC-STREAM]   Chunks received before error: {Count}", chunkCount);
            _logger.LogError("[PHASE-EXEC-STREAM]   Content accumulated: {Length} chars", contentBuilder.Length);
            throw;
        }

        return (contentBuilder.ToString(), toolCalls);
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

        // Report start
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.Anonymization,
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

        // Report start
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
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
        if (progressCallback != null)
        {
            await progressCallback(new ResearchPhaseProgress
            {
                Phase = ResearchPhase.PeerReview,
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
    /// </summary>
    private IChatService? GetChatServiceForAgent(ResearchAgent agent)
    {
        _logger.LogDebug("[PHASE-EXEC] GetChatServiceForAgent called for {Role}", agent.Role);
        _logger.LogDebug("[PHASE-EXEC]   BaseAgentId: {BaseAgentId}", agent.BaseAgentId);
        _logger.LogDebug("[PHASE-EXEC]   InferenceEngineId: {InferenceEngineId}", agent.InferenceEngineId);
        _logger.LogDebug("[PHASE-EXEC]   Model: {Model}", agent.Model);

        if (!agent.InferenceEngineId.HasValue)
        {
            _logger.LogWarning("[PHASE-EXEC] FAILURE: Agent {Role} has no inference engine ID configured", agent.Role);
            _logger.LogWarning("[PHASE-EXEC]   This agent will NOT be able to perform LLM calls!");
            return null;
        }

        var engineIdKey = agent.InferenceEngineId.Value.ToString();
        _logger.LogDebug("[PHASE-EXEC] Attempting to get keyed service IChatService with key: '{EngineIdKey}'", engineIdKey);

        var chatService = _serviceProvider.GetKeyedService<IChatService>(engineIdKey);

        if (chatService == null)
        {
            _logger.LogWarning("[PHASE-EXEC] FAILURE: No IChatService found for inference engine ID: {EngineId}", agent.InferenceEngineId);
            _logger.LogWarning("[PHASE-EXEC]   Available keyed services might not include this engine!");
            _logger.LogWarning("[PHASE-EXEC]   Check that the inference engine module registered correctly.");
        }
        else
        {
            _logger.LogDebug("[PHASE-EXEC] SUCCESS: IChatService resolved: {ServiceType}", chatService.GetType().Name);
        }

        return chatService;
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

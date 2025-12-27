using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Executes research phases (planning and research) for agents.
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
/// Note: Full chat integration will be added when wiring to the inference module.
/// </summary>
public class ResearchPhaseExecutor : IResearchPhaseExecutor
{
    private readonly ILogger<ResearchPhaseExecutor> _logger;

    public ResearchPhaseExecutor(ILogger<ResearchPhaseExecutor> logger)
    {
        _logger = logger;
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
        var tasks = new List<Task<(Guid TeamMemberId, string Plan)>>();

        // Execute planning for each agent in parallel
        foreach (var agent in agents.Where(a => !a.IsChairman))
        {
            tasks.Add(ExecuteAgentPlanningAsync(
                agent,
                refinedQuery,
                progressCallback,
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
        _logger.LogInformation("Starting research phase for session {SessionId} with {Count} agents",
            session.Id, agents.Count);

        var submissions = new List<ResearchSubmission>();
        var tasks = new List<Task<ResearchSubmission?>>();

        // Execute research for each agent in parallel
        foreach (var agent in agents.Where(a => !a.IsChairman))
        {
            var plan = agentPlans.TryGetValue(agent.TeamMemberId, out var p) ? p : "";

            tasks.Add(ExecuteAgentResearchAsync(
                session,
                agent,
                refinedQuery,
                plan,
                progressCallback,
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

        _logger.LogInformation("Research phase complete. {Count} submissions created", submissions.Count);

        return submissions;
    }

    private async Task<(Guid TeamMemberId, string Plan)> ExecuteAgentPlanningAsync(
        ResearchAgent agent,
        string refinedQuery,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            // Report start
            if (progressCallback != null)
            {
                await progressCallback(new ResearchPhaseProgress
                {
                    Phase = ResearchPhase.Planning,
                    AgentRole = agent.Role,
                    TeamMemberId = agent.TeamMemberId,
                    Message = $"{agent.RoleName} is creating research plan...",
                    PercentComplete = 0
                });
            }

            // TODO: Integrate with IChatService when available
            // For now, create a stub plan based on the role
            var plan = GenerateStubPlan(agent, refinedQuery);

            // Simulate some work
            await Task.Delay(100, cancellationToken);

            // Report completion
            if (progressCallback != null)
            {
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

            _logger.LogDebug("Agent {Role} created plan with {Length} characters",
                agent.Role, plan.Length);

            return (agent.TeamMemberId, plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during planning for agent {Role}", agent.Role);
            return (agent.TeamMemberId, "");
        }
    }

    private async Task<ResearchSubmission?> ExecuteAgentResearchAsync(
        ResearchSession session,
        ResearchAgent agent,
        string refinedQuery,
        string plan,
        Func<ResearchPhaseProgress, Task>? progressCallback,
        CancellationToken cancellationToken)
    {
        try
        {
            // Report start
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

            // TODO: Integrate with IChatService when available
            // For now, create a stub research result
            var content = GenerateStubResearch(agent, refinedQuery, plan);

            // Simulate some work
            await Task.Delay(100, cancellationToken);

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

            // Report completion
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

            _logger.LogDebug("Agent {Role} created submission with {Length} characters",
                agent.Role, content.Length);

            return submission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during research for agent {Role}", agent.Role);
            return null;
        }
    }

    private static string GenerateStubPlan(ResearchAgent agent, string query)
    {
        return agent.Role switch
        {
            ResearchRole.DeepDiver => $"""
                # Research Plan: Deep Diver

                Query: {query}

                ## Approach
                1. Break down query into sub-questions
                2. Identify primary sources
                3. Deep-dive into each source
                4. Document findings with citations

                ## Expected Deliverables
                - Comprehensive findings
                - Citation list
                - Confidence assessments
                """,

            ResearchRole.Synthesizer => $"""
                # Research Plan: Synthesizer

                Query: {query}

                ## Approach
                1. Cast wide net across domains
                2. Identify cross-cutting themes
                3. Build integrated narrative
                4. Highlight unexpected connections

                ## Expected Deliverables
                - Pattern analysis
                - Cross-domain insights
                - Unified narrative
                """,

            ResearchRole.DevilsAdvocate => $"""
                # Research Plan: Devil's Advocate

                Query: {query}

                ## Approach
                1. Identify key assumptions
                2. Search for contradictory evidence
                3. Propose alternative hypotheses
                4. Stress-test conclusions

                ## Expected Deliverables
                - Challenged assumptions
                - Counter-evidence
                - Alternative explanations
                """,

            _ => $"Research plan for {query}"
        };
    }

    private static string GenerateStubResearch(ResearchAgent agent, string query, string plan)
    {
        return agent.Role switch
        {
            ResearchRole.DeepDiver => $"""
                # Deep Diver Research Report

                Query: {query}

                ## Findings

                *Note: This is a stub response. Full integration with inference engine pending.*

                ### Finding 1
                [Placeholder for deep research finding]
                Confidence: Medium

                ### Finding 2
                [Placeholder for deep research finding]
                Confidence: Medium

                ## Sources
                - Source 1: [Pending]
                - Source 2: [Pending]
                """,

            ResearchRole.Synthesizer => $"""
                # Synthesizer Research Report

                Query: {query}

                ## Synthesis

                *Note: This is a stub response. Full integration with inference engine pending.*

                ### Pattern Analysis
                [Placeholder for pattern analysis]

                ### Cross-Domain Insights
                [Placeholder for cross-domain insights]
                """,

            ResearchRole.DevilsAdvocate => $"""
                # Devil's Advocate Analysis

                Query: {query}

                ## Critical Analysis

                *Note: This is a stub response. Full integration with inference engine pending.*

                ### Challenged Assumptions
                [Placeholder for challenged assumptions]

                ### Alternative Hypotheses
                [Placeholder for alternative hypotheses]
                """,

            _ => $"Research for {query}"
        };
    }
}

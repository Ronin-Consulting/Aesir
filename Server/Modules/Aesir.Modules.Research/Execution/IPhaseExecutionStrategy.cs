using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;

namespace Aesir.Modules.Research.Execution;

/// <summary>
/// Strategy for executing a research phase with specific parallelization and retry policies.
/// </summary>
/// <typeparam name="TInput">The input type for the phase (e.g., ResearchAgent, tuple of agent and plan).</typeparam>
/// <typeparam name="TResult">The result type for the phase (e.g., plan string, ResearchSubmission).</typeparam>
public interface IPhaseExecutionStrategy<TInput, TResult>
{
    /// <summary>
    /// Executes a phase with the configured strategy.
    /// </summary>
    /// <param name="session">The research session context.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="executeFunc">The function to execute for each input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of results.</returns>
    Task<IReadOnlyList<TResult>> ExecutePhaseAsync(
        ResearchSession session,
        IReadOnlyList<TInput> inputs,
        Func<ResearchSession, TInput, CancellationToken, Task<TResult>> executeFunc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a phase with agent tracking for progress broadcasts.
    /// </summary>
    /// <param name="session">The research session context.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="executeFunc">The function to execute for each input.</param>
    /// <param name="agentInfoExtractor">Function to extract agent info from input for progress tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of results.</returns>
    Task<IReadOnlyList<TResult>> ExecutePhaseWithAgentTrackingAsync(
        ResearchSession session,
        IReadOnlyList<TInput> inputs,
        Func<ResearchSession, TInput, CancellationToken, Task<TResult>> executeFunc,
        Func<TInput, ActiveAgentInfo> agentInfoExtractor,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory for creating phase execution strategies based on research configuration.
/// </summary>
public interface IPhaseExecutionStrategyFactory
{
    /// <summary>
    /// Creates a strategy for the planning phase (2 parallel agents).
    /// </summary>
    /// <returns>A planning phase execution strategy.</returns>
    IPhaseExecutionStrategy<ResearchAgent, (Guid TeamMemberId, string Plan)> CreatePlanningStrategy();

    /// <summary>
    /// Creates a strategy for the research phase (2 parallel agents).
    /// </summary>
    /// <returns>A research phase execution strategy.</returns>
    IPhaseExecutionStrategy<(ResearchAgent Agent, string Plan), ResearchSubmission> CreateResearchStrategy();

    /// <summary>
    /// Creates a strategy for peer review phase (1 reviewer, 2 submissions in parallel).
    /// </summary>
    /// <returns>A peer review phase execution strategy.</returns>
    IPhaseExecutionStrategy<(ResearchAgent Reviewer, AnonymizedSubmission Submission), PeerReview> CreatePeerReviewStrategy();
}
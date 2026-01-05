using Aesir.Infrastructure.Concurrency;
using Aesir.Modules.Research.Agents;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aesir.Modules.Research.Execution;

/// <summary>
/// Factory that creates phase execution strategies with appropriate retry policies.
/// </summary>
public class PhaseExecutionStrategyFactory : IPhaseExecutionStrategyFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly IServiceProvider _serviceProvider;

    public PhaseExecutionStrategyFactory(
        ILoggerFactory loggerFactory,
        IResearchProgressBroadcaster progressBroadcaster,
        IServiceProvider serviceProvider)
    {
        _loggerFactory = loggerFactory;
        _progressBroadcaster = progressBroadcaster;
        _serviceProvider = serviceProvider;
    }

    public IPhaseExecutionStrategy<ResearchAgent, (Guid TeamMemberId, string Plan)> CreatePlanningStrategy()
    {
        return CreateStrategy<ResearchAgent, (Guid, string)>(ResearchPhase.Planning);
    }

    public IPhaseExecutionStrategy<(ResearchAgent Agent, string Plan), ResearchSubmission> CreateResearchStrategy()
    {
        return CreateStrategy<(ResearchAgent, string), ResearchSubmission>(ResearchPhase.Research);
    }

    public IPhaseExecutionStrategy<(ResearchAgent Reviewer, AnonymizedSubmission Submission), PeerReview> CreatePeerReviewStrategy()
    {
        return CreateStrategy<(ResearchAgent, AnonymizedSubmission), PeerReview>(ResearchPhase.PeerReview);
    }

    /// <summary>
    /// Creates a strategy with DI-resolved executor and retry policy.
    /// </summary>
    private IPhaseExecutionStrategy<TInput, TResult> CreateStrategy<TInput, TResult>(ResearchPhase phase)
    {
        var retryPolicy = CreateLlmRetryPolicy<TResult>();

        // Resolve executor from DI to maintain testability and proper dependency management
        var executor = _serviceProvider.GetRequiredService<IConcurrentExecutor<TInput, TResult>>();

        return new ParallelPhaseExecutionStrategy<TInput, TResult>(
            _loggerFactory.CreateLogger<ParallelPhaseExecutionStrategy<TInput, TResult>>(),
            executor,
            maxDegreeOfParallelism: 2, // Fixed: 2 concurrent tasks
            retryPolicy,
            _progressBroadcaster,
            phase);
    }

    /// <summary>
    /// Creates a Polly retry policy for LLM calls.
    /// Retries on transient failures (timeout, rate limit, network errors).
    /// </summary>
    private IAsyncPolicy<TResult> CreateLlmRetryPolicy<TResult>()
    {
        return Policy<TResult>
            .Handle<HttpRequestException>() // Network errors
            .Or<TimeoutException>() // Timeout errors
            .Or<TaskCanceledException>() // Task cancellation
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = _loggerFactory.CreateLogger<PhaseExecutionStrategyFactory>();
                    logger.LogWarning(
                        "LLM call failed with {Exception}. Retry {RetryCount}/3 after {Delay}s",
                        outcome.Exception?.GetType().Name ?? "Unknown",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }
}
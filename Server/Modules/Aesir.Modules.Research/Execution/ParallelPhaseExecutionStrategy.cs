using System.Collections.Concurrent;
using Aesir.Infrastructure.Concurrency;
using Aesir.Modules.Research.Contracts;
using Aesir.Modules.Research.Models;
using Aesir.Modules.Research.Services;
using Microsoft.Extensions.Logging;
using Polly;

namespace Aesir.Modules.Research.Execution;

/// <summary>
/// Executes a research phase with configurable parallelization and Polly retry policies.
/// Supports multi-agent tracking for real-time progress broadcasts.
/// </summary>
/// <typeparam name="TInput">The input type for the phase.</typeparam>
/// <typeparam name="TResult">The result type for the phase.</typeparam>
public class ParallelPhaseExecutionStrategy<TInput, TResult> : IPhaseExecutionStrategy<TInput, TResult>
{
    private readonly ILogger<ParallelPhaseExecutionStrategy<TInput, TResult>> _logger;
    private readonly IConcurrentExecutor<TInput, TResult> _executor;
    private readonly int _maxDegreeOfParallelism;
    private readonly IAsyncPolicy<TResult>? _retryPolicy;
    private readonly IResearchProgressBroadcaster _progressBroadcaster;
    private readonly ResearchPhase _phase;

    public ParallelPhaseExecutionStrategy(
        ILogger<ParallelPhaseExecutionStrategy<TInput, TResult>> logger,
        IConcurrentExecutor<TInput, TResult> executor,
        int maxDegreeOfParallelism,
        IAsyncPolicy<TResult>? retryPolicy,
        IResearchProgressBroadcaster progressBroadcaster,
        ResearchPhase phase)
    {
        _logger = logger;
        _executor = executor;
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        _retryPolicy = retryPolicy;
        _progressBroadcaster = progressBroadcaster;
        _phase = phase;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TResult>> ExecutePhaseAsync(
        ResearchSession session,
        IReadOnlyList<TInput> inputs,
        Func<ResearchSession, TInput, CancellationToken, Task<TResult>> executeFunc,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing {Phase} phase with {Count} items, max parallelism: {MaxParallelism}",
            _phase, inputs.Count, _maxDegreeOfParallelism);

        // Create progress reporter that broadcasts to SignalR clients
        var progress = new Progress<ConcurrentExecutionProgress>(p =>
        {
            // Broadcast progress to SignalR clients asynchronously
            // Don't await here to avoid blocking the progress callback
            _ = _progressBroadcaster.BroadcastProgressAsync(session.Id, new ResearchPhaseProgress
            {
                Phase = _phase,
                Message = $"{_phase}: {p.CompletedCount} of {p.TotalCount} completed",
                PercentComplete = p.PercentComplete
            });
        });

        var options = new ConcurrentExecutionOptions<TResult>
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            ResiliencePolicy = _retryPolicy,
            Progress = progress,
            StopOnFirstError = false // Continue even if some agents fail
        };

        var results = await _executor.ExecuteAsync(
            inputs,
            async (input, ct) => await executeFunc(session, input, ct).ConfigureAwait(false),
            options,
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "{Phase} phase completed: {SuccessCount}/{TotalCount} successful",
            _phase, results.Count(r => r != null), inputs.Count);

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TResult>> ExecutePhaseWithAgentTrackingAsync(
        ResearchSession session,
        IReadOnlyList<TInput> inputs,
        Func<ResearchSession, TInput, CancellationToken, Task<TResult>> executeFunc,
        Func<TInput, ActiveAgentInfo> agentInfoExtractor,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Executing {Phase} phase with agent tracking: {Count} items, max parallelism: {MaxParallelism}",
            _phase, inputs.Count, _maxDegreeOfParallelism);

        // Track currently active agents using a thread-safe dictionary
        var activeAgents = new ConcurrentDictionary<Guid, ActiveAgentInfo>();
        var completedCount = 0;
        var totalCount = inputs.Count;

        // Helper to broadcast current state
        async Task BroadcastCurrentStateAsync(string message, int percentComplete)
        {
            var agentList = activeAgents.Values.ToList();
            await _progressBroadcaster.BroadcastProgressAsync(
                session.Id,
                _phase,
                percentComplete,
                message,
                agentList).ConfigureAwait(false);
        }

        // Initial broadcast showing phase start
        await BroadcastCurrentStateAsync($"{_phase}: Starting...", 0).ConfigureAwait(false);

        var options = new ConcurrentExecutionOptions<TResult>
        {
            MaxDegreeOfParallelism = _maxDegreeOfParallelism,
            ResiliencePolicy = _retryPolicy,
            Progress = null, // We handle progress ourselves
            StopOnFirstError = false
        };

        var results = await _executor.ExecuteAsync(
            inputs,
            async (input, ct) =>
            {
                // Extract agent info and add to active set
                var agentInfo = agentInfoExtractor(input);
                agentInfo.Activity = $"{agentInfo.RoleName} is working...";
                activeAgents.TryAdd(agentInfo.TeamMemberId, agentInfo);

                _logger.LogDebug(
                    "[AGENT-TRACKING] Agent {Role} ({TeamMemberId}) started",
                    agentInfo.Role, agentInfo.TeamMemberId);

                // Broadcast agent started
                var currentPercent = totalCount > 0 ? (completedCount * 100) / totalCount : 0;
                await BroadcastCurrentStateAsync(
                    $"{_phase}: {completedCount} of {totalCount} completed",
                    currentPercent).ConfigureAwait(false);

                try
                {
                    // Execute the actual work
                    var result = await executeFunc(session, input, ct).ConfigureAwait(false);

                    // Update agent status on completion
                    agentInfo.Activity = $"{agentInfo.RoleName} completed";
                    return result;
                }
                catch (Exception ex)
                {
                    // Update agent status on error
                    agentInfo.Activity = $"{agentInfo.RoleName} encountered an error";
                    _logger.LogWarning(ex, "[AGENT-TRACKING] Agent {Role} failed", agentInfo.Role);
                    throw;
                }
                finally
                {
                    // Remove from active set and increment completed count
                    activeAgents.TryRemove(agentInfo.TeamMemberId, out _);
                    Interlocked.Increment(ref completedCount);

                    _logger.LogDebug(
                        "[AGENT-TRACKING] Agent {Role} ({TeamMemberId}) finished. Completed: {Completed}/{Total}",
                        agentInfo.Role, agentInfo.TeamMemberId, completedCount, totalCount);

                    // Broadcast agent completed
                    var newPercent = totalCount > 0 ? (completedCount * 100) / totalCount : 0;
                    await BroadcastCurrentStateAsync(
                        $"{_phase}: {completedCount} of {totalCount} completed",
                        newPercent).ConfigureAwait(false);
                }
            },
            options,
            cancellationToken).ConfigureAwait(false);

        // Final broadcast showing completion
        await BroadcastCurrentStateAsync(
            $"{_phase}: All {totalCount} items completed",
            100).ConfigureAwait(false);

        _logger.LogInformation(
            "{Phase} phase completed with agent tracking: {SuccessCount}/{TotalCount} successful",
            _phase, results.Count(r => r != null), inputs.Count);

        return results;
    }
}
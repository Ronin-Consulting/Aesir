using Polly;

namespace Aesir.Infrastructure.Concurrency;

/// <summary>
/// Executes tasks concurrently with configurable limits, retry policies, and progress tracking.
/// </summary>
/// <typeparam name="TInput">The input type for each task.</typeparam>
/// <typeparam name="TResult">The result type for each task.</typeparam>
public interface IConcurrentExecutor<TInput, TResult>
{
    /// <summary>
    /// Executes tasks concurrently with a maximum degree of parallelism.
    /// </summary>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="executeAsync">The async function to execute for each input.</param>
    /// <param name="options">Execution options including parallelism limits and retry policies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A collection of results in the same order as inputs.
    /// IMPORTANT: For failed tasks (when StopOnFirstError is false), the result will be default(TResult),
    /// which is null for reference types. Callers must handle null results appropriately.
    /// </returns>
    Task<IReadOnlyList<TResult>> ExecuteAsync(
        IReadOnlyList<TInput> inputs,
        Func<TInput, CancellationToken, Task<TResult>> executeAsync,
        ConcurrentExecutionOptions<TResult>? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for concurrent execution behavior.
/// </summary>
public class ConcurrentExecutionOptions<TResult>
{
    /// <summary>
    /// Maximum number of tasks to execute concurrently.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 2;

    /// <summary>
    /// Polly resilience policy for retry logic.
    /// </summary>
    public IAsyncPolicy<TResult>? ResiliencePolicy { get; set; }

    /// <summary>
    /// Optional progress reporter for tracking completion.
    /// </summary>
    public IProgress<ConcurrentExecutionProgress>? Progress { get; set; }

    /// <summary>
    /// Whether to stop on first error or collect all errors.
    /// </summary>
    public bool StopOnFirstError { get; set; } = false;
}

/// <summary>
/// Progress information for concurrent execution.
/// </summary>
public class ConcurrentExecutionProgress
{
    /// <summary>
    /// Total number of tasks to execute.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of tasks that have completed successfully.
    /// </summary>
    public int CompletedCount { get; init; }

    /// <summary>
    /// Number of tasks that have failed.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Completion percentage (0-100).
    /// </summary>
    public int PercentComplete => TotalCount > 0 ? (CompletedCount * 100) / TotalCount : 0;
}
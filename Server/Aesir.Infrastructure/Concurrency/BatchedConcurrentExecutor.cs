using Microsoft.Extensions.Logging;
using Polly;
using System.Collections.Concurrent;

namespace Aesir.Infrastructure.Concurrency;

/// <summary>
/// Implementation of IConcurrentExecutor that uses SemaphoreSlim for throttling
/// and Polly for resilience.
/// </summary>
/// <typeparam name="TInput">The input type for each task.</typeparam>
/// <typeparam name="TResult">The result type for each task.</typeparam>
public class BatchedConcurrentExecutor<TInput, TResult> : IConcurrentExecutor<TInput, TResult>
{
    private readonly ILogger<BatchedConcurrentExecutor<TInput, TResult>> _logger;

    public BatchedConcurrentExecutor(ILogger<BatchedConcurrentExecutor<TInput, TResult>> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<TResult>> ExecuteAsync(
        IReadOnlyList<TInput> inputs,
        Func<TInput, CancellationToken, Task<TResult>> executeAsync,
        ConcurrentExecutionOptions<TResult>? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ConcurrentExecutionOptions<TResult>();

        if (inputs.Count == 0)
            return Array.Empty<TResult>();

        _logger.LogDebug(
            "Starting concurrent execution: {Count} items, max parallelism: {MaxParallelism}",
            inputs.Count, options.MaxDegreeOfParallelism);

        using var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism, options.MaxDegreeOfParallelism);
        var results = new ConcurrentDictionary<int, TResult>();
        var errors = new ConcurrentBag<Exception>();
        var completed = 0;
        var failed = 0;

        var tasks = inputs.Select(async (input, index) =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                TResult result;
                if (options.ResiliencePolicy != null)
                {
                    // Execute with retry policy
                    result = await options.ResiliencePolicy.ExecuteAsync(
                        async (ct) => await executeAsync(input, ct).ConfigureAwait(false),
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Execute without retry
                    result = await executeAsync(input, cancellationToken).ConfigureAwait(false);
                }

                results[index] = result;
                Interlocked.Increment(ref completed);

                // Report progress
                options.Progress?.Report(new ConcurrentExecutionProgress
                {
                    TotalCount = inputs.Count,
                    CompletedCount = completed,
                    FailedCount = failed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing concurrent task for input at index {Index}", index);
                errors.Add(ex);
                Interlocked.Increment(ref failed);

                // Report progress even on failure
                options.Progress?.Report(new ConcurrentExecutionProgress
                {
                    TotalCount = inputs.Count,
                    CompletedCount = completed,
                    FailedCount = failed
                });

                if (options.StopOnFirstError)
                    throw;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Task.WhenAll will throw if any task throws (and StopOnFirstError is true)
            // We want to ensure all tasks complete before disposing the semaphore
            // If a task threw, it's already in the errors collection
        }

        if (errors.Any())
        {
            _logger.LogWarning(
                "Concurrent execution completed with {ErrorCount} errors out of {TotalCount} tasks",
                errors.Count, inputs.Count);

            if (options.StopOnFirstError)
                throw new AggregateException("Concurrent execution failed", errors);

            // Log warning about null results for failed tasks
            var failedIndices = Enumerable.Range(0, inputs.Count)
                .Where(i => !results.ContainsKey(i))
                .ToList();

            if (failedIndices.Any())
            {
                _logger.LogWarning(
                    "Failed tasks at indices [{Indices}] will return default(TResult) which may be null. " +
                    "Consumers must handle null results.",
                    string.Join(", ", failedIndices));
            }
        }

        // Return results in original input order
        // IMPORTANT: Failed tasks return default(TResult)! which is null for reference types
        return Enumerable.Range(0, inputs.Count)
            .Select(i => results.TryGetValue(i, out var result) ? result : default(TResult)!)
            .ToList();
    }
}
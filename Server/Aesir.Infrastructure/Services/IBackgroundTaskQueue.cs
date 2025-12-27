namespace Aesir.Infrastructure.Services;

/// <summary>
/// Interface for a background task queue that accepts work items to be processed asynchronously.
/// Used to decouple long-running operations (like document indexing) from HTTP request lifecycles.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a work item for background processing.
    /// </summary>
    /// <param name="workItem">
    /// A function that receives a scoped IServiceProvider and CancellationToken.
    /// The service provider should be used to resolve scoped services within the work item.
    /// </param>
    ValueTask QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem);

    /// <summary>
    /// Dequeues a work item for processing.
    /// This method blocks until a work item is available or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Token to signal cancellation of the wait.</param>
    /// <returns>The next work item to process.</returns>
    ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

using System.Threading.Channels;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Thread-safe background task queue implementation using System.Threading.Channels.
/// Provides bounded capacity with backpressure to prevent memory exhaustion.
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, ValueTask>> _queue;

    /// <summary>
    /// Creates a new background task queue with bounded capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of queued items. Default is 100.</param>
    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            // Wait when queue is full (backpressure)
            FullMode = BoundedChannelFullMode.Wait,
            // Single consumer (BackgroundTaskProcessorService)
            SingleReader = true,
            // Multiple producers (HTTP request handlers)
            SingleWriter = false
        };

        _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, ValueTask>>(options);
    }

    /// <inheritdoc />
    public async ValueTask QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
    }
}

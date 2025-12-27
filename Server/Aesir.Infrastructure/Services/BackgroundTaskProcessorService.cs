using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Hosted service that continuously processes work items from the background task queue.
/// Creates a scoped service provider for each work item to properly handle scoped dependencies.
/// </summary>
public class BackgroundTaskProcessorService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskProcessorService> _logger;

    public BackgroundTaskProcessorService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<BackgroundTaskProcessorService> logger)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Task Processor Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a work item to be available
                var workItem = await _taskQueue.DequeueAsync(stoppingToken).ConfigureAwait(false);

                _logger.LogDebug("Dequeued background work item for processing");

                // Create a scope for the work item to properly handle scoped services
                await using var scope = _serviceProvider.CreateAsyncScope();

                try
                {
                    // Execute the work item with the scoped service provider
                    // Use the application stopping token, not the HTTP request token
                    await workItem(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);

                    _logger.LogDebug("Background work item completed successfully");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Log the error but continue processing other items
                    _logger.LogError(ex, "Error occurred executing background work item");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown - exit gracefully
                break;
            }
            catch (Exception ex)
            {
                // Unexpected error in the dequeue operation itself
                _logger.LogError(ex, "Error occurred in background task processor");

                // Brief delay before retrying to prevent tight error loop
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Background Task Processor Service is stopping");
    }
}

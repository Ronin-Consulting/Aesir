using System.Threading.Channels;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Implementation of tool call broadcasting using Kernel.Data for per-request scoping.
/// Each request gets its own channel for tool call events.
/// Uses Kernel.Data instead of AsyncLocal because Semantic Kernel's internal async execution
/// may break AsyncLocal context flow (due to ConfigureAwait(false) or Task.Run).
/// </summary>
public class ToolCallBroadcaster : IToolCallBroadcaster
{
    private readonly ILogger<ToolCallBroadcaster> _logger;

    /// <summary>
    /// The key used to store the broadcaster scope in Kernel.Data
    /// </summary>
    internal const string KernelDataKey = "ToolCallBroadcasterScope";

    public ToolCallBroadcaster(ILogger<ToolCallBroadcaster> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the current active scope from the Kernel's data dictionary.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance</param>
    /// <returns>The active scope, or null if none exists</returns>
    public static ToolCallBroadcasterScope? GetScope(Kernel kernel)
    {
        if (kernel.Data.TryGetValue(KernelDataKey, out var value) && value is ToolCallBroadcasterScope scope)
        {
            return scope;
        }
        return null;
    }

    /// <inheritdoc />
    public IToolCallBroadcasterScope CreateScope()
    {
        var scope = new ToolCallBroadcasterScope(_logger);
        _logger.LogDebug("Created new ToolCallBroadcasterScope");
        return scope;
    }

    /// <summary>
    /// Internal scope implementation that manages the channel lifecycle.
    /// </summary>
    public class ToolCallBroadcasterScope : IToolCallBroadcasterScope
    {
        private readonly Channel<AesirToolCallInfo> _channel;
        private readonly ILogger _logger;
        private bool _isCompleted;

        public ToolCallBroadcasterScope(ILogger logger)
        {
            _logger = logger;
            // Unbounded channel with single reader for optimal performance
            _channel = Channel.CreateUnbounded<AesirToolCallInfo>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false // Multiple filters may write concurrently
                });
        }

        /// <inheritdoc />
        public ChannelReader<AesirToolCallInfo> Reader => _channel.Reader;

        /// <inheritdoc />
        public async Task BroadcastStartAsync(AesirToolCallInfo toolCall)
        {
            if (_isCompleted) return;
            _logger.LogDebug("Broadcasting tool call START: {FunctionName} ({ToolCallId})",
                toolCall.FunctionName, toolCall.ToolCallId);
            await _channel.Writer.WriteAsync(toolCall);
        }

        /// <inheritdoc />
        public async Task BroadcastCompletionAsync(AesirToolCallInfo toolCall)
        {
            if (_isCompleted) return;
            _logger.LogDebug("Broadcasting tool call COMPLETION: {FunctionName} ({ToolCallId}) - Status: {Status}",
                toolCall.FunctionName, toolCall.ToolCallId, toolCall.Status);
            await _channel.Writer.WriteAsync(toolCall);
        }

        /// <inheritdoc />
        public void Complete()
        {
            if (_isCompleted) return;
            _isCompleted = true;
            _logger.LogDebug("ToolCallBroadcasterScope completed");
            _channel.Writer.TryComplete();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Complete();
            return ValueTask.CompletedTask;
        }
    }
}

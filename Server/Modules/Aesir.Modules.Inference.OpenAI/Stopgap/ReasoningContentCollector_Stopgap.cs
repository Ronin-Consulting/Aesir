using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.OpenAI.Stopgap;

/// <summary>
/// STOPGAP: Collects reasoning_content from HTTP responses and makes it available
/// to ChatService via a channel-based approach.
///
/// Pattern follows ToolCallBroadcaster and InferenceLogCollector in codebase.
///
/// This will be removed when native SDK support is available.
/// </summary>
public sealed class ReasoningContentCollector_Stopgap : IReasoningContentCollector_Stopgap
{
    /// <summary>
    /// Key for storing collector in Kernel.Data
    /// </summary>
    public const string KernelDataKey = "ReasoningContentCollector_Stopgap";

    /// <summary>
    /// AsyncLocal for bridging HTTP handler to request scope.
    /// Only used for the synchronous portion of the call chain.
    /// </summary>
    internal static readonly AsyncLocal<ReasoningContentCollector_Stopgap?> Current = new();

    private readonly Channel<ReasoningChunk_Stopgap> _channel;
    private readonly ILogger? _logger;
    private bool _isThinking = true;
    private bool _isCompleted;

    public ReasoningContentCollector_Stopgap(ILogger? logger = null)
    {
        _logger = logger;
        _channel = Channel.CreateUnbounded<ReasoningChunk_Stopgap>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true // Only the handler writes
            });
    }

    public ChannelReader<ReasoningChunk_Stopgap> Reader => _channel.Reader;

    public bool IsThinking => _isThinking;

    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        if (_isCompleted || string.IsNullOrEmpty(content)) return;

        // _logger?.LogDebug("[Stopgap] Reasoning chunk: {Content}",
        //     content.Length > 50 ? content[..50] + "..." : content);

        await _channel.Writer.WriteAsync(
            new ReasoningChunk_Stopgap(content, _isThinking),
            cancellationToken);
    }

    public void EndReasoning()
    {
        _isThinking = false;
        //_logger?.LogDebug("[Stopgap] Reasoning ended, transitioning to content");
    }

    public void Complete()
    {
        if (_isCompleted) return;
        _isCompleted = true;
        _channel.Writer.TryComplete();
        //_logger?.LogDebug("[Stopgap] Collector completed");
    }

    public ValueTask DisposeAsync()
    {
        Complete();
        return ValueTask.CompletedTask;
    }

    // Static helpers following existing patterns in codebase

    public static ReasoningContentCollector_Stopgap? GetFromKernel(Kernel kernel)
    {
        if (kernel.Data.TryGetValue(KernelDataKey, out var collector)
            && collector is ReasoningContentCollector_Stopgap c)
        {
            return c;
        }
        return null;
    }

    public static void StoreInKernel(Kernel kernel, ReasoningContentCollector_Stopgap collector)
    {
        kernel.Data[KernelDataKey] = collector;
    }

    public static void RemoveFromKernel(Kernel kernel)
    {
        kernel.Data.Remove(KernelDataKey);
    }

    /// <summary>
    /// Creates a scope where the collector is available to HTTP handlers via AsyncLocal.
    /// Use with 'using' statement around the SK call.
    /// </summary>
    public IDisposable CreateScope()
    {
        return new CollectorScope(this);
    }

    private sealed class CollectorScope : IDisposable
    {
        public CollectorScope(ReasoningContentCollector_Stopgap collector)
        {
            Current.Value = collector;
        }

        public void Dispose()
        {
            Current.Value = null;
        }
    }
}

using System.Threading.Channels;
using Aesir.Common.Models;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Service for broadcasting tool call events from SK filters to streaming responses.
/// Uses a channel-based approach for thread-safe communication.
/// </summary>
public interface IToolCallBroadcaster
{
    /// <summary>
    /// Creates a new scope for tool call broadcasting (per-request).
    /// </summary>
    /// <returns>A scoped broadcaster instance for this request.</returns>
    IToolCallBroadcasterScope CreateScope();
}

/// <summary>
/// Scoped broadcaster for a single chat request.
/// Provides isolated tool call channels per request.
/// </summary>
public interface IToolCallBroadcasterScope : IAsyncDisposable
{
    /// <summary>
    /// Gets the channel reader for consuming tool call events.
    /// </summary>
    ChannelReader<AesirToolCallInfo> Reader { get; }

    /// <summary>
    /// Broadcasts a tool call start event.
    /// </summary>
    /// <param name="toolCall">The tool call information to broadcast.</param>
    Task BroadcastStartAsync(AesirToolCallInfo toolCall);

    /// <summary>
    /// Broadcasts a tool call completion event.
    /// </summary>
    /// <param name="toolCall">The tool call information with result to broadcast.</param>
    Task BroadcastCompletionAsync(AesirToolCallInfo toolCall);

    /// <summary>
    /// Marks the channel as complete, signaling no more tool calls will be broadcast.
    /// </summary>
    void Complete();
}

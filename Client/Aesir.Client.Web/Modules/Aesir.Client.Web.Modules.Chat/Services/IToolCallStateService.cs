using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing tool call state during streaming chat responses.
/// Accumulates and tracks tool calls for UI display.
/// </summary>
public interface IToolCallStateService
{
    /// <summary>
    /// Gets the current list of tool calls for the active streaming message.
    /// </summary>
    IReadOnlyList<AesirToolCallInfo> CurrentToolCalls { get; }

    /// <summary>
    /// Gets whether any tool calls are currently in progress (status = Started).
    /// </summary>
    bool HasActiveToolCalls { get; }

    /// <summary>
    /// Gets the count of tool calls by status.
    /// </summary>
    (int Total, int Completed, int Failed, int InProgress) GetStatusCounts();

    /// <summary>
    /// Event fired when the tool calls collection changes (new call added, status updated).
    /// </summary>
    event Action? OnToolCallsChanged;

    /// <summary>
    /// Processes a tool call event from the streaming response.
    /// Adds new tool calls or updates existing ones based on ToolCallId.
    /// </summary>
    /// <param name="toolCall">The tool call information to process.</param>
    void ProcessToolCallEvent(AesirToolCallInfo toolCall);

    /// <summary>
    /// Clears all tool calls. Call this when starting a new message.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a specific tool call by its ID.
    /// </summary>
    /// <param name="toolCallId">The tool call ID to find.</param>
    /// <returns>The tool call info, or null if not found.</returns>
    AesirToolCallInfo? GetToolCall(string toolCallId);
}

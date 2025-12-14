using Aesir.Common.Models;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Interface for collecting inference operation data during chat completion.
/// This collector accumulates tool calls and tracks timing to create a complete inference log.
/// </summary>
public interface IInferenceLogCollector
{
    /// <summary>
    /// Gets the unique ID for this inference log.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets whether the collector has been started.
    /// </summary>
    bool IsStarted { get; }

    /// <summary>
    /// Gets whether the collector has been completed (success or failure).
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Starts collecting inference data.
    /// </summary>
    /// <param name="chatSessionId">The chat session ID.</param>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="userQuery">The user's query that triggered the inference.</param>
    void Start(Guid? chatSessionId, Guid? conversationId, string userQuery);

    /// <summary>
    /// Adds a tool call to the collection.
    /// </summary>
    /// <param name="toolCall">The tool call info to add.</param>
    void AddToolCall(AesirToolCallInfo toolCall);

    /// <summary>
    /// Completes the inference collection successfully.
    /// </summary>
    /// <param name="assistantResponse">The assistant's response text (will be truncated to 500 chars).</param>
    /// <returns>The complete inference log ready for persistence.</returns>
    AesirInferenceLog Complete(string? assistantResponse);

    /// <summary>
    /// Marks the inference as failed.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>The inference log with failed status.</returns>
    AesirInferenceLog Fail(string errorMessage);

    /// <summary>
    /// Marks the inference as cancelled.
    /// </summary>
    /// <returns>The inference log with cancelled status.</returns>
    AesirInferenceLog Cancel();
}

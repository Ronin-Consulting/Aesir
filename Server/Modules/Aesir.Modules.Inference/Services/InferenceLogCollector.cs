using System.Diagnostics;
using Aesir.Common.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Collects inference operation data during chat completion.
/// This collector accumulates tool calls and tracks timing to create a complete inference log.
/// Thread-safe for concurrent tool call additions.
/// </summary>
public class InferenceLogCollector : IInferenceLogCollector
{
    /// <summary>
    /// The key used to store the collector in Kernel.Data.
    /// </summary>
    public const string KernelDataKey = "InferenceLogCollector";

    private readonly object _lock = new();
    private readonly List<AesirToolCallInfo> _toolCalls = new();
    private readonly Stopwatch _stopwatch = new();

    private Guid? _chatSessionId;
    private Guid? _conversationId;
    private string _userQuery = string.Empty;
    private bool _isStarted;
    private bool _isCompleted;

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public bool IsStarted => _isStarted;

    /// <inheritdoc />
    public bool IsCompleted => _isCompleted;

    /// <inheritdoc />
    public void Start(Guid? chatSessionId, Guid? conversationId, string userQuery)
    {
        lock (_lock)
        {
            if (_isStarted)
            {
                throw new InvalidOperationException("Collector has already been started.");
            }

            _chatSessionId = chatSessionId;
            _conversationId = conversationId;
            _userQuery = userQuery ?? string.Empty;
            _isStarted = true;
            _stopwatch.Start();
        }
    }

    /// <inheritdoc />
    public void AddToolCall(AesirToolCallInfo toolCall)
    {
        lock (_lock)
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Collector has not been started.");
            }

            if (_isCompleted)
            {
                throw new InvalidOperationException("Collector has already been completed.");
            }

            // Ensure the tool call has the session context
            toolCall.ChatSessionId = _chatSessionId;
            toolCall.ConversationId = _conversationId;

            _toolCalls.Add(toolCall);
        }
    }

    /// <inheritdoc />
    public AesirInferenceLog Complete(string? assistantResponse)
    {
        lock (_lock)
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Collector has not been started.");
            }

            if (_isCompleted)
            {
                throw new InvalidOperationException("Collector has already been completed.");
            }

            _stopwatch.Stop();
            _isCompleted = true;

            return BuildInferenceLog(InferenceStatus.Completed, assistantResponse, null);
        }
    }

    /// <inheritdoc />
    public AesirInferenceLog Fail(string errorMessage)
    {
        lock (_lock)
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Collector has not been started.");
            }

            if (_isCompleted)
            {
                throw new InvalidOperationException("Collector has already been completed.");
            }

            _stopwatch.Stop();
            _isCompleted = true;

            return BuildInferenceLog(InferenceStatus.Failed, null, errorMessage);
        }
    }

    /// <inheritdoc />
    public AesirInferenceLog Cancel()
    {
        lock (_lock)
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Collector has not been started.");
            }

            if (_isCompleted)
            {
                throw new InvalidOperationException("Collector has already been completed.");
            }

            _stopwatch.Stop();
            _isCompleted = true;

            return BuildInferenceLog(InferenceStatus.Cancelled, null, null);
        }
    }

    private AesirInferenceLog BuildInferenceLog(InferenceStatus status, string? assistantResponse, string? errorMessage)
    {
        var now = DateTimeOffset.UtcNow;
        var startedAt = now.AddMilliseconds(-_stopwatch.ElapsedMilliseconds);

        return new AesirInferenceLog
        {
            Id = Id,
            ChatSessionId = _chatSessionId,
            ConversationId = _conversationId,
            UserQuery = _userQuery,
            UserQueryTruncated = TruncateText(_userQuery, 500),
            AssistantResponse = TruncateText(assistantResponse, 500),
            ToolCalls = new List<AesirToolCallInfo>(_toolCalls),
            ToolCallCount = _toolCalls.Count,
            TotalDurationMs = _stopwatch.ElapsedMilliseconds,
            StartedAt = startedAt,
            CompletedAt = now,
            Status = status,
            ErrorMessage = errorMessage
        };
    }

    private static string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Length <= maxLength
            ? text
            : text[..maxLength];
    }

    /// <summary>
    /// Gets the collector from the kernel's data dictionary.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <returns>The collector if found, otherwise null.</returns>
    public static IInferenceLogCollector? GetFromKernel(Kernel kernel)
    {
        if (kernel.Data.TryGetValue(KernelDataKey, out var collector) && collector is IInferenceLogCollector logCollector)
        {
            return logCollector;
        }

        return null;
    }

    /// <summary>
    /// Stores the collector in the kernel's data dictionary.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    /// <param name="collector">The collector to store.</param>
    public static void StoreInKernel(Kernel kernel, IInferenceLogCollector collector)
    {
        kernel.Data[KernelDataKey] = collector;
    }

    /// <summary>
    /// Removes the collector from the kernel's data dictionary.
    /// </summary>
    /// <param name="kernel">The kernel instance.</param>
    public static void RemoveFromKernel(Kernel kernel)
    {
        kernel.Data.Remove(KernelDataKey);
    }
}

using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Implementation of <see cref="IToolCallStateService"/> that manages tool call state
/// during streaming chat responses.
/// </summary>
public sealed class ToolCallStateService : IToolCallStateService
{
    private readonly Dictionary<string, AesirToolCallInfo> _toolCallsById = [];
    private readonly List<AesirToolCallInfo> _toolCallsOrdered = [];
    private readonly object _lock = new();

    /// <inheritdoc />
    public IReadOnlyList<AesirToolCallInfo> CurrentToolCalls
    {
        get
        {
            lock (_lock)
            {
                return _toolCallsOrdered.ToList();
            }
        }
    }

    /// <inheritdoc />
    public bool HasActiveToolCalls
    {
        get
        {
            lock (_lock)
            {
                return _toolCallsOrdered.Any(tc => tc.Status == ToolCallStatus.Started);
            }
        }
    }

    /// <inheritdoc />
    public event Action? OnToolCallsChanged;

    /// <inheritdoc />
    public (int Total, int Completed, int Failed, int InProgress) GetStatusCounts()
    {
        lock (_lock)
        {
            var total = _toolCallsOrdered.Count;
            var completed = _toolCallsOrdered.Count(tc => tc.Status == ToolCallStatus.Completed);
            var failed = _toolCallsOrdered.Count(tc => tc.Status == ToolCallStatus.Failed);
            var inProgress = _toolCallsOrdered.Count(tc => tc.Status == ToolCallStatus.Started);
            return (total, completed, failed, inProgress);
        }
    }

    /// <inheritdoc />
    public void ProcessToolCallEvent(AesirToolCallInfo toolCall)
    {
        ArgumentNullException.ThrowIfNull(toolCall);

        lock (_lock)
        {
            if (_toolCallsById.TryGetValue(toolCall.ToolCallId, out var existing))
            {
                // Update existing tool call
                UpdateExistingToolCall(existing, toolCall);
            }
            else
            {
                // Add new tool call
                _toolCallsById[toolCall.ToolCallId] = toolCall;
                _toolCallsOrdered.Add(toolCall);
            }
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _toolCallsById.Clear();
            _toolCallsOrdered.Clear();
        }

        NotifyChanged();
    }

    /// <inheritdoc />
    public AesirToolCallInfo? GetToolCall(string toolCallId)
    {
        lock (_lock)
        {
            return _toolCallsById.GetValueOrDefault(toolCallId);
        }
    }

    /// <summary>
    /// Updates an existing tool call with new information.
    /// </summary>
    private static void UpdateExistingToolCall(AesirToolCallInfo existing, AesirToolCallInfo updated)
    {
        // Update status
        existing.Status = updated.Status;

        // Update completion info
        if (updated.CompletedAt.HasValue)
        {
            existing.CompletedAt = updated.CompletedAt;
        }

        // Update result or error
        if (!string.IsNullOrEmpty(updated.Result))
        {
            existing.Result = updated.Result;
        }

        if (!string.IsNullOrEmpty(updated.Error))
        {
            existing.Error = updated.Error;
        }
    }

    /// <summary>
    /// Notifies subscribers that tool calls have changed.
    /// </summary>
    private void NotifyChanged()
    {
        OnToolCallsChanged?.Invoke();
    }
}

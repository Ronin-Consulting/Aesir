using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Modules.Inference.Ollama.Services;

// NOTE: This is ripped from Semantic Kernel because they do not allow switching of model on reduction calls.

[Experimental("SKEXP0070")]

/// <summary>
/// Reduce the chat history by summarizing message past the target message count.
/// </summary>
/// <remarks>
/// Summarization will always avoid orphaning function-content as the presence of
/// a function-call _must_ be followed by a function-result.  When a threshold count is
/// is provided (recommended), reduction will scan within the threshold window in an attempt to
/// avoid orphaning a user message from an assistant response.
/// </remarks>
public class ChatHistorySummarizationReducer : IChatHistoryReducer
{
    /// <summary>
    /// Metadata key to indicate a summary message.
    /// </summary>
    public const string SummaryMetadataKey = "__summary__";

    /// <summary>
    /// The default summarization system instructions.
    /// </summary>
    public const string DefaultSummarizationPrompt =
        """
        Provide a concise and complete summarization of the entire dialog that does not exceed 5 sentences

        This summary must always:
        - Consider both user and assistant interactions
        - Maintain continuity for the purpose of further dialog
        - Include details from any existing summary
        - Focus on the most significant aspects of the dialog

        This summary must never:
        - Critique, correct, interpret, presume, or assume
        - Identify faults, mistakes, misunderstanding, or correctness
        - Analyze what has not occurred
        - Exclude details from any existing summary
        """;

    /// <summary>
    /// System instructions for summarization.  Defaults to <see cref="DefaultSummarizationPrompt"/>.
    /// </summary>
    public string SummarizationInstructions { get; init; } = DefaultSummarizationPrompt;

    /// <summary>
    /// Flag to indicate if an exception should be thrown if summarization fails.
    /// </summary>
    public bool FailOnError { get; init; } = true;

    /// <summary>
    /// Flag to indicate summarization is maintained in a single message, or if a series of
    /// summations are generated over time.
    /// </summary>
    /// <remarks>
    /// Not using a single summary may ultimately result in a chat history that exceeds the token limit.
    /// </remarks>
    public bool UseSingleSummary { get; init; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHistorySummarizationReducer"/> class.
    /// </summary>
    /// <param name="service">A <see cref="IChatCompletionService"/> instance to be used for summarization.</param>
    /// <param name="targetCount">The desired number of target messages after reduction.</param>
    /// <param name="thresholdCount">
    /// An optional number of messages beyond the target count that must be present
    /// in order to trigger reduction.
    /// </param>
    /// <param name="modelId">
    /// An optional identifier for the model to be utilized during summarization. If null, a default model is used.
    /// </param>
    /// <remarks>
    /// The threshold count, though optional, ensures reduction is not triggered for minor
    /// increments to the chat history beyond the target count. The model specified allows use
    /// of custom model configurations for reduction operations.
    /// </remarks>
    public ChatHistorySummarizationReducer(IChatCompletionService service, int targetCount, int? thresholdCount = null,
        string? modelId = null)
    {
        // Verify.NotNull(service, nameof(service));
        // Verify.True(targetCount > 0, "Target message count must be greater than zero.");
        // Verify.True(!thresholdCount.HasValue || thresholdCount > 0, "The reduction threshold length must be greater than zero.");

        _service = service;
        _targetCount = targetCount;
        _modelId = modelId;
        _thresholdCount = thresholdCount ?? 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ChatMessageContent>?> ReduceAsync(IReadOnlyList<ChatMessageContent> chatHistory,
        CancellationToken cancellationToken = default)
    {
        var systemMessage = chatHistory.FirstOrDefault(l => l.Role == AuthorRole.System);

        // Identify where summary messages end and regular history begins
        int insertionPoint = chatHistory.LocateSummarizationBoundary(SummaryMetadataKey);

        // First pass to determine the truncation index
        int truncationIndex = chatHistory.LocateSafeReductionIndex(
            _targetCount,
            _thresholdCount,
            insertionPoint,
            hasSystemMessage: systemMessage is not null);

        IEnumerable<ChatMessageContent>? truncatedHistory = null;

        if (truncationIndex >= 0)
        {
            // Second pass to extract history for summarization
            IEnumerable<ChatMessageContent> summarizedHistory =
                chatHistory.Extract(
                    UseSingleSummary ? 0 : insertionPoint,
                    truncationIndex,
                    filter: (m) => m.Items.Any(i => i is FunctionCallContent || i is FunctionResultContent));

            try
            {
                // Summarize
                ChatHistory summarizationRequest = [.. summarizedHistory, new ChatMessageContent(AuthorRole.System, SummarizationInstructions)];

                var settings = new OllamaPromptExecutionSettings
                {
                    ModelId = _modelId
                };

                ChatMessageContent summaryMessage = await _service.GetChatMessageContentAsync(
                    summarizationRequest,settings, cancellationToken: cancellationToken).ConfigureAwait(false);
                summaryMessage.Metadata = new Dictionary<string, object?> { { SummaryMetadataKey, true } };

                // Assembly the summarized history
                truncatedHistory = AssemblySummarizedHistory(summaryMessage, systemMessage);
            }
            catch
            {
                if (FailOnError)
                {
                    throw;
                }
            }
        }

        return truncatedHistory;

        // Inner function to assemble the summarized history
        IEnumerable<ChatMessageContent> AssemblySummarizedHistory(ChatMessageContent? summaryMessage, ChatMessageContent? systemMessage)
        {
            if (systemMessage is not null)
            {
                yield return systemMessage;
            }

            if (insertionPoint > 0 && !UseSingleSummary)
            {
                for (int index = 0; index <= insertionPoint - 1; ++index)
                {
                    yield return chatHistory[index];
                }
            }

            if (summaryMessage is not null)
            {
                yield return summaryMessage;
            }

            for (int index = truncationIndex; index < chatHistory.Count; ++index)
            {
                yield return chatHistory[index];
            }
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        ChatHistorySummarizationReducer? other = obj as ChatHistorySummarizationReducer;
        return other != null &&
               _thresholdCount == other._thresholdCount &&
               _targetCount == other._targetCount &&
               UseSingleSummary == other.UseSingleSummary &&
               string.Equals(SummarizationInstructions, other.SummarizationInstructions, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(nameof(ChatHistorySummarizationReducer), _thresholdCount, _targetCount, SummarizationInstructions, UseSingleSummary);

    private readonly IChatCompletionService _service;
    private readonly int _thresholdCount;
    private readonly int _targetCount;
    private readonly string? _modelId;
}

/// <summary>
/// Discrete operations used when reducing chat history.
/// </summary>
/// <remarks>
/// Allows for improved testability.
/// </remarks>
internal static class ChatHistoryReducerExtensions
{
    /// <summary>
    /// Extract a range of messages from the source history.
    /// </summary>
    /// <param name="chatHistory">The source history</param>
    /// <param name="startIndex">The index of the first message to extract</param>
    /// <param name="finalIndex">The index of the last message to extract</param>
    /// <param name="systemMessage">An optional system message content to include</param>
    /// <param name="filter">The optional filter to apply to each message</param>
    public static IEnumerable<ChatMessageContent> Extract(
        this IReadOnlyList<ChatMessageContent> chatHistory,
        int startIndex,
        int? finalIndex = null,
        ChatMessageContent? systemMessage = null,
        Func<ChatMessageContent, bool>? filter = null)
    {
        int maxIndex = chatHistory.Count - 1;
        if (startIndex > maxIndex)
        {
            yield break;
        }

        if (systemMessage is not null)
        {
            yield return systemMessage;
        }

        finalIndex ??= maxIndex;

        finalIndex = Math.Min(finalIndex.Value, maxIndex);

        for (int index = startIndex; index <= finalIndex; ++index)
        {
            if (filter?.Invoke(chatHistory[index]) ?? false)
            {
                continue;
            }

            yield return chatHistory[index];
        }
    }

    /// <summary>
    /// Identify the index of the first message that is not a summary message, as indicated by
    /// the presence of the specified metadata key.
    /// </summary>
    /// <param name="chatHistory">The source history</param>
    /// <param name="summaryKey">The metadata key that identifies a summary message.</param>
    public static int LocateSummarizationBoundary(this IReadOnlyList<ChatMessageContent> chatHistory, string summaryKey)
    {
        for (int index = 0; index < chatHistory.Count; ++index)
        {
            ChatMessageContent message = chatHistory[index];

            if (!message.Metadata?.ContainsKey(summaryKey) ?? true)
            {
                return index;
            }
        }

        return chatHistory.Count;
    }

    /// <summary>
    /// Identify the index of the first message at or beyond the specified targetCount that
    /// does not orphan sensitive content.
    /// Specifically: function calls and results shall not be separated since chat-completion requires that
    /// a function-call always be followed by a function-result.
    /// In addition, the first user message (if present) within the threshold window will be included
    /// in order to maintain context with the subsequent assistant responses.
    /// </summary>
    /// <param name="chatHistory">The source history</param>
    /// <param name="targetCount">The desired message count, should reduction occur.</param>
    /// <param name="thresholdCount">
    /// The threshold, beyond targetCount, required to trigger reduction.
    /// History is not reduces it the message count is less than targetCount + thresholdCount.
    /// </param>
    /// <param name="offsetCount">
    /// Optionally ignore an offset from the start of the history.
    /// This is useful when messages have been injected that are not part of the raw dialog
    /// (such as summarization).
    /// </param>
    /// <param name="hasSystemMessage">Indicates whether chat history contains system message.</param>
    /// <returns>An index that identifies the starting point for a reduced history that does not orphan sensitive content.</returns>
    public static int LocateSafeReductionIndex(
        this IReadOnlyList<ChatMessageContent> chatHistory,
        int targetCount,
        int? thresholdCount = null,
        int offsetCount = 0,
        bool hasSystemMessage = false)
    {
        targetCount -= hasSystemMessage ? 1 : 0;

        // Compute the index of the truncation threshold
        int thresholdIndex = chatHistory.Count - (thresholdCount ?? 0) - targetCount;

        if (thresholdIndex <= offsetCount)
        {
            // History is too short to truncate
            return -1;
        }

        // Compute the index of truncation target
        int messageIndex = chatHistory.Count - targetCount;

        // Skip function related content
        while (messageIndex >= 0)
        {
            if (!chatHistory[messageIndex].Items.Any(i => i is FunctionCallContent || i is FunctionResultContent))
            {
                break;
            }

            --messageIndex;
        }

        // Capture the earliest non-function related message
        int targetIndex = messageIndex;

        // Scan for user message within truncation range to maximize chat cohesion
        while (messageIndex >= thresholdIndex)
        {
            // A user message provides a superb truncation point
            if (chatHistory[messageIndex].Role == AuthorRole.User)
            {
                return messageIndex;
            }

            --messageIndex;
        }

        // No user message found, fallback to the earliest non-function related message
        return targetIndex;
    }
}

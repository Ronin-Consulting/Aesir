using System.Threading.Channels;

namespace Aesir.Modules.Inference.OpenAI.Stopgap;

/// <summary>
/// STOPGAP: Temporary interface for collecting reasoning_content from OpenAI-compatible APIs.
///
/// This will be removed when the OpenAI .NET SDK or Semantic Kernel adds native support
/// for reasoning_content in streaming responses.
///
/// Background: llama.cpp's llama-server returns delta.reasoning_content in SSE streams
/// for reasoning models (GPT-OSS, DeepSeek), but the OpenAI SDK drops this field.
/// </summary>
public interface IReasoningContentCollector_Stopgap : IAsyncDisposable
{
    /// <summary>
    /// Gets the channel reader for consuming reasoning content chunks.
    /// </summary>
    ChannelReader<ReasoningChunk_Stopgap> Reader { get; }

    /// <summary>
    /// Writes a reasoning content chunk to the channel.
    /// Called by the HTTP handler when it intercepts reasoning_content.
    /// </summary>
    Task WriteAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that reasoning has completed (transitioning to regular content).
    /// </summary>
    void EndReasoning();

    /// <summary>
    /// Signals that no more chunks will be written.
    /// </summary>
    void Complete();

    /// <summary>
    /// Gets whether the model is currently in "thinking" mode.
    /// </summary>
    bool IsThinking { get; }
}

/// <summary>
/// A chunk of reasoning content from the model.
/// </summary>
public readonly record struct ReasoningChunk_Stopgap(string Content, bool IsThinking);

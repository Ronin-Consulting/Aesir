using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides chat completion functionality for AI models.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Processes a chat completion request and returns a complete response.
    /// </summary>
    /// <param name="request">The chat completion request containing messages and model parameters.</param>
    /// <returns>A task representing the asynchronous operation that returns the chat completion result.</returns>
    Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request);

    /// <summary>
    /// Processes a chat completion request and returns a streamed response.
    /// </summary>
    /// <param name="request">The chat completion request containing messages and model parameters.</param>
    /// <returns>An async enumerable of streamed chat completion results.</returns>
    IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request);
}

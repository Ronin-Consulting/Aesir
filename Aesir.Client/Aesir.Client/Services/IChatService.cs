using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;

namespace Aesir.Client.Services;

/// <summary>
/// Provides methods for interacting with chat services.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Sends a chat completion request asynchronously and retrieves the result from the API.
    /// </summary>
    /// <param name="request">The request object of type <see cref="AesirChatRequest"/> containing the parameters for the chat completion.</param>
    /// <returns>A task representing the asynchronous operation, with a result of type <see cref="AesirChatResult"/> containing the response from the API.</returns>
    Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request);

    /// Streams chat completions asynchronously based on the provided request.
    /// The method returns an asynchronous enumerable that yields partial results as they are received.
    /// <param name="request">The chat request containing parameters such as model, conversation details, and configuration options for the chat session.</param>
    /// <returns>
    /// An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> objects,
    /// where each object contains incremental results of the chat completion stream.
    /// Results include message deltas and metadata, and may yield null entries if no incremental data is provided.
    /// </returns>
    IAsyncEnumerable<AesirChatStreamedResult?> ChatCompletionsStreamedAsync(AesirChatRequest request);
}
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Provides functionality for interacting with a chat system using asynchronous operations.
/// </summary>
public class ChatService(
    ILogger<ChatService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IChatService
{
    /// <summary>
    /// Represents an instance of the Flurl HTTP client utilized for making HTTP requests
    /// in the context of chat operations. This client is configured using a centralized
    /// client cache and specific configuration settings, allowing for efficient reuse across
    /// multiple requests and managing connection overhead effectively.
    /// </summary>
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ChatClient",
            configuration.GetValue<string>("Inference:Chat"));

    /// <summary>
    /// Sends a chat completion request to the API and retrieves the results asynchronously.
    /// </summary>
    /// <param name="request">The <see cref="AesirChatRequest"/> containing the parameters for the chat completion.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains an <see cref="AesirChatResult"/> with the chat completion response.</returns>
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        try
        {
            var response = await _flurlClient.Request().PostJsonAsync(request);

            return await response.GetJsonAsync<AesirChatResult>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// Asynchronously streams chat completion results based on the given request.
    /// <param name="request">
    /// An instance of <see cref="AesirChatRequest"/> containing the details for the chat query,
    /// such as the conversation context, model, and user preferences.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> objects representing
    /// the streamed responses from the chat service.
    /// </returns>
    public async IAsyncEnumerable<AesirChatStreamedResult?> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        // 1. Build the request as usual.
        var flurlRequest = _flurlClient.Request().AppendPathSegment("streamed");

        // 2. Use SendAsync to get the response as a stream.
        // This is the key change. We manually create the JSON content and tell
        // Flurl to return as soon as the response headers are read.
        var response = await flurlRequest.SendAsync(
            HttpMethod.Post,
            new Flurl.Http.Content.CapturedJsonContent(JsonSerializer.Serialize(request)),
            completionOption: HttpCompletionOption.ResponseHeadersRead
        );
        
        await using var responseStream = await response.GetStreamAsync();
        
        var asyncEnumerable = 
            JsonSerializer.DeserializeAsyncEnumerable<AesirChatStreamedResult?>(
                responseStream,
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultBufferSize = 128
                }
            );
        
        await foreach (var item in asyncEnumerable)
        {
            yield return item;
        }
    }
}
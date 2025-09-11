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
    /// Sends an agent chat completion request asynchronously and retrieves the result from the API.
    /// </summary>
    /// <param name="request">The request object of type <see cref="AesirAgentChatRequestBase"/> containing the parameters for the chat completion.</param>
    /// <returns>A task representing the asynchronous operation, with a result of type <see cref="AesirChatResult"/> containing the response from the API.</returns>
    public async Task<AesirChatResult> AgentChatCompletionsAsync(AesirAgentChatRequestBase request)
    {
        try
        {
            var response = await _flurlClient.Request()
                .AppendPathSegment("agent")
                .PostJsonAsync(request);

            return await response.GetJsonAsync<AesirChatResult>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// Streams agent chat completions asynchronously based on the provided request.
    /// The method returns an asynchronous enumerable that yields partial results as they are received.
    /// <param name="request">The chat request containing parameters such as model, conversation details, and configuration options for the chat session.</param>
    /// <returns>
    /// An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> objects,
    /// where each object contains incremental results of the chat completion stream.
    /// Results include message deltas and metadata, and may yield null entries if no incremental data is provided.
    /// </returns>
    public async IAsyncEnumerable<AesirChatStreamedResult?> AgentChatCompletionsStreamedAsync(AesirAgentChatRequestBase request)
    {
        // 1. Build the request as usual.
        var flurlRequest = _flurlClient.Request()
            .AppendPathSegment("agent")
            .AppendPathSegment("streamed");

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
    
    /// <summary>
    /// Sends a model chat completion request asynchronously and retrieves the result from the API.
    /// </summary>
    /// <param name="request">The request object of type <see cref="AesirChatRequestBase"/> containing the parameters for the chat completion.</param>
    /// <returns>A task representing the asynchronous operation, with a result of type <see cref="AesirChatResult"/> containing the response from the API.</returns>
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequestBase request)
    {
        try
        {
            var response = await _flurlClient.Request()
                .PostJsonAsync(request);

            return await response.GetJsonAsync<AesirChatResult>();
        }
        catch (FlurlHttpException ex)
        {
            await logger.LogFlurlExceptionAsync(ex);
            throw;
        }
    }

    /// Streams chat completions asynchronously based on the provided request.
    /// The method returns an asynchronous enumerable that yields partial results as they are received.
    /// <param name="request">The chat request containing parameters such as model, conversation details, and configuration options for the chat session.</param>
    /// <returns>
    /// An asynchronous enumerable of <see cref="AesirChatStreamedResult"/> objects,
    /// where each object contains incremental results of the chat completion stream.
    /// Results include message deltas and metadata, and may yield null entries if no incremental data is provided.
    /// </returns>
    public async IAsyncEnumerable<AesirChatStreamedResult?> ChatCompletionsStreamedAsync(AesirChatRequestBase request)
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
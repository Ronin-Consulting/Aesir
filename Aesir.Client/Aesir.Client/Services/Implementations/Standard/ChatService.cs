using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ChatService(
    ILogger<ChatService> logger,
    IConfiguration configuration,
    IFlurlClientCache flurlClientCache)
    : IChatService
{
    private readonly IFlurlClient _flurlClient = flurlClientCache
        .GetOrAdd("ChatClient",
            configuration.GetValue<string>("Inference:Chat"));

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

    public async IAsyncEnumerable<AesirChatStreamedResult?> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        var response =
            _flurlClient.Request().AppendPathSegment("streamed").PostJsonAsync(request).Result;

        await using var responseStream = await response.GetStreamAsync();
        
        var asyncEnumerable = 
            JsonSerializer.DeserializeAsyncEnumerable<AesirChatStreamedResult?>(
                responseStream,
                new JsonSerializerOptions()
                {
                    DefaultBufferSize = 128
                }
            );
        
        await foreach (var item in asyncEnumerable)
        {
            yield return item;
        }
    }
}
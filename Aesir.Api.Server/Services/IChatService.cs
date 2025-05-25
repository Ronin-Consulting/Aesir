using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

public interface IChatService
{
    Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request);
    
    IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request);
}

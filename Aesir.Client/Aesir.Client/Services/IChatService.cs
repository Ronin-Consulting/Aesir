using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;

namespace Aesir.Client.Services;

public interface IChatService
{
    Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request);

    IAsyncEnumerable<AesirChatStreamedResult?> ChatCompletionsStreamedAsync(AesirChatRequest request);
}
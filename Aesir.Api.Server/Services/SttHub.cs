using Microsoft.AspNetCore.SignalR;

namespace Aesir.Api.Server.Services;

public class SttHub(ISttService sttService) : Hub
{
    public async IAsyncEnumerable<string> ProcessAudioStream(IAsyncEnumerable<byte[]> audioStream)
    {
        await foreach (var chunk in sttService.GenerateTextChunksAsync(audioStream))
        {
            yield return chunk;
        }
    }
}
using Microsoft.AspNetCore.SignalR;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Represents a SignalR Hub that facilitates the real-time processing of audio streams to text
/// by communicating with an <see cref="ISttService"/> for speech-to-text functionality.
/// </summary>
public class SttHub(ISttService sttService) : Hub
{
    /// <summary>
    /// Processes an audio stream by converting audio frames into text chunks asynchronously.
    /// </summary>
    /// <param name="audioFrames">An asynchronous stream of audio frame data represented as byte arrays.</param>
    /// <returns>An asynchronous enumerable of text chunks generated from the audio frames.</returns>
    public async IAsyncEnumerable<string> ProcessAudioStream(IAsyncEnumerable<byte[]> audioFrames)
    {
        await foreach (var chunk in sttService.GenerateTextChunksAsync(audioFrames))
        {
            yield return chunk;
        }
    }
}
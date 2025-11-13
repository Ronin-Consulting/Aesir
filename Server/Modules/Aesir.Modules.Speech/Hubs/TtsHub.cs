using Aesir.Modules.Speech.Services;
using Microsoft.AspNetCore.SignalR;

namespace Aesir.Modules.Speech.Hubs;

/// <summary>
/// Represents a SignalR hub that provides real-time text-to-speech functionality by streaming audio data
/// derived from text inputs to connected clients.
/// </summary>
/// <remarks>
/// Utilizes the ITtsService for processing text-to-speech operations asynchronously, enabling efficient
/// audio streaming in chunks to improve performance and user experience in real-time scenarios.
/// </remarks>
public class TtsHub(ITtsService ttsService) : Hub
{
    /// <summary>
    /// Generates audio data in chunks from the provided text using text-to-speech (TTS) synthesis.
    /// The audio data is streamed as a series of byte arrays.
    /// </summary>
    /// <param name="text">The input text to be synthesized into audio.</param>
    /// <param name="speed">The playback speed for the synthesized audio. Default is 1.0f.</param>
    /// <returns>An asynchronous stream of byte arrays, where each byte array represents a chunk of the generated audio in WAV format.</returns>
    public async IAsyncEnumerable<byte[]> GenerateAudio(string text, float speed = 1.0f)
    {
        await foreach (var chunk in ttsService.GenerateAudioChunksAsync(text, speed))
        {
            yield return chunk;
        }
    }
}
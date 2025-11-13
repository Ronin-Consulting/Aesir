using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a service for audio playback functionalities,
/// including streaming audio and managing playback state.
/// </summary>
public interface IAudioPlaybackService : IDisposable
{
    bool IsPlaying { get; }
    
    /// <summary>
    /// Plays audio from a stream of audio chunks asynchronously.
    /// </summary>
    /// <param name="audioChunks">A stream of audio chunks represented as an asynchronous enumerable of byte arrays.</param>
    /// <returns>A task that represents the asynchronous audio playback operation.</returns>
    Task PlayStreamAsync(IAsyncEnumerable<byte[]> audioChunks);

    /// <summary>
    /// Stops the playback of the audio stream.
    /// </summary>
    void Stop();
}
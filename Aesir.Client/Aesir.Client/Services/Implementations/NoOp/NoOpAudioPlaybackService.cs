using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services.Implementations.NoOp;

/// <summary>
/// A no-operation implementation of the <see cref="IAudioPlaybackService"/> interface.
/// This class serves as a placeholder and does not perform any actual audio playback functionality.
/// </summary>
public class NoOpAudioPlaybackService : IAudioPlaybackService
{
    /// <summary>
    /// Releases resources used by the NoOpAudioPlaybackService instance.
    /// </summary>
    /// <remarks>
    /// This method is intended to dispose managed resources associated
    /// with the service when it is no longer needed. Currently, no resources
    /// are explicitly released in this implementation.
    /// </remarks>
    public void Dispose()
    {
        // TODO release managed resources here
    }

    /// <summary>
    /// Plays audio from a stream of audio chunks asynchronously.
    /// </summary>
    /// <param name="audioChunks">A stream of audio chunks represented as an asynchronous enumerable of byte arrays.</param>
    /// <returns>A task that represents the asynchronous audio playback operation.</returns>
    public Task PlayStreamAsync(IAsyncEnumerable<byte[]> audioChunks)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Stops the playback of the audio stream.
    /// </summary>
    /// <remarks>
    /// This method halts any ongoing audio playback activity.
    /// It can be used to stop playback immediately without completing
    /// the current audio chunk or stream.
    /// </remarks>
    public void Stop()
    {
        throw new System.NotImplementedException();
    }
}
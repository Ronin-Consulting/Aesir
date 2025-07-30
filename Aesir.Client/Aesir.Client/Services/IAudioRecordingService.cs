using System;
using System.Collections.Generic;
using System.Threading;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a service for audio recording functionalities,
/// including streaming audio capture and managing recording state.
/// </summary>
public interface IAudioRecordingService : IDisposable
{
    /// <summary>
    /// Starts recording audio and returns a stream of audio chunks asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the recording operation.</param>
    /// <returns>An asynchronous enumerable of audio chunks represented as byte arrays.</returns>
    IAsyncEnumerable<byte[]> StartRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the audio recording operation.
    /// </summary>
    void StopRecording();

    /// <summary>
    /// Gets a value indicating whether recording is currently active.
    /// </summary>
    bool IsRecording { get; }
}
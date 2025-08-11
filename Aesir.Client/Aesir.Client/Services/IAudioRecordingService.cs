using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a contract for audio recording services, enabling asynchronous recording,
/// managing the recording lifecycle, and detecting silence during the recording process.
/// </summary>
public interface IAudioRecordingService : IDisposable
{
    /// <summary>
    /// Raised when a period of silence is detected during an audio recording session.
    /// </summary>
    event EventHandler<SilenceDetectedEventArgs>? SilenceDetected;

    /// <summary>
    /// Starts the audio recording as an asynchronous stream of byte arrays.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable of byte arrays representing recorded audio data.</returns>
    IAsyncEnumerable<byte[]> StartRecordingAsync(CancellationToken cancellationToken = default);
    
    Task StopAsync();

    /// <summary>
    /// Indicates whether the audio recording is currently active.
    /// </summary>
    bool IsRecording { get; }
}

/// <summary>
/// Represents the event arguments for a silence detection event, which contains information
/// about the duration of a detected silent period in an audio recording context.
/// </summary>
public class SilenceDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Represents the duration of detected silence in milliseconds.
    /// </summary>
    public int SilenceDurationMs { get; set; }
}
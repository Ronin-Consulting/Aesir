using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines a contract for providing audio recording functionalities,
/// including handling the recording process, detecting silence, and managing the recording state.
/// </summary>
public interface IAudioRecordingService : IDisposable
{
    /// <summary>
    /// Occurs when a silent period is detected during an ongoing audio recording session.
    /// Provides details about the duration of silence through the associated event arguments.
    /// </summary>
    event EventHandler<SilenceDetectedEventArgs>? SilenceDetected;

    /// <summary>
    /// Starts the audio recording as an asynchronous stream of byte arrays.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous enumerable of byte arrays representing recorded audio data.</returns>
    IAsyncEnumerable<byte[]> StartRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the audio recording process asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Indicates whether the audio recording service is currently active and recording audio.
    /// </summary>
    bool IsRecording { get; }
}

/// <summary>
/// Represents the event arguments for an event indicating that silence has been detected
/// during audio recording, providing metadata such as the duration of the silent period.
/// </summary>
public class SilenceDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the duration of a detected silent period, in milliseconds, during an audio recording session.
    /// </summary>
    public int SilenceDurationMs { get; set; }
}
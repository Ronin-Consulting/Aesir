using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using MiniAudioEx.Core.StandardAPI;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Represents configuration settings for the audio playback functionality,
/// including controls for sample rate, channel configuration, and buffer size validation.
/// </summary>
public class AudioPlaybackConfig
{
    /// <summary>
    /// Provides a default configuration instance for audio playback settings.
    /// This configuration serves as a baseline with pre-defined values for
    /// audio playback, including sample rate, channel count, and buffer size.
    /// </summary>
    public static AudioPlaybackConfig Default => new();

    /// <summary>
    /// Specifies the audio sampling rate, measured in Hertz (Hz).
    /// This property determines the number of samples per second in audio data,
    /// impacting audio fidelity and compatibility with playback devices.
    /// </summary>
    public uint SampleRate { get; set; } = 22050;

    /// <summary>
    /// Specifies the number of audio channels used during playback.
    /// Common values are 1 for mono and 2 for stereo, which determine the configuration
    /// and spatial characteristics of the audio output.
    /// </summary>
    public uint Channels { get; set; } = 1;

    /// <summary>
    /// Specifies the buffer size used for validation of audio data chunks during playback.
    /// This value is used to determine the alignment and integrity of audio data, ensuring consistency with the expected format and structure.
    /// </summary>
    public int ValidationBufferSize { get; set; } = 2;
}

/// <summary>
/// A service responsible for handling audio playback operations, integrating with real-time audio streaming and audio state management.
/// </summary>
/// <remarks>
/// The <see cref="AudioPlaybackService"/> is designed to process audio streams in real time, providing methods to start and stop playback,
/// and ensuring proper resource management through its implementation of the <see cref="IDisposable"/> interface.
/// </remarks>
public sealed class AudioPlaybackService(
    ILogger<AudioPlaybackService> logger,
    AudioPlaybackConfig? config = null) : IAudioPlaybackService
{
    /// <summary>
    /// Represents a logger instance used for tracking events, errors, and informational messages
    /// within the AudioPlaybackService during audio processing and playback operations.
    /// Useful for debugging and monitoring service behavior.
    /// </summary>
    private readonly ILogger<AudioPlaybackService> _logger = logger;

    /// <summary>
    /// Holds the configuration settings for the audio playback service, including parameters such as
    /// sample rate, channels, and validation buffer size. If no configuration is provided,
    /// default settings specified in <see cref="AudioPlaybackConfig.Default"/> are used.
    /// </summary>
    private readonly AudioPlaybackConfig _config = config ?? AudioPlaybackConfig.Default;
    
    // Assumptions: All WAV chunks are 16-bit signed PCM, configurable sample rate and channels
    /// <summary>
    /// Gets the sample rate (in Hz) used for audio playback operations.
    /// Controls the number of audio samples processed per second, which
    /// affects audio fidelity and compatibility across different audio systems.
    /// </summary>
    private uint SampleRate => _config.SampleRate;

    /// <summary>
    /// Specifies the number of audio playback channels.
    /// Determines whether the audio output is mono (1 channel), stereo (2 channels),
    /// or a higher number for multi-channel audio configurations, impacting
    /// the distribution of sound across speaker systems.
    /// </summary>
    private uint Channels => _config.Channels;

    /// <summary>
    /// Indicates whether audio playback is currently active.
    /// If true, audio is actively playing; if false, playback is paused or stopped.
    /// This property is updated in real-time based on the playback state.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// A cancellation token source used to manage and signal cancellation
    /// of the current audio playback operation.
    /// This allows graceful interruption of asynchronous tasks associated with audio playback.
    /// </summary>
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Plays audio from a provided asynchronous stream of audio chunks.
    /// </summary>
    /// <param name="audioChunks">An asynchronous enumerable of byte arrays representing chunks of audio data.</param>
    /// <returns>A task representing the asynchronous operation of audio playback.</returns>
    public async Task PlayStreamAsync(IAsyncEnumerable<byte[]> audioChunks)
    {
        Stop();

        _cts = new CancellationTokenSource();

        AudioSource? audioSource = null;
        var audioApp = new AudioApp(SampleRate, Channels);
        audioApp.Loaded += () =>
        {
            audioSource = new AudioSource();
        };
        _ = Task.Run(() => audioApp.Run());

        while (audioSource == null)
        {
            await Task.Delay(100);
        }

        audioSource.End += () =>
        {
            IsPlaying = false;
        };

        try
        {
            var chunkCount = 0;
            await foreach (var chunk in audioChunks.WithCancellation(_cts.Token))
            {
                chunkCount++;
                ValidateChunk(chunk);
                
                using var clip = new AudioClip(chunk);
                audioSource.Play(clip);
                IsPlaying = true;
                _logger.LogDebug("Audio clip {ClipNumber} playing.", chunkCount);
                while (IsPlaying)
                {
                    await Task.Delay(100);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Audio stream playback was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during audio stream playback");
        }
    }

    /// <summary>
    /// Validates the provided audio chunk for size consistency and alignment with the expected format.
    /// </summary>
    /// <param name="chunk">A byte array representing an audio chunk to be validated.</param>
    private void ValidateChunk(byte[] chunk)
    {
        // Example: Check if chunk size is consistent with 16-bit PCM
        if (chunk.Length % _config.ValidationBufferSize != 0)
        {
            _logger.LogWarning("Invalid chunk size: {Length} bytes, not aligned to 16-bit PCM", chunk.Length);
        }
    }

    /// <summary>
    /// Stops the current audio playback session, cancels any active playback operations,
    /// and deinitializes the audio context, ensuring proper resource cleanup and halting all playback activities.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();

        AudioContext.Deinitialize();

        _logger.LogDebug("Audio context deinitialized");
    }

    /// <summary>
    /// Releases all resources used by the <see cref="AudioPlaybackService"/> instance.
    /// </summary>
    /// <remarks>
    /// This method stops any ongoing audio playback, deinitializes the audio context,
    /// disposes of any queued audio clips, and terminates the update thread.
    /// It ensures that all unmanaged resources and allocated memory are properly freed.
    /// Any errors encountered during the cleanup process are logged.
    /// </remarks>
    public void Dispose()
    {
        _logger.LogInformation("Disposing AudioPlaybackService");

        try
        {
            Stop();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deinitializing audio context during disposal");
        }

        _logger.LogInformation("AudioPlaybackService disposed successfully");
    }
}
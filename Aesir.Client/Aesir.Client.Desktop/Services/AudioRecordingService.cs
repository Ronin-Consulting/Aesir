using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// A service responsible for managing audio recording functionalities, leveraging the SoundFlow library
/// for audio processing and device control. This service provides capabilities to initiate, terminate, and
/// monitor audio recordings, while ensuring proper handling of their lifecycle and associated operations.
/// </summary>
public sealed class AudioRecordingService : IAudioRecordingService
{
    /// <summary>
    /// Represents the constant number of audio channels to be utilized for audio recording.
    /// Configured for optimal integration with audio processing systems, typically reflecting a mono (1 channel) setting.
    /// </summary>
    private const uint Channels = 1;

    /// <summary>
    /// Represents the fixed size, in bytes, of audio chunks to be processed during recording.
    /// Optimized for maintaining a balance between processing performance and latency,
    /// with the default value set to 4096 bytes.
    /// </summary>
    private const int ChunkSize = 4096;

    /// <summary>
    /// Defines the amplitude threshold value used to determine silence in audio processing.
    /// Any audio signal with amplitude below this value is classified as silence.
    /// Permissible range: 0.0 (absolute silence) to 1.0 (maximum signal amplitude).
    /// Utilized during audio processing for silence detection and filtering.
    /// </summary>
    private const float SilenceThreshold = 0.01f;

    /// <summary>
    /// Defines the duration in milliseconds after which the recording automatically stops
    /// if no audio input is detected. This value is used to terminate recordings when silence is sustained
    /// for the specified time interval.
    /// </summary>
    private const int SilenceTimeoutMs = 120000;

    /// <summary>
    /// Represents a logger instance used within the <see cref="AudioRecordingService"/> class
    /// to log events, warnings, errors, and debugging information during the execution
    /// of audio recording operations. Enables tracking and troubleshooting of processes
    /// related to audio recording functionality.
    /// </summary>
    private readonly ILogger<AudioRecordingService> _logger;

    /// <summary>
    /// Represents the audio engine instance used for audio processing and device management within the service.
    /// This instance is responsible for capturing, playback, and general audio operations via the SoundFlow library.
    /// </summary>
    private readonly AudioEngine _engine;

    /// <summary>
    /// Represents the audio capture device employed within the AudioRecordingService for handling audio acquisition.
    /// This private field is responsible for managing the lifecycle of the capture device, including starting, stopping, and disposing,
    /// as well as attaching event handlers for processing audio data.
    /// </summary>
    private AudioCaptureDevice? _captureDevice;

    /// <summary>
    /// Represents a private channel writer used for streaming audio data as byte array chunks within the recording process.
    /// Enables asynchronous communication from audio processing components to consumers of the audio data.
    /// Typically initialized when recording begins and completed or set to null when recording stops.
    /// </summary>
    private ChannelWriter<byte[]>? _channelWriter;

    /// <summary>
    /// Represents the cancellation token source used to signal and manage the termination of the audio recording process.
    /// Functions as a control mechanism to ensure proper handling of recording operations, including stopping and resource cleanup.
    /// </summary>
    private CancellationTokenSource? _recordingCts;

    /// <summary>
    /// Indicates the current state of the audio recording process within the service.
    /// When set to true, audio recording is actively occurring; when false, recording is stopped or has not started.
    /// This field is managed internally to ensure thread-safe updates and accurate state representation.
    /// </summary>
    private bool _isRecording;

    /// <summary>
    /// A private synchronization object used to enforce thread-safe access to shared
    /// resources within the AudioRecordingService. It ensures proper synchronization
    /// for concurrent operations like managing the recording state and interacting
    /// with audio devices.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Represents a mutable, in-memory collection of audio data as a sequence of bytes.
    /// Primarily utilized as a temporary storage buffer during the audio recording lifecycle.
    /// Enables efficient management of audio chunks by accumulating and processing data
    /// progressively before it is flushed or transmitted.
    /// </summary>
    private readonly List<byte> _audioBuffer = [];

    /// <summary>
    /// Stores the timestamp of the most recent detected audio signal.
    /// Used to track silence periods and manage features like automatic recording stop
    /// based on the absence of audio input.
    /// </summary>
    private DateTime _lastAudioDetected = DateTime.UtcNow;

    /// <summary>
    /// A timer used to monitor periods of silence during an active audio recording session.
    /// Triggers actions such as auto-stopping the recording if the silence exceeds a predefined threshold.
    /// </summary>
    private Timer? _silenceTimer;

    /// <summary>
    /// Indicates whether the audio recording service is currently active.
    /// Ensures thread-safe access to the recording state.
    /// </summary>
    public bool IsRecording
    {
        get
        {
            lock (_lock)
            {
                return _isRecording;
            }
        }
    }

    /// <summary>
    /// Service responsible for managing audio recording functionality, including operations such as
    /// starting, stopping, and handling the audio recording lifecycle. Utilizes the SoundFlow library
    /// for audio processing and interaction with audio devices.
    /// </summary>
    public AudioRecordingService(ILogger<AudioRecordingService> logger)
    {
        _logger = logger;

        try
        {
            _engine = new MiniAudioEngine();
            _logger.LogInformation("AudioRecordingService initialized successfully with SoundFlow MiniAudioEngine");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SoundFlow MiniAudioEngine");
            throw;
        }
    }

    /// <summary>
    /// Initiates an asynchronous recording of audio and provides a continuous stream of audio chunks as an enumerable.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests during the recording process.</param>
    /// <returns>An asynchronous enumerable of audio chunks represented as byte arrays.</returns>
    public async IAsyncEnumerable<byte[]> StartRecordingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Initial check without a lock to return early. The IsRecording property is thread-safe.
        if (IsRecording)
        {
            _logger.LogWarning("Recording is already in progress.");
            yield break;
        }

        AudioCaptureDevice? captureDevice;
        try
        {
            // Perform potentially blocking I/O outside of the main lock.
            var format = AudioFormat.Broadcast; // S16, Mono, 48kHz - perfect for STT service
            _logger.LogDebug("Searching for default capture device with format {Format}", format);
            
            var availableDevices = _engine.CaptureDevices.ToList();
            _logger.LogDebug("Found {DeviceCount} capture devices available", availableDevices.Count);
            
            DeviceInfo? defaultCapture = availableDevices.FirstOrDefault(x => x.IsDefault);
            if (defaultCapture == null)
            {
                _logger.LogError("No default capture device found among {DeviceCount} available devices", availableDevices.Count);
                throw new InvalidOperationException("No default capture device found.");
            }

            _logger.LogInformation("Using default capture device found");
            captureDevice = _engine.InitializeCaptureDevice(defaultCapture, format);
            _logger.LogDebug("Capture device initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize audio capture device");
            yield break; // Exit cleanly if device init fails.
        }

        Channel<byte[]> channel;
        CancellationTokenSource localCancellationTokenSource;

        lock (_lock)
        {
            // Double-check state inside the lock to prevent a race condition.
            if (_isRecording)
            {
                _logger.LogWarning("Recording started by another call while this one was initializing");
                captureDevice.Dispose(); // Clean up the newly created device.
                yield break;
            }

            _logger.LogDebug("Setting up recording channel and cancellation token");
            channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
            _channelWriter = channel.Writer;

            localCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _recordingCts = localCancellationTokenSource;

            _captureDevice = captureDevice;
            // The lambda can be replaced with a named method if it grows more complex.
            _captureDevice.OnAudioProcessed += OnAudioProcessedHandler;
            _logger.LogDebug("Audio processing event handler attached");

            _isRecording = true;
            _lastAudioDetected = DateTime.UtcNow;
            
            // Start silence detection timer
            _silenceTimer = new Timer(CheckSilenceTimeout, null, 1000, 1000); // Check every second
            _logger.LogDebug("Recording state set to active with silence detection enabled");
        }

        _logger.LogInformation("Starting audio capture device");
        _captureDevice.Start();
        _logger.LogInformation("Audio recording started successfully");

        var chunkCount = 0;
        try
        {
            // Stream from the channel. Cancellation is handled by ReadAllAsync.
            await foreach (var chunk in channel.Reader.ReadAllAsync(localCancellationTokenSource.Token))
            {
                chunkCount++;
                if (chunkCount == 1)
                {
                    _logger.LogDebug("First audio chunk received and streamed");
                }
                else if (chunkCount % 100 == 0) // Log every 100th chunk to avoid spam
                {
                    _logger.LogDebug("Processed {ChunkCount} audio chunks", chunkCount);
                }
                yield return chunk;
            }
            
            _logger.LogInformation("Audio recording stream completed. Total chunks processed: {ChunkCount}", chunkCount);
        }
        finally
        {
            // This ensures StopRecording is always called to clean up resources.
            _logger.LogDebug("Recording stream ended, initiating cleanup");
            StopRecording();
        }
    }

    /// <summary>
    /// Handles the event of receiving raw audio data captured from the audio device.
    /// This method processes the provided audio data and attempts to write it to an internal audio stream or buffer.
    /// </summary>
    /// <param name="audioData">The byte array containing the raw audio data captured from the audio device.</param>
    private void OnAudioDataReceived(byte[] audioData)
    {
        try
        {
            if (_channelWriter == null)
            {
                _logger.LogDebug("Channel writer is null, skipping audio data");
                return;
            }

            if (_recordingCts?.Token.IsCancellationRequested == true)
            {
                _logger.LogDebug("Recording cancellation requested, skipping audio data");
                return;
            }

            // Send the audio chunk to the channel (non-blocking)
            if (!_channelWriter.TryWrite(audioData))
            {
                _logger.LogWarning("Failed to write {DataSize} bytes of audio data to channel", audioData.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {DataSize} bytes of audio data", audioData?.Length ?? 0);
        }
    }

    /// <summary>
    /// Stops the current audio recording session. This method ensures that all resources related to the
    /// audio recording process are released, any buffered audio data is processed, and logging provides
    /// insights into the termination process. It handles errors gracefully and ensures proper cleanup operations.
    /// </summary>
    public void StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false; // Set this first to prevent race conditions.
            _logger.LogInformation("Stopping audio recording");

            try
            {
                _logger.LogDebug("Cancelling recording operations");
                _recordingCts?.Cancel();

                if (_captureDevice != null)
                {
                    // Detach the event handler to prevent it from firing during disposal.
                    _logger.LogDebug("Detaching audio processing event handler");
                    _captureDevice.OnAudioProcessed -= OnAudioProcessedHandler;
                    
                    _logger.LogDebug("Stopping capture device");
                    _captureDevice.Stop();
                    
                    _logger.LogDebug("Disposing capture device");
                    _captureDevice.Dispose();
                    _captureDevice = null;
                }

                if (_audioBuffer.Count > 0)
                {
                    _logger.LogInformation("Flushing {RemainingBytes} bytes from audio buffer", _audioBuffer.Count);
                    OnAudioDataReceived(_audioBuffer.ToArray());
                    _audioBuffer.Clear();
                    _logger.LogDebug("Audio buffer cleared");
                }

                _logger.LogDebug("Completing channel writer");
                _channelWriter?.TryComplete();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while stopping audio recording");
            }
            finally
            {
                _channelWriter = null;
                _recordingCts?.Dispose();
                _recordingCts = null;
                
                // Stop and dispose silence timer
                _silenceTimer?.Dispose();
                _silenceTimer = null;
                
                _logger.LogInformation("Audio recording stopped and all resources cleaned up");
            }
        }
    }

    /// <summary>
    /// Handles audio processing events generated by the capture device. Converts the provided audio
    /// samples into byte arrays, facilitates silence detection, and prepares audio chunks
    /// for subsequent processing or transmission.
    /// </summary>
    /// <param name="samples">A span of floating-point audio samples to be processed.</param>
    /// <param name="capability">Metadata or capability information related to the audio being processed.</param>
    private void OnAudioProcessedHandler(Span<float> samples, Capability capability)
    {
        try
        {
            // Calculate RMS (Root Mean Square) for silence detection
            var rms = CalculateRms(samples);
            var isAudioDetected = rms > SilenceThreshold;

            if (isAudioDetected)
            {
                _lastAudioDetected = DateTime.UtcNow;
                _logger.LogTrace("Audio detected with RMS: {Rms:F4}", rms);
            }

            var byteBuffer = new byte[samples.Length * Channels * 2];
            var byteIndex = 0;
            foreach (var sample in samples)
            {
                var val = (short)(sample * 32767f);
                byteBuffer[byteIndex++] = (byte)(val & 0xFF);
                byteBuffer[byteIndex++] = (byte)((val >> 8) & 0xFF);
            }

            lock (_lock)
            {
                _audioBuffer.AddRange(byteBuffer);

                var chunksProcessed = 0;
                while (_audioBuffer.Count >= ChunkSize)
                {
                    var chunk = new byte[ChunkSize];
                    _audioBuffer.CopyTo(0, chunk, 0, ChunkSize);
                    _audioBuffer.RemoveRange(0, ChunkSize);

                    OnAudioDataReceived(chunk);
                    chunksProcessed++;
                }

                if (chunksProcessed > 0)
                {
                    _logger.LogTrace("Processed {ChunksProcessed} audio chunks from {SampleCount} samples, {BufferSize} bytes remaining in buffer", 
                        chunksProcessed, samples.Length, _audioBuffer.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing audio samples in OnAudioProcessedHandler");
        }
    }

    /// <summary>
    /// Calculates the Root Mean Square (RMS) value of the provided audio samples.
    /// RMS is used to measure the audio level, which helps in detecting the presence of sound.
    /// </summary>
    /// <param name="samples">An array of audio samples to calculate the RMS value from.</param>
    /// <returns>A float value representing the calculated RMS of the audio samples.</returns>
    private static float CalculateRms(Span<float> samples)
    {
        if (samples.Length == 0) return 0.0f;

        var sum = 0.0f;
        foreach (var sample in samples)
        {
            sum += sample * sample;
        }

        return (float)Math.Sqrt(sum / samples.Length);
    }

    /// <summary>
    /// Timer callback method that checks whether the silence duration has exceeded the configured timeout limit.
    /// If the duration exceeds the threshold, the recording is stopped automatically.
    /// </summary>
    /// <param name="state">An optional object state parameter for the timer callback. This parameter is not utilized in this implementation.</param>
    private void CheckSilenceTimeout(object? state)
    {
        try
        {
            if (!_isRecording) return;

            var silenceDuration = DateTime.UtcNow - _lastAudioDetected;
            if (silenceDuration.TotalMilliseconds >= SilenceTimeoutMs)
            {
                _logger.LogInformation("Auto-stopping recording due to {SilenceDuration:F1} seconds of silence", 
                    silenceDuration.TotalSeconds);
                
                // Stop recording due to silence timeout
                Task.Run(StopRecording);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in silence timeout check");
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="AudioRecordingService"/> instance.
    /// </summary>
    /// <remarks>
    /// This method handles the cleanup of internal components such as the silence timer and audio engine,
    /// ensuring that any ongoing operations are stopped and all resources are disposed of properly.
    /// </remarks>
    public void Dispose()
    {
        _logger.LogInformation("Disposing AudioRecordingService");

        try
        {
            StopRecording();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping recording during disposal");
        }

        try
        {
            _silenceTimer?.Dispose();
            _logger.LogDebug("Silence timer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing silence timer");
        }
        
        try
        {
            _engine?.Dispose();
            _logger.LogDebug("SoundFlow engine disposed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing SoundFlow engine");
        }
        
        _logger.LogInformation("AudioRecordingService disposed successfully");
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// A service responsible for handling audio recording operations, utilizing the SoundFlow library
/// for audio processing and device interaction. This service supports starting, stopping, and managing
/// audio recordings as part of its functionality.
/// </summary>
public sealed class AudioRecordingService : IAudioRecordingService
{
    /// <summary>
    /// Specifies the constant number of audio channels to be used for recording.
    /// Optimized for compatibility with SoundFlow's default configurations, typically set to stereo (2 channels).
    /// </summary>
    private const uint Channels = 2;

    /// <summary>
    /// Defines the size, in bytes, of audio chunks to be captured and processed during recording.
    /// Set to 4096 bytes to ensure an optimal balance between data processing efficiency and latency.
    /// </summary>
    private const int ChunkSize = 4096;

    /// <summary>
    /// A logger instance used to log informational messages, warnings, and errors during the lifecycle and operations
    /// of the <see cref="AudioRecordingService"/>. Facilitates effective debugging and monitoring of audio recording
    /// processes and related events.
    /// </summary>
    private readonly ILogger<AudioRecordingService> _logger;

    /// <summary>
    /// Represents the instance of the SoundFlow audio engine used for managing audio processing and device interactions.
    /// Initialized as a MiniAudioEngine within the service to handle audio capture and playback functionalities.
    /// </summary>
    private readonly AudioEngine _engine;

    /// <summary>
    /// Represents the audio capture device used for recording operations in the AudioRecordingService.
    /// This device handles audio acquisition and processing through the SoundFlow library.
    /// </summary>
    private AudioCaptureDevice? _captureDevice;

    /// <summary>
    /// A private channel writer for handling the streaming of byte array audio chunks.
    /// Used internally to send audio data from the recording process to consumers through a channel.
    /// Can be null when recording is inactive or has been stopped.
    /// </summary>
    private ChannelWriter<byte[]>? _channelWriter;

    /// <summary>
    /// Provides a cancellation token source used to manage the lifecycle of the audio recording process.
    /// This is utilized to gracefully handle the start, stop, and cleanup of recording operations, ensuring proper resource management.
    /// </summary>
    private CancellationTokenSource? _recordingCts;

    /// <summary>
    /// A private field indicating the current state of the audio recording process.
    /// True if recording is active; false if recording is stopped or not started.
    /// Used internally for managing recording state in a thread-safe manner.
    /// </summary>
    private bool _isRecording;

    /// <summary>
    /// A private synchronization object used to coordinate access to shared resources
    /// within the AudioRecordingService class, ensuring thread-safe operations
    /// during recording, state management, and device interaction.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Serves as an in-memory buffer for temporarily holding audio data bytes during the audio recording process.
    /// Ensures smooth data processing by managing data chunks before they are transmitted or saved.
    /// </summary>
    private readonly List<byte> _audioBuffer = [];

    /// <summary>
    /// Indicates whether an audio recording is currently in progress.
    /// This property is thread-safe and reflects the state of the recording
    /// managed by the audio recording service.
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
    /// Service for managing audio recording using the SoundFlow library. Provides functionality to start and stop
    /// audio recording and handles the lifecycle of the recording process.
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
    /// Initiated asynchronous recording of audio, providing a continuous stream of audio chunks.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the recording operation if necessary.</param>
    /// <returns>An asynchronous enumerable of audio data chunks as byte arrays.</returns>
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
            var format = AudioFormat.Dvd;
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
            _logger.LogDebug("Recording state set to active");
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
    /// Handles the event of receiving audio data from the capture device.
    /// This method processes the audio data and sends it for further streaming or buffering operations.
    /// </summary>
    /// <param name="audioData">A byte array containing the audio data captured from the device.</param>
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
    /// Terminates the ongoing audio recording process, cleans up resources, and ensures that all
    /// remaining audio data is processed and flushed from the buffer.
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
                _logger.LogInformation("Audio recording stopped and all resources cleaned up");
            }
        }
    }

    /// <summary>
    /// Handles audio processing events from the capture device, converting audio samples to byte arrays
    /// and forwarding processed audio chunks for further handling.
    /// </summary>
    /// <param name="samples">A span of floating-point audio samples from the capture device.</param>
    /// <param name="capability">Capability information related to the audio being processed.</param>
    private void OnAudioProcessedHandler(Span<float> samples, Capability capability)
    {
        try
        {
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
    /// Releases all resources used by the current instance of the <see cref="AudioRecordingService"/>.
    /// </summary>
    /// <remarks>
    /// This method ensures the proper cleanup of resources, including stopping any ongoing
    /// audio recording operations and disposing of the audio engine.
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
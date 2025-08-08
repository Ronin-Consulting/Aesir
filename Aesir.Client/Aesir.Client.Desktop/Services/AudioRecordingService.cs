using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Configuration class for audio recording service settings, allowing for easy tuning and overrides.
/// </summary>
public class AudioRecordingConfig
{
    public static AudioRecordingConfig Default => new();
    
    /// <summary>
    /// Defines the audio sample rate for recording, specified in hertz (Hz).
    /// Determines the number of audio samples captured per second,
    /// impacting audio quality and data size.
    /// </summary>
    public int SampleRate { get; set; } = 16000;
    
    /// <summary>
    /// The number of audio samples processed per chunk. This value determines the size of each audio chunk
    /// and is calculated based on the sample rate and desired chunk duration.
    /// </summary>
    public int SamplesPerChunk { get; set; } = (16000 / 10) * 7; // 11,200 samples by default
    
    /// <summary>
    /// Represents the number of bytes per audio sample. Commonly used to determine
    /// the size of memory allocations or buffer calculations for audio processing.
    /// </summary>
    public int BytesPerSample { get; set; } = 2; // 16-bit
    
    /// <summary>
    /// Represents the Root Mean Square (RMS) amplitude threshold used to determine silence during audio recording.
    /// Audio segments with an RMS value below this threshold are classified as silence.
    /// The value can be adjusted based on the recording environment or sensitivity requirements.
    /// </summary>
    public float SilenceRmsThreshold { get; set; } = 0.02f; // Adjustable RMS threshold for silence
    
    /// <summary>
    /// The number of consecutive silent audio chunks required to trigger a silence detection event.
    /// This represents the duration of silence, where each chunk corresponds to approximately 100ms of audio.
    /// Adjust this value to change the sensitivity of silence detection.
    /// </summary>
    public int SilenceChunkThreshold { get; set; } = 5; // ~500ms of consecutive silence to trigger event
    
    /// <summary>
    /// Number of audio channels for recording (1 for mono, 2 for stereo).
    /// </summary>
    public int Channels { get; set; } = 1;
    
    /// <summary>
    /// Audio sample format for recording.
    /// </summary>
    public SampleFormat SampleFormat { get; set; } = SampleFormat.S16;
}

/// <summary>
/// Provides audio recording functionality using a capture device and recorder.
/// </summary>
/// <remarks>
/// This service enables starting and stopping an audio recording process. It captures audio in a specific format
/// and supports asynchronous stream-based data processing. The service emits audio data as chunks via an asynchronous enumerable.
/// </remarks>
public class AudioRecordingService(
    ILogger<AudioRecordingService> logger, 
    AudioRecordingConfig? config = null) : IAudioRecordingService
{
    /// <summary>
    /// Logger instance used for recording and emitting logs within the
    /// <see cref="AudioRecordingService"/> class. This is primarily used to
    /// trace the execution flow, report errors, or log informational messages
    /// during audio recording operations and related activities.
    /// </summary>
    private readonly ILogger<AudioRecordingService> _logger = logger;
    
    /// <summary>
    /// Configuration settings for the audio recording service.
    /// </summary>
    private readonly AudioRecordingConfig _config = config ?? AudioRecordingConfig.Default;

    /// <summary>
    /// Represents the audio engine responsible for managing audio device initialization
    /// and operations in the recording service. This includes updating device information,
    /// handling capture devices, and providing access to audio processing functionality.
    /// </summary>
    private readonly MiniAudioEngine? _engine = new();

    /// <summary>
    /// Represents the audio capture device used for recording audio input.
    /// It is initialized with the audio engine and configured to capture audio
    /// in a specified format.
    /// </summary>
    private AudioCaptureDevice? _captureDevice; // Updated to use AudioCaptureDevice from docs

    /// <summary>
    /// Represents the recorder instance used to capture audio data from the configured audio capture device.
    /// Manages the audio recording lifecycle, including starting, stopping, and processing audio samples.
    /// </summary>
    private Recorder? _recorder;

    /// <summary>
    /// Represents a channel used for asynchronously transferring audio data in byte array chunks.
    /// Acts as a communication mechanism between the audio capture process and consumers,
    /// facilitating unbounded, thread-safe buffering of audio samples.
    /// </summary>
    private Channel<byte[]>? _audioChannel;

    /// <summary>
    /// A buffer used to store audio samples temporarily during the recording process.
    /// Samples are enqueued as they are captured, typically in float format.
    /// </summary>
    private readonly Queue<float> _sampleBuffer = new();

    /// <summary>
    /// A locking mechanism used to synchronize access to the sample buffer during audio recording.
    /// Ensures thread-safe operations when processing or clearing the buffer.
    /// </summary>
    private readonly Lock _bufferLock = new();
    
    /// <summary>
    /// Tracks the number of consecutive silent audio chunks detected during recording.
    /// This variable is incremented for each silent chunk and reset to zero
    /// when a non-silent chunk is encountered. Used to identify prolonged silence
    /// and trigger the <see cref="SilenceDetected"/> event if the configured threshold is reached.
    /// </summary>
    private int _consecutiveSilentChunks;

    /// <summary>
    /// Indicates whether the recording process is currently active.
    /// This property reflects the active state of the recording operation and can be used
    /// to determine if audio is being captured at a given time.
    /// </summary>
    public bool IsRecording { get; private set; }

    /// <summary>
    /// Event triggered when a silent period is identified during the recording process
    /// based on the predefined silence detection criteria.
    /// Allows subscribers to handle or react to periods of detected silence.
    /// </summary>
    public event EventHandler<SilenceDetectedEventArgs>? SilenceDetected;

    /// Begins an asynchronous audio recording process and returns a stream of byte arrays representing audio chunks.
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. Recording stops when the token is canceled.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of byte arrays, where each byte array represents a chunk of recorded audio data.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the recording process is already active.
    /// </exception>
    public async IAsyncEnumerable<byte[]> StartRecordingAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (IsRecording) throw new InvalidOperationException("Recording already started.");
        cancellationToken.Register(StopRecording);

        _audioChannel = Channel.CreateUnbounded<byte[]>();

        // Define format and initialize capture device
        var format = new AudioFormat
        {
            SampleRate = _config.SampleRate,
            Channels = _config.Channels,
            Format = _config.SampleFormat
        };
        _engine!.UpdateDevicesInfo();
        var defaultCapture = _engine!.CaptureDevices.First(d => d.IsDefault);
        
        _captureDevice = _engine.InitializeCaptureDevice(defaultCapture, format);
        _captureDevice.Start();

        // Setup recorder with callback
        _recorder = new Recorder(_captureDevice, ProcessAudioCallback); // Assuming this takes engine; adjust if needed
        _recorder.StartRecording();
        IsRecording = true;

        // Yield from channel
        await foreach (var chunk in _audioChannel.Reader.ReadAllAsync(cancellationToken))
            yield return chunk;
    }

    /// <summary>
    /// Stops the ongoing audio recording process if it is currently active.
    /// </summary>
    /// <remarks>
    /// This method halts the audio capture and releases resources associated with the recording process.
    /// If there is buffered audio data yet to be processed, it will finalize and ensure no data is left unprocessed.
    /// Calling this method when no recording is active is safe and will return without performing any operations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if an attempt is made to stop recording while the system is in an invalid state.
    /// </exception>
    public void StopRecording()
    {
        if (!IsRecording) return;

        _recorder?.StopRecording();
        _captureDevice?.Stop();
        IsRecording = false;

        // Yield any remaining samples
        lock (_bufferLock)
        {
            if (_sampleBuffer.Count > 0)
            {
                WriteChunk(_sampleBuffer.ToArray());
                _sampleBuffer.Clear();
            }
        }

        _audioChannel?.Writer.Complete();
    }

    /// <summary>
    /// Processes audio samples in chunks and performs operations such as detecting silence,
    /// calculating RMS, and enqueuing processed audio chunks for further consumption.
    /// </summary>
    /// <param name="samples">The span of audio samples captured, represented as floating-point values.</param>
    /// <param name="capability">The capability settings associated with the audio processing.</param>
    private void ProcessAudioCallback(Span<float> samples, Capability capability)
    {
        lock (_bufferLock)
        {
            foreach(var sample in samples)
            {
                _sampleBuffer.Enqueue(sample);
            }
            
            while (_sampleBuffer.Count >= _config.SamplesPerChunk)
            {
                var chunkSamples = new float[_config.SamplesPerChunk];
                for (var i = 0; i < _config.SamplesPerChunk; i++)
                {
                    chunkSamples[i] = _sampleBuffer.Dequeue();
                }
                _logger.LogDebug("Writing sample chunk of length {SampleChunkLength}", chunkSamples.Length);

                // Detect silence
                var rms = CalculateRms(chunkSamples);
                if (rms < _config.SilenceRmsThreshold)
                {
                    _consecutiveSilentChunks++;
                    if (_consecutiveSilentChunks >= _config.SilenceChunkThreshold)
                    {
                        OnSilenceDetected(new SilenceDetectedEventArgs { SilenceDurationMs = _consecutiveSilentChunks * 100 }); // ~100ms per chunk
                        // Do not reset here; consumer decides to stop or continue
                    }
                }
                else
                {
                    _consecutiveSilentChunks = 0; // Reset on speech
                }

                WriteChunk(chunkSamples);
            }
        }
    }

    /// <summary>
    /// Calculates the root mean square (RMS) of an array of audio samples.
    /// </summary>
    /// <param name="samples">An array of float audio samples to calculate the RMS.</param>
    /// <returns>The calculated RMS value as a float.</returns>
    private float CalculateRms(float[] samples)
    {
        float sumSquares = 0;
        foreach (var sample in samples)
        {
            sumSquares += sample * sample;
        }
        return (float)Math.Sqrt(sumSquares / samples.Length);
    }

    /// <summary>
    /// Invokes the SilenceDetected event when silence is detected during audio recording.
    /// </summary>
    /// <param name="e">The event data containing details about the detected silence.</param>
    private void OnSilenceDetected(SilenceDetectedEventArgs e)
    {
        SilenceDetected?.Invoke(this, e);
    }

    /// <summary>
    /// Converts an array of floating-point audio samples into a byte array in PCM format and writes the resulting byte array to the audio channel.
    /// </summary>
    /// <param name="samples">An array of floating-point audio samples, each representing audio data in the range of -1.0 to 1.0.</param>
    private void WriteChunk(float[] samples)
    {
        var byteChunk = ArrayPool<byte>.Shared.Rent(samples.Length * _config.BytesPerSample);
        try
        {
            for (var i = 0; i < samples.Length; i++)
            {
                var pcmValue = (short)(samples[i] * 32767);
                BitConverter.GetBytes(pcmValue).CopyTo(byteChunk, i * _config.BytesPerSample);
            }
            _audioChannel?.Writer.TryWrite(byteChunk.AsSpan(0, samples.Length * _config.BytesPerSample).ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(byteChunk, clearArray: true);
        }
    }

    /// <summary>
    /// Releases the resources used by the <see cref="AudioRecordingService"/> class.
    /// </summary>
    /// <remarks>
    /// This method ensures that any ongoing audio recording is stopped, and the underlying audio
    /// capture device and engine are disposed of properly to free up system resources. Additionally,
    /// it suppresses finalization for the instance, preventing the garbage collector from calling the finalizer.
    /// </remarks>
    public void Dispose()
    {
        StopRecording();
        _captureDevice?.Dispose(); // If IDisposable
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }
}
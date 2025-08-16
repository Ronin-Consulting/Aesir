using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Aesir.Client.Services;
using Microsoft.Extensions.Logging;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace Aesir.Client.Desktop.Services;

/// <summary>
/// Represents configuration settings for controlling the behavior of the audio recording service,
/// including parameters such as sample rate, chunk size, silence threshold, and audio format.
/// </summary>
public class AudioRecordingConfig
{
    /// <summary>
    /// Gets the default configuration instance for the audio recording service.
    /// Provides pre-set values that can be used as a baseline for custom configurations.
    /// </summary>
    public static AudioRecordingConfig Default => new();

    /// <summary>
    /// Represents the audio sample rate for the recording process, measured in hertz (Hz).
    /// Specifies the number of audio samples captured per second, influencing both quality and processing requirements.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Specifies the number of audio samples processed in each chunk during recording.
    /// Affects the granularity of audio processing and determines how audio data
    /// is segmented for tasks such as silence detection or chunk-based streaming.
    /// </summary>
    public int SamplesPerChunk { get; set; } = (16000 / 10) * 6; // 9600 samples by default

    /// <summary>
    /// Specifies the number of bytes used to store a single audio sample in PCM format.
    /// Determines the bit depth of the sample, with common values being 2 bytes for 16-bit audio.
    /// Impacts the precision and size of the audio data.
    /// </summary>
    public int BytesPerSample { get; set; } = 2; // 16-bit

    /// <summary>
    /// Specifies the root mean square (RMS) amplitude threshold used to detect silence in the audio signal.
    /// Values below this threshold are considered silent, impacting silence detection logic.
    /// </summary>
    public float SilenceRmsThreshold { get; set; } = 0.03f; // Adjustable RMS threshold for silence

    /// <summary>
    /// Specifies the number of consecutive silent chunks required to trigger the silence detection event.
    /// A larger value ensures that only prolonged silence is detected, while a smaller value increases sensitivity.
    /// Measured based on the RMS threshold defined by <see cref="SilenceRmsThreshold"/>.
    /// </summary>
    public int SilenceChunkThreshold { get; set; } = 3; // ~300ms of consecutive silence to trigger event

    /// <summary>
    /// Specifies the number of audio channels used in recording. Determines whether audio
    /// is captured in mono (1 channel) or stereo (2 channels). Impacts the captured audio
    /// data format and overall size.
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// Specifies the format of audio samples, determining the data type and bit-depth representation.
    /// Common formats include 16-bit signed integer and float, influencing audio quality and precision.
    /// </summary>
    public SampleFormat SampleFormat { get; set; } = SampleFormat.S16;
}

/// <summary>
/// Provides audio recording functionality, supporting asynchronous audio data processing, event-driven silence detection, and seamless control of audio capture operations.
/// </summary>
/// <remarks>
/// The service offers tools for managing audio recording sessions, including starting and stopping recordings and emitting audio data in a chunked format via asynchronous enumerable.
/// It also integrates with a configured logger and optionally uses the provided recording configuration to manage settings.
/// </remarks>
public class AudioRecordingService(
    ILogger<AudioRecordingService> logger,
    AudioRecordingConfig? config = null) : IAudioRecordingService
{
    /// <summary>
    /// Represents an instance of <see cref="ILogger"/> used for logging messages
    /// related to the operation and state of the <see cref="AudioRecordingService"/>.
    /// Facilitates logging of diagnostic information, error messages, and execution traces.
    /// </summary>
    private readonly ILogger<AudioRecordingService> _logger = logger;

    /// <summary>
    /// Represents the configuration settings used by the audio recording service.
    /// Defines adjustable parameters, such as sample rate, audio format, and thresholds,
    /// used to control recording behavior and process audio data.
    /// </summary>
    private readonly AudioRecordingConfig _config = config ?? AudioRecordingConfig.Default;

    /// <summary>
    /// Represents the audio engine used to manage audio device operations and configurations
    /// within the recording service. It handles initialization, device enumeration, and other
    /// functionalities required for audio capturing and processing.
    /// </summary>
    private readonly MiniAudioEngine? _engine = new();

    /// <summary>
    /// Represents the audio capture device utilized for recording audio input.
    /// Manages audio acquisition according to the specified audio configuration
    /// within the recording service.
    /// </summary>
    private AudioCaptureDevice? _captureDevice; // Updated to use AudioCaptureDevice from docs

    /// <summary>
    /// Represents the private instance of the recorder used to handle audio capture operations.
    /// Responsible for managing the audio recording process, including initialization,
    /// starting, stopping, and processing audio data captured from the audio capture device.
    /// </summary>
    private Recorder? _recorder;

    /// <summary>
    /// A private channel used for asynchronously transferring audio data in byte array chunks.
    /// Enables unbounded, thread-safe buffering and communication between the audio capture process
    /// and audio data consumers.
    /// </summary>
    private Channel<byte[]>? _audioChannel;

    /// <summary>
    /// A buffer used to temporarily store audio samples during the recording process.
    /// It holds audio data in float format, which is processed incrementally into chunks.
    /// </summary>
    private readonly Queue<float> _sampleBuffer = new();

    /// <summary>
    /// A synchronization object used to control concurrent access to the audio buffer
    /// during recording operations. Ensures thread safety when modifying or accessing
    /// the shared buffer in multi-threaded environments.
    /// </summary>
    private readonly Lock _bufferLock = new();

    /// <summary>
    /// Tracks the number of consecutive silent audio chunks observed during the recording process.
    /// The variable increments with each detected silent chunk and resets to zero upon encountering
    /// a non-silent chunk. It is primarily used to detect extended periods of silence and trigger
    /// events or actions when a predefined silence threshold is exceeded.
    /// </summary>
    private int _consecutiveSilentChunks;

    /// <summary>
    /// Indicates whether the audio recording is currently active.
    /// Returns true if recording is in progress; otherwise, false.
    /// </summary>
    public bool IsRecording { get; private set; }

    /// <summary>
    /// An event triggered when a prolonged period of silence is detected during audio recording.
    /// It provides details about the duration of the detected silence through its event arguments.
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
        cancellationToken.Register(async () => await StopAsync());

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


    /// Stops the currently active audio recording process and performs cleanup of associated resources.
    /// <returns>
    /// A task that represents the asynchronous operation to stop recording. If no recording is active, the task completes immediately.
    /// </returns>
    public Task StopAsync()
    {
        if (!IsRecording) return Task.CompletedTask;

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

        _audioChannel?.Writer.TryComplete();

        _consecutiveSilentChunks = 0;
        
        return Task.CompletedTask;
    }

    /// Processes audio samples in chunks and performs operations such as detecting silence,
    /// calculating RMS values, and managing the buffering of audio data for subsequent use.
    /// <param name="samples">
    /// The span of audio samples captured, represented as an array of floating-point values.
    /// </param>
    /// <param name="capability">
    /// The capability settings used to configure the behavior of the audio processing logic.
    /// </param>
    private void ProcessAudioCallback(Span<float> samples, Capability capability)
    {
        lock (_bufferLock)
        {
            foreach (var sample in samples)
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
                //_logger.LogDebug("Writing sample chunk of length {SampleChunkLength}", chunkSamples.Length);

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
    /// <param name="samples">
    /// An array of float audio samples to calculate the RMS.
    /// </param>
    /// <returns>
    /// The calculated RMS value as a float.
    /// </returns>
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
    /// <param name="e">
    /// The event data containing information about the duration of the detected silent period.
    /// </param>
    private void OnSilenceDetected(SilenceDetectedEventArgs e)
    {
        SilenceDetected?.Invoke(this, e);
    }

    /// <summary>
    /// Converts an array of floating-point audio samples into a byte array in PCM format and writes the resulting byte array to the audio channel.
    /// </summary>
    /// <param name="samples">
    /// An array of floating-point audio samples. Each value represents normalized audio data in the range of -1.0 to 1.0.
    /// </param>
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

    /// Disposes the resources used by the AudioRecordingService instance.
    /// <remarks>
    /// This method ensures proper cleanup by stopping any active audio recording processes
    /// and releasing resources associated with the audio capture device and engine.
    /// Suppresses finalization to optimize garbage collection.
    /// </remarks>
    public void Dispose()
    {
        StopAsync();
        _captureDevice?.Dispose(); // If IDisposable
        _engine?.Dispose();
        GC.SuppressFinalize(this);
    }
}
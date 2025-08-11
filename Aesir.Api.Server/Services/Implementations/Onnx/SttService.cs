using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SherpaOnnx;
using Whisper.net;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// Defines the configuration options for Speech-to-Text (STT) processing,
/// allowing tailored settings including model paths, language preference,
/// sensitivity parameters, execution threads, and hardware acceleration capabilities.
/// </summary>
public class SttConfig
{
    /// <summary>
    /// Provides a static instance of the <see cref="SttConfig"/> class initialized with default values,
    /// allowing for quick setup of the Speech-to-Text (STT) configuration settings.
    /// </summary>
    public static SttConfig Default => new SttConfig();

    /// <summary>
    /// Gets or sets the file path to the Whisper model used for speech-to-text inference.
    /// </summary>
    public string WhisperModelPath { get; set; } = "ggml-base.bin";

    /// <summary>
    /// Gets or sets the language configuration for the Whisper model used in the Speech-to-Text (STT) service.
    /// Determines the language to be recognized in the audio input. Defaults to "en" (English).
    /// </summary>
    public string WhisperLanguage { get; set; } = "en";

    /// <summary>
    /// Gets or sets the temperature value used in the Whisper model for controlling randomness in text generation.
    /// Lower values result in more deterministic outputs, while higher values introduce more variability.
    /// </summary>
    public float WhisperTemperature { get; set; } = 0.2f;

    /// <summary>
    /// Gets or sets the file path to the Voice Activity Detection (VAD) model used for audio processing in the STT service.
    /// </summary>
    public string VadModelPath { get; set; } = "silero-vad.onnx";

    /// <summary>
    /// Represents the threshold value for Voice Activity Detection (VAD), used to distinguish between speech and non-speech segments in audio processing.
    /// </summary>
    public float VadThreshold { get; set; } = 0.3f; // Slightly lower for better sensitivity per tuning tips

    /// <summary>
    /// Gets or sets the minimum duration of silence that will be considered as a pause during speech recognition.
    /// </summary>
    public float MinSilenceDuration { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the minimum duration of speech, in seconds, required to process audio input for recognition.
    /// </summary>
    public float MinSpeechDuration { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the size of the window in frames for voice activity detection (VAD).
    /// This value determines the length of the analysis segment used during VAD processing.
    /// </summary>
    public int VadWindowSize { get; set; } = 512;

    /// <summary>
    /// Gets or sets the maximum allowable duration, in seconds, for speech input to be processed by the speech-to-text service.
    /// </summary>
    public float MaxSpeechDuration { get; set; } = 15f; // Higher for longer utterances

    /// <summary>
    /// Gets or sets the audio sample rate in hertz, commonly used for audio processing.
    /// Higher values support better quality for longer utterances.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Gets or sets the number of threads to be used for processing, initialized with a default value based
    /// on the system's processor count with a maximum of four.
    /// </summary>
    public int NumThreads { get; set; } = Math.Min(Environment.ProcessorCount / 2, 4);

    /// <summary>
    /// Gets or sets the debug level for the STT configuration, typically used for logging or troubleshooting purposes.
    /// </summary>
    public int Debug { get; set; } = 0;

    /// <summary>
    /// Indicates whether CUDA acceleration is enabled for the speech-to-text inference process.
    /// </summary>
    public bool CudaEnabled { get; set; } = false;
}

/// <summary>
/// Provides a service for performing speech-to-text operations utilizing Whisper and VAD (Voice Activity Detection) models.
/// This service handles streaming audio input and generates transcribed text in an asynchronous manner.
/// </summary>
public class SttService : ISttService, IDisposable
{
    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> used for recording log messages related to the operation and behavior of the class.
    /// </summary>
    private readonly ILogger<SttService> _logger;

    /// <summary>
    /// Represents an instance of WhisperFactory used for managing and processing speech-to-text operations.
    /// </summary>
    private readonly WhisperFactory _whisperFactory;

    /// <summary>
    /// Stores the configuration settings for the Voice Activity Detection (VAD) model used within the Speech-to-Text (STT) service.
    /// </summary>
    private readonly VadModelConfig _vadModelConfig;

    /// <summary>
    /// Represents the configuration settings used by the STT service to manage its operational parameters.
    /// </summary>
    private readonly SttConfig _config;

    /// <summary>
    /// The SttService class is responsible for speech-to-text processing utilizing ONNX models.
    /// It employs advanced configurations and logging capabilities to facilitate the transcription
    /// of audio into textual data.
    /// </summary>
    public SttService(
        ILogger<SttService> logger,
        SttConfig? config = null)
    {
        _config = config ?? new SttConfig();

        _logger = logger;
        _whisperFactory = WhisperFactory.FromPath(_config.WhisperModelPath);

        var vadProvider = _config.CudaEnabled ? "cuda" : "cpu";
        var isArm = RuntimeInformation.OSArchitecture == Architecture.Arm64;
        if (OperatingSystem.IsMacOS() && isArm)
        {
            vadProvider = "coreml";
        }

        // Threshold = 0.5f:
        //
        // Description: The probability threshold (between 0.0 and 1.0) above which an audio chunk is classified as speech. The model outputs a speech probability for each processed chunk; if it's above this value, it's considered speech.
        // Default Value: 0.5 (50% probability).
        // Purpose: Controls sensitivity to speech detection. A lower value (e.g., 0.2) makes the VAD more sensitive, capturing quieter or hesitant speech but risking false positives (e.g., detecting noise as speech). A higher value (e.g., 0.7) reduces false positives but might miss subtle speech.
        // Tuning Tip: Tune per dataset—0.5 is a "lazy" good starting point for most audio, as noted in Sherpa-Onnx docs. In noisy environments (e.g., military ops in AESIR), increase it; for clear recordings, decrease it.
        //
        // MinSilenceDuration = 0.5f:
        //
        // Description: The minimum duration (in seconds) of silence required to end a detected speech segment. This acts as a "debounce" to avoid prematurely splitting speech due to brief pauses (e.g., breaths).
        // Default Value: 0.5 seconds.
        // Purpose: At the end of a detected speech chunk, the VAD waits for at least this duration of silence before marking the speech segment as complete and separating it. It prevents fragmenting natural speech with short silences.
        // Tuning Tip: Increase for languages with longer pauses (e.g., 1.0s for thoughtful speech); decrease for fast-paced audio. Values <=0 are invalid and would cause errors.
        //
        // MinSpeechDuration = 0.25f:
        //
        // Description: The minimum duration (in seconds) required for a detected audio segment to be considered valid speech.
        // Default Value: 0.25 seconds.
        // Purpose: Filters out very short bursts of sound that might be noise or non-speech (e.g., clicks). Only segments longer than this are accepted as speech.
        // Tuning Tip: Lower for detecting short words/commands (e.g., 0.1s in command-based STT); higher to ignore brief noises. Must be >0.
        //
        // WindowSize = 512
        //
        // Description: The number of audio samples in each processing window (chunk) fed to the VAD model.
        // Default Value: 512 samples (hex 0x0200 is just a comment notation for 512).
        // Purpose: Defines the granularity of audio analysis. For a 16kHz sample rate (common in speech), this equates to about 32ms per window (512 / 16000 = 0.032s). The model processes these chunks to compute speech probabilities.
        // Tuning Tip: Silero models are trained on specific sizes: 512, 1024, or 1536 for 16kHz; 256, 512, or 768 for 8kHz. Using non-standard values may degrade performance. Match your audio's sample rate—e.g., stick to 512 for balanced latency and accuracy in AESIR's real-time STT.
        //
        // MaxSpeechDuration = 5f:
        //
        // Description: The maximum allowed duration (in seconds) for a continuous speech segment before adjusting detection.
        // Default Value: 5 seconds.
        // Purpose: If a speech segment exceeds this, the VAD dynamically increases the threshold (e.g., to 0.9) to force a split, preventing overly long segments (useful for streaming). After splitting, the threshold resets to its original value.
        // Tuning Tip: Increase for long monologues (e.g., 20s for lectures); decrease for short interactions. Must be >0. This helps in scenarios like AESIR's voice-enabled ops where segments need to be processed in real-time without hanging.

        _vadModelConfig = new VadModelConfig()
        {
            SileroVad = new SileroVadModelConfig
            {
                Model = _config.VadModelPath,
                Threshold = _config.VadThreshold,
                MinSilenceDuration = _config.MinSilenceDuration,
                MinSpeechDuration = _config.MinSpeechDuration,
                WindowSize = _config.VadWindowSize,
                MaxSpeechDuration = _config.MaxSpeechDuration
            },
            SampleRate = _config.SampleRate,
            NumThreads = _config.NumThreads,
            Debug = _config.Debug,
            Provider = vadProvider
        };
    }

    /// <summary>
    /// Asynchronously generates transcribed text chunks from a stream of audio data in byte arrays.
    /// </summary>
    /// <param name="audioStream">
    /// An asynchronous enumerable of byte arrays, where each array represents a chunk of audio data.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete, allowing operation cancellation.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of transcribed text chunks as strings.
    /// </returns>
    public async IAsyncEnumerable<string> GenerateTextChunksAsync(
        IAsyncEnumerable<byte[]> audioStream, CancellationToken cancellationToken = default)
    {
        await using var whisperProcessor = _whisperFactory.CreateBuilder()
            .WithTemperature(_config.WhisperTemperature)
            .WithLanguage(_config.WhisperLanguage)
            .WithThreads(_config.NumThreads)
            .Build();

        using var vadProcessor = new VadProcessor(_logger, _vadModelConfig, whisperProcessor);

        await foreach (var audioChunk in audioStream.WithCancellation(cancellationToken))
        {
            // Check for cancellation before processing each chunk
            cancellationToken.ThrowIfCancellationRequested();
            
            await foreach (var text in vadProcessor.ProcessChunkAsync(audioChunk, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return text;
            }
        }

        // After stream ends, flush and yield any trailing segments
        await foreach (var text in vadProcessor.FlushAndYieldAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return text;
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="SttService"/> class.
    /// This includes the disposal of internal resources like the WhisperFactory instance
    /// and ensures proper cleanup of memory by suppressing finalization.
    /// </summary>
    public void Dispose()
    {
        _whisperFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Internal processor (handling byte[] input)
/// <summary>
/// Responsible for processing audio input through voice activity detection (VAD),
/// enabling segmentation and transcription of speech data into text using Speech-to-Text (STT) models.
/// </summary>
internal class VadProcessor : IDisposable
{
    /// <summary>
    /// Represents a logging instance used to log messages within the <see cref="VadProcessor"/> class,
    /// including debug information, warnings, and transcriptions generated during voice activity detection (VAD).
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Stores configuration settings for the Voice Activity Detection (VAD) process used within the <see cref="VadProcessor"/>.
    /// This includes essential parameters such as window size and sample rate required for audio segmentation and speech detection.
    /// </summary>
    private readonly VadModelConfig _vadConfig;

    /// <summary>
    /// Provides functionality to process audio streams for transcription using Whisper.
    /// Handles audio input and interacts with the Whisper.NET transcription library
    /// to generate textual outputs from voice data.
    /// </summary>
    private readonly WhisperProcessor _whisperProcessor;

    /// <summary>
    /// The voice activity detector (VAD) responsible for analyzing
    /// audio input, detecting segments of speech activity, and
    /// segmenting the audio for further processing.
    /// </summary>
    private readonly VoiceActivityDetector _vad;

    /// <summary>
    /// Holds the unprocessed audio samples that remain after processing a segment.
    /// These samples are carried over to the next processing iteration to ensure
    /// continuity in voice activity detection (VAD) and transcription.
    /// </summary>
    private readonly List<float> _remainingSamples = [];

    /// <summary>
    /// The VadProcessor class is responsible for processing audio data by performing voice activity detection (VAD)
    /// and converting detected voice segments into text using a specified speech-to-text (STT) model.
    /// </summary>
    public VadProcessor(ILogger logger, VadModelConfig vadConfig, WhisperProcessor whisperProcessor)
    {
        _logger = logger;
        _vadConfig = vadConfig;
        _whisperProcessor = whisperProcessor;
        _vad = new VoiceActivityDetector(_vadConfig, 60);
    }

    /// <summary>
    /// Asynchronously processes a chunk of audio data, detects speech regions, and returns transcribed text.
    /// </summary>
    /// <param name="audioChunk">The audio data to process, represented as a byte array in 16-bit PCM format.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the operation should be canceled.</param>
    /// <returns>An asynchronous stream of transcribed text for detected speech regions in the audio data.</returns>
    public async IAsyncEnumerable<string> ProcessChunkAsync(byte[] audioChunk,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (audioChunk == null || audioChunk.Length == 0)
        {
            yield break; // Early exit for empty chunks
        }

        if (audioChunk.Length % 2 != 0)
        {
            _logger.LogWarning("Invalid audio chunk length (not even for 16-bit PCM): {Length}", audioChunk.Length);
            yield break; // Or throw ArgumentException if preferred
        }

        // Convert byte[] (16-bit PCM) to float[] for VAD
        var newSamples = new float[audioChunk.Length / 2];
        for (var i = 0; i < newSamples.Length; i++)
        {
            newSamples[i] = BitConverter.ToInt16(audioChunk, i * 2) / 32768f;
        }

        _remainingSamples.AddRange(newSamples);

        var windowSize = _vadConfig.SileroVad.WindowSize;
        var numberOfIterations = _remainingSamples.Count / windowSize;

        for (var i = 0; i < numberOfIterations; ++i)
        {
            var start = i * windowSize;
            var windowedSamples = new float[windowSize];
            _remainingSamples.CopyTo(start, windowedSamples, 0, windowSize);

            _vad.AcceptWaveform(windowedSamples);

            while (!_vad.IsEmpty())
            {
                var segment = _vad.Front();
                using var wavStream = CreateWavStream(segment.Samples, _vadConfig.SampleRate, 1, 16);
                wavStream.Position = 0;

                // Transcribe with Whisper
                var transcription = string.Empty;
                await foreach (var result in _whisperProcessor.ProcessAsync(wavStream, cancellationToken))
                {
                    transcription += result.Text;
                }
                
                if (!string.IsNullOrEmpty(transcription))
                {
                    _logger.LogDebug("Transcribed utterance: {Transcription}", transcription); // Use Debug for less spam
                    yield return transcription;
                }

                _vad.Pop();
            }
        }

        // Remove processed samples, keep remainder
        _remainingSamples.RemoveRange(0, numberOfIterations * windowSize);
    }

    /// <summary>
    /// Flushes any remaining voice activity detection (VAD) segments and yields their transcriptions.
    /// This method is used to ensure that any trailing audio input is processed and transcribed
    /// after the audio stream has ended.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. This allows the operation to be cancelled before completion.
    /// </param>
    /// <returns>
    /// An asynchronous stream of transcribed text for any remaining speech segments detected by VAD.
    /// </returns>
    public async IAsyncEnumerable<string> FlushAndYieldAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _vad.Flush();
        while (!_vad.IsEmpty())
        {
            var segment = _vad.Front();
            using var wavStream = CreateWavStream(segment.Samples, _vadConfig.SampleRate, 1, 16);
            wavStream.Position = 0;

            var transcription = string.Empty;
            await foreach (var result in _whisperProcessor.ProcessAsync(wavStream, cancellationToken))
            {
                transcription += result.Text;
            }
            
            if (!string.IsNullOrEmpty(transcription))
            {
                _logger.LogDebug("Transcribed utterance: {Transcription}", transcription);
                yield return transcription;
            }

            _vad.Pop();
        }
    }

    /// <summary>
    /// Creates a WAV audio stream from the provided samples using the specified parameters.
    /// </summary>
    /// <param name="samples">An array of floating-point PCM samples, where each sample is in the range of -1.0 to 1.0.</param>
    /// <param name="sampleRate">The sample rate of the audio in Hz, specifying the number of samples per second.</param>
    /// <param name="channels">The number of audio channels (e.g., 1 for mono, 2 for stereo).</param>
    /// <param name="bitsPerSample">The bit depth of each audio sample (e.g., 16 for 16-bit audio).</param>
    /// <returns>A memory stream containing the WAV-formatted audio data.</returns>
    private static MemoryStream CreateWavStream(float[] samples, int sampleRate, short channels, short bitsPerSample)
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        // Convert float samples to 16-bit PCM
        var pcmData = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            var sample16 = (short)(samples[i] * 32767f);
            var bytes = BitConverter.GetBytes(sample16);
            pcmData[i * 2] = bytes[0];
            pcmData[i * 2 + 1] = bytes[1];
        }

        var bytesPerSample = bitsPerSample / 8;
        var byteRate = sampleRate * channels * bytesPerSample;
        var blockAlign = (short)(channels * bytesPerSample);

        // WAV header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + pcmData.Length); // ChunkSize
        writer.Write("WAVE".ToCharArray());

        // fmt subchunk
        writer.Write("fmt ".ToCharArray());
        writer.Write(16); // Subchunk1Size (16 for PCM)
        writer.Write((short)1); // AudioFormat (1 for PCM)
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write("data".ToCharArray());
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the SttService class
    /// and optionally disposes of the managed resources.
    /// </summary>
    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _vad?.Dispose();
        GC.SuppressFinalize(this);
    }
}
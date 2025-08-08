using System.Runtime.InteropServices;
using SherpaOnnx;
using Whisper.net;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// Represents the configuration settings for a Speech-to-Text (STT) service, enabling customization of
/// model paths, language, sensitivity thresholds, and hardware acceleration options.
/// </summary>
public class SttConfig
{
    /// <summary>
    /// Gets the default configuration instance for the STT service, pre-initialized with default values.
    /// </summary>
    public static SttConfig Default => new SttConfig();

    /// <summary>
    /// File path to the Whisper model used for speech-to-text transcription.
    /// </summary>
    public string WhisperModelPath { get; set; } = "ggml-base.bin";

    /// <summary>
    /// Specifies the language code used by the Whisper transcription engine (e.g., "en" for English).
    /// This property defines the language for speech-to-text processing and impacts the model's transcription behavior.
    /// </summary>
    public string WhisperLanguage { get; set; } = "en";

    /// <summary>
    /// Temperature setting used in Whisper transcription, controlling the randomness of predictions.
    /// A higher value increases diversity, while a lower value makes output more focused.
    /// </summary>
    public float WhisperTemperature { get; set; } = 0.2f;

    /// <summary>
    /// File path to the Voice Activity Detection (VAD) model used for processing audio inputs.
    /// </summary>
    public string VadModelPath { get; set; } = "silero-vad.onnx";

    /// <summary>
    /// Threshold value for Voice Activity Detection (VAD), used to determine whether audio contains speech or silence.
    /// </summary>
    public float VadThreshold { get; set; } = 0.3f; // Slightly lower for better sensitivity per tuning tips

    /// <summary>
    /// The minimum duration of silence required to trigger a segment break in speech-to-text processing, measured in seconds.
    /// </summary>
    public float MinSilenceDuration { get; set; } = 0.6f;

    /// <summary>
    /// Specifies the minimum duration (in seconds) of detected speech
    /// required to process and consider it valid.
    /// </summary>
    public float MinSpeechDuration { get; set; } = 0.5f;

    /// <summary>
    /// Size of the VAD (Voice Activity Detection) analysis window, typically used to process audio segments.
    /// </summary>
    public int VadWindowSize { get; set; } = 512;

    /// <summary>
    /// Maximum duration, in seconds, allowed for speech input during transcription.
    /// </summary>
    public float MaxSpeechDuration { get; set; } = 15f; // Higher for longer utterances

    /// <summary>
    /// The sample rate (in Hertz) used for audio processing.
    /// Determines the number of audio samples captured per second.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// The number of threads to be used for processing.
    /// Defaults to the lesser of half the available processor count or 4.
    /// </summary>
    public int NumThreads { get; set; } = Math.Min(Environment.ProcessorCount / 2, 4);

    /// <summary>
    /// Indicates the debug level for the STT service configuration. Higher values may enable more detailed logging or diagnostic information.
    /// </summary>
    public int Debug { get; set; } = 0;

    /// <summary>
    /// Indicates whether CUDA is enabled for GPU acceleration.
    /// </summary>
    public bool CudaEnabled { get; set; } = false;
}

/// <summary>
/// Provides functionality for audio-to-text transcription using VAD (Voice Activity Detection) and Whisper models.
/// This service processes audio streams and converts them into text chunks asynchronously.
/// </summary>
public class SttService : ISttService, IDisposable
{
    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> used for logging operations within the <see cref="SttService"/> class.
    /// </summary>
    private readonly ILogger<SttService> _logger;

    private readonly WhisperFactory _whisperFactory;
    private readonly VadModelConfig _vadModelConfig;
    private readonly SttConfig _config;

    /// <summary>
    /// The SttService class provides functionality for speech-to-text (STT) processing using ONNX models.
    /// It integrates Voice Activity Detection (VAD) and Whisper model capabilities to process audio and
    /// transcribe speech into text.
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
    /// Asynchronously generates text chunks from a stream of audio data in byte array format.
    /// </summary>
    /// <param name="audioStream">
    /// A stream of audio data represented as an <see cref="IAsyncEnumerable{T}"/> of byte arrays. Each byte array represents a chunk of audio data.
    /// </param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of strings, where each string corresponds to a transcribed text chunk generated from the audio input.
    /// </returns>
    public async IAsyncEnumerable<string> GenerateTextChunksAsync(IAsyncEnumerable<byte[]> audioStream)
    {
        await using var whisperProcessor = _whisperFactory.CreateBuilder()
            .WithTemperature(_config.WhisperTemperature)
            .WithLanguage(_config.WhisperLanguage)
            .WithThreads(_config.NumThreads)
            .Build();

        using var vadProcessor = new VadProcessor(_logger, _vadModelConfig, whisperProcessor);

        await foreach (var audioChunk in audioStream)
        {
            await foreach (var text in vadProcessor.ProcessChunkAsync(audioChunk))
            {
                yield return text;
            }
        }

        // After stream ends, flush and yield any trailing segments
        await foreach (var text in vadProcessor.FlushAndYieldAsync())
        {
            yield return text;
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="SttService"/> class.
    /// </summary>
    public void Dispose()
    {
        _whisperFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Internal processor (handling byte[] input)
/// <summary>
/// Represents a processor responsible for handling byte-based audio input,
/// applying voice activity detection (VAD), and generating transcriptions
/// using a speech-to-text (STT) model.
/// </summary>
internal class VadProcessor : IDisposable
{
    /// <summary>
    /// Represents a logging instance used to capture and record log messages within the <see cref="VadProcessor"/> class.
    /// This is primarily utilized to log information such as transcriptions generated during voice activity detection (VAD).
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Configuration settings for the Voice Activity Detection (VAD) model used by the <see cref="VadProcessor"/>.
    /// Provides parameters required by the VAD to perform audio segmentation and speech activity detection.
    /// </summary>
    private readonly VadModelConfig _vadConfig;

    private readonly WhisperProcessor _whisperProcessor;
    private readonly VoiceActivityDetector _vad;
    private readonly List<float> _remainingSamples = [];

    /// <summary>
    /// Processes audio data to detect voice activity and performs speech-to-text conversion.
    /// </summary>
    public VadProcessor(ILogger logger, VadModelConfig vadConfig, WhisperProcessor whisperProcessor)
    {
        _logger = logger;
        _vadConfig = vadConfig;
        _whisperProcessor = whisperProcessor;
        _vad = new VoiceActivityDetector(_vadConfig, 60);
    }

    /// <summary>
    /// Asynchronously processes a chunk of audio data and detects speech segments,
    /// generating transcribed text for detected speech regions.
    /// </summary>
    /// <param name="audioChunk">The audio data to process, represented as a byte array.</param>
    /// <returns>An asynchronous stream of transcribed text for detected speech regions.</returns>
    public async IAsyncEnumerable<string> ProcessChunkAsync(byte[] audioChunk)
    {
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
                await foreach (var result in _whisperProcessor.ProcessAsync(wavStream))
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
    /// Flushes any remaining speech segments and yields their transcriptions.
    /// This should be called at the end of the audio stream.
    /// </summary>
    /// <returns>An asynchronous stream of transcribed text for remaining segments.</returns>
    public async IAsyncEnumerable<string> FlushAndYieldAsync()
    {
        _vad.Flush();
        while (!_vad.IsEmpty())
        {
            var segment = _vad.Front();
            using var wavStream = CreateWavStream(segment.Samples, _vadConfig.SampleRate, 1, 16);
            wavStream.Position = 0;

            var transcription = string.Empty;
            await foreach (var result in _whisperProcessor.ProcessAsync(wavStream))
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
    /// Releases the unmanaged resources used by the VadProcessor class
    /// and optionally disposes of the managed resources.
    /// </summary>
    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _vad?.Dispose();
        GC.SuppressFinalize(this);
    }
}
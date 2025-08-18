using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SherpaOnnx;
using Whisper.net;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// Encapsulates the configuration parameters for optimizing Speech-to-Text (STT) processing,
/// including model paths, language settings, detection thresholds, processing constraints,
/// and hardware acceleration options.
/// </summary>
public class SttConfig
{
    /// <summary>
    /// Gets a default instance of the <see cref="SttConfig"/> class initialized with pre-defined values
    /// for Speech-to-Text (STT) processing, offering a convenient starting point for configuration.
    /// </summary>
    public static SttConfig Default => new SttConfig();

    /// <summary>
    /// Specifies the file path of the Whisper model binary, used to configure the Speech-to-Text (STT) engine for processing audio data.
    /// </summary>
    public string WhisperModelPath { get; set; } = "ggml-base.bin";

    /// <summary>
    /// Specifies the language for the Whisper Speech-to-Text (STT) model,
    /// used to guide transcription processing in the chosen language.
    /// Default value is set to "en" (English).
    /// </summary>
    public string WhisperLanguage { get; set; } = "en";

    /// <summary>
    /// Specifies the temperature setting for Whisper-based processing, influencing the randomness
    /// of predictions and controlling the balance between creativity and determinism in Speech-to-Text (STT) outputs.
    /// Lower values make predictions more focused, while higher values increase variability in results.
    /// </summary>
    public float WhisperTemperature { get; set; } = 0.2f;

    /// <summary>
    /// Specifies the file path to the Voice Activity Detection (VAD) model,
    /// which is used for identifying segments of audio that contain active speech.
    /// </summary>
    public string VadModelPath { get; set; } = "silero-vad.onnx";

    /// <summary>
    /// Represents the Voice Activity Detection (VAD) threshold used to determine the minimum energy level
    /// required to classify audio as containing speech during the Speech-to-Text (STT) processing.
    /// </summary>
    public float VadThreshold { get; set; } = 0.3f; // Slightly lower for better sensitivity per tuning tips

    /// <summary>
    /// Defines the minimum duration of silence, in seconds, that the system recognizes
    /// when identifying pauses during speech analysis. It is used to optimize sensitivity
    /// in Speech-to-Text processing.
    /// </summary>
    public float MinSilenceDuration { get; set; } = 0.6f;

    /// <summary>
    /// Represents the minimum duration, in seconds, of a speech segment required for processing in the Speech-to-Text (STT) configuration.
    /// This property determines the threshold for identifying and analyzing speech input.
    /// </summary>
    public float MinSpeechDuration { get; set; } = 0.5f;

    /// <summary>
    /// Represents the size of the Voice Activity Detection (VAD) window in samples,
    /// defining the length of audio segments used for detecting speech activity.
    /// </summary>
    public int VadWindowSize { get; set; } = 512;

    /// <summary>
    /// Defines the maximum allowable duration, in seconds, for a single speech input
    /// to be processed within the Speech-to-Text (STT) configuration.
    /// </summary>
    public float MaxSpeechDuration { get; set; } = 15f; // Higher for longer utterances

    /// <summary>
    /// Specifies the sample rate, in Hz, used for audio processing.
    /// Higher values are typically suited for longer utterances and can improve audio quality during processing.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Determines the number of threads to be utilized for processing tasks, with a default value
    /// calculated based on the system's available processor count, optimized for performance.
    /// </summary>
    public int NumThreads { get; set; } = Math.Min(Environment.ProcessorCount / 2 + 1, 4);

    /// <summary>
    /// Represents a debug level setting for the <see cref="SttConfig"/> class,
    /// allowing for configuration of debugging verbosity or diagnostic output.
    /// </summary>
    public int Debug { get; set; } = 0;

    /// <summary>
    /// Indicates whether CUDA (Compute Unified Device Architecture) support is enabled for leveraging GPU acceleration
    /// in speech-to-text processing tasks. If set to <c>true</c>, CUDA-enabled hardware will be used for computation when available;
    /// otherwise, processing will be performed on the CPU.
    /// </summary>
    public bool CudaEnabled { get; set; } = false;
}

/// <summary>
/// Implements a speech-to-text service leveraging Whisper and VAD (Voice Activity Detection) models
/// for real-time audio transcription. The service is designed to process audio streams asynchronously,
/// providing transcribed text as output and supporting customizable configurations for optimized performance.
/// </summary>
public class SttService : ISttService, IDisposable
{
    /// <summary>
    /// An instance of <see cref="ILogger{TCategoryName}"/> for logging messages, errors, and other relevant information specific to the execution of the <see cref="SttService"/> class.
    /// </summary>
    private readonly ILogger<SttService> _logger;

    /// <summary>
    /// Represents an instance of the <see cref="WhisperFactory"/> class used for managing and facilitating speech-to-text processing operations within the service.
    /// </summary>
    private readonly WhisperFactory _whisperFactory;

    /// <summary>
    /// Represents the configuration settings for the Voice Activity Detection (VAD) model
    /// utilized by the Speech-to-Text (STT) service for processing audio input and detecting active voice segments.
    /// </summary>
    private readonly VadModelConfig _vadModelConfig;

    /// <summary>
    /// Holds the configuration settings specific to the Speech-to-Text (STT) service, enabling customization
    /// and management of service behavior and parameters.
    /// </summary>
    private readonly SttConfig _config;

    /// <summary>
    /// The SttService class handles the conversion of audio data into text using ONNX-based models.
    /// It supports configurable options and integrates logging functionalities to monitor and manage
    /// the execution and behavior of the speech-to-text operations.
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
    /// An asynchronous enumerable of byte arrays representing chunks of audio data to be processed.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that allows the operation to be cancelled before completion.
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
    /// This includes disposing of managed and unmanaged resources, ensuring a proper release of memory,
    /// and calling <see cref="GC.SuppressFinalize"/> to prevent the garbage collector from finalizing the object.
    /// </summary>
    public void Dispose()
    {
        _whisperFactory?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Internal processor (handling byte[] input)
/// <summary>
/// Handles audio processing by using Voice Activity Detection (VAD) to segment audio streams
/// and passing detected speech segments to a specified Speech-to-Text (STT) model for transcription.
/// </summary>
internal class VadProcessor : IDisposable
{
    /// <summary>
    /// Represents a logging instance used to record diagnostic messages, warnings, and
    /// debug information within the <see cref="VadProcessor"/> class, especially during
    /// the handling of voice activity detection (VAD) and speech-to-text transcription processes.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Represents the configuration settings for the Voice Activity Detection (VAD) model
    /// within the <see cref="VadProcessor"/>. These settings dictate parameters such as
    /// audio processing window size and sample rate, which are critical for detecting
    /// and segmenting speech in audio input.
    /// </summary>
    private readonly VadModelConfig _vadConfig;

    /// <summary>
    /// Serves as an internal processor leveraging the Whisper.NET library for speech recognition,
    /// enabling transcription of audio streams into textual data during speech-to-text processing.
    /// </summary>
    private readonly WhisperProcessor _whisperProcessor;

    /// <summary>
    /// Represents the core voice activity detection (VAD) component that processes audio input
    /// to detect active speech segments, enabling efficient handling of speech data for
    /// further processing like transcription or analysis.
    /// </summary>
    private readonly VoiceActivityDetector _vad;

    /// <summary>
    /// Stores the unprocessed audio samples that are left over after processing a segment,
    /// enabling continuity in subsequent iterations of voice activity detection (VAD) and transcription.
    /// </summary>
    private readonly List<float> _remainingSamples = [];

    /// <summary>
    /// The VadProcessor class facilitates audio data processing by employing voice activity detection (VAD)
    /// to isolate speech segments and passing them to a speech-to-text (STT) processor for transcription.
    /// It is designed to work with asynchronous audio streams and supports efficient resource management.
    /// </summary>
    public VadProcessor(ILogger logger, VadModelConfig vadConfig, WhisperProcessor whisperProcessor)
    {
        _logger = logger;
        _vadConfig = vadConfig;
        _whisperProcessor = whisperProcessor;
        _vad = new VoiceActivityDetector(_vadConfig, 60);
    }

    /// <summary>
    /// Asynchronously processes a chunk of audio data, applies Voice Activity Detection (VAD) to identify speech,
    /// and streams the resulting transcribed text for detected speech regions.
    /// </summary>
    /// <param name="audioChunk">The audio data to process, provided as a byte array in 16-bit PCM format.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests and terminate the operation if triggered.</param>
    /// <returns>An asynchronous stream of transcribed string outputs corresponding to detected speech segments in the audio data.</returns>
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
    /// This method ensures that trailing audio input, after the audio stream has ended, is processed
    /// and converted into text.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to observe while flushing and yielding transcriptions, allowing the operation to be
    /// interrupted before completion.
    /// </param>
    /// <returns>
    /// An asynchronous stream containing the transcribed text for any remaining speech segments
    /// detected by the VAD.
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
    /// Generates a WAV-formatted audio stream from the provided PCM samples and specified audio configuration.
    /// </summary>
    /// <param name="samples">The array of PCM samples, with each value normalized to the range -1.0 to 1.0.</param>
    /// <param name="sampleRate">The number of audio samples per second, expressed in Hz.</param>
    /// <param name="channels">The number of audio channels, indicating mono (1) or stereo (2).</param>
    /// <param name="bitsPerSample">The bit depth of each individual audio sample, such as 16 bits per sample.</param>
    /// <returns>A <see cref="MemoryStream"/> containing the audio data formatted as a WAV file.</returns>
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
    /// Releases the resources used by the SttService class and its associated components.
    /// Ensures proper cleanup of both managed and unmanaged resources, such as
    /// the WhisperProcessor and VoiceActivityDetector instances.
    /// </summary>
    public void Dispose()
    {
        _whisperProcessor?.Dispose();
        _vad?.Dispose();
        GC.SuppressFinalize(this);
    }
}
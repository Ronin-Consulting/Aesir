using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SherpaOnnx;

namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Represents the configuration settings for the TTS service, including model path,
/// hardware acceleration, threading, and performance tuning parameters.
/// </summary>
public class TtsConfig
{
    /// <summary>
    /// Provides a default configuration instance for TTS settings, enabling out-of-the-box functionality without requiring manual configuration.
    /// </summary>
    public static TtsConfig Default => new();

    /// <summary>
    /// Specifies the file path to the TTS model that will be used for text-to-speech processing.
    /// This must be a valid path to the model file.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether CUDA acceleration is enabled for TTS service processing.
    /// </summary>
    public bool CudaEnabled { get; set; } = false;

    /// <summary>
    /// Specifies the number of threads used for TTS processing.
    /// This influences performance and multi-threading optimization.
    /// </summary>
    public int NumThreads { get; set; } = Math.Min(Environment.ProcessorCount / 2 + 1, 4);

    /// <summary>
    /// Defines the rate of speech synthesis playback.
    /// A value of 1.0 represents normal speed, while values greater than 1.0 increase the speed, and values less than 1.0 decrease it.
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// The identifier of the speaker used for text-to-speech (TTS) audio generation.
    /// Allows for selecting a specific speaker voice profile from the model.
    /// </summary>
    public int SpeakerId { get; set; } = 0;

    /// <summary>
    /// Enable or disable debug mode for TTS processing, controlling the level of logging or diagnostic output.
    /// </summary>
    public int Debug { get; set; } = 1;

    /// <summary>
    /// Specifies the maximum number of sentences to include in a single chunk for TTS processing.
    /// </summary>
    public int MaxSentencesPerChunk { get; set; } = 25;
}

/// <summary>
/// Implements the text-to-speech (TTS) functionality, allowing for conversion of text input
/// into audio output. The TtsService supports generating audio in chunks and can be configured
/// for offline use with specific models and settings, including hardware acceleration options.
/// </summary>
public partial class TtsService : ITtsService
{
    /// <summary>
    /// Provides logging functionality for the TtsService class.
    /// </summary>
    /// <remarks>
    /// This variable is an instance of <see cref="ILogger{TtsService}"/> and is used
    /// to log diagnostic information, including informational, warning, and error messages
    /// during the lifecycle and operation of the text-to-speech service.
    /// </remarks>
    private readonly ILogger<TtsService> _logger;

    /// <summary>
    /// The text-to-speech engine used for converting text into synthesized audio output.
    /// </summary>
    /// <remarks>
    /// This field is instantiated as an <see cref="OfflineTts"/> object configured with specific model
    /// parameters, including paths for the speech synthesis model and token files, as well as runtime
    /// configurations such as CPU/GPU support and thread allocation. It forms the core component used
    /// in the text-to-speech processing pipeline.
    /// </remarks>
    private readonly OfflineTts _ttsEngine;

    /// <summary>
    /// Stores configuration settings used by the <see cref="TtsService"/> class for text-to-speech
    /// processing, including model paths, performance tuning options, and hardware acceleration settings.
    /// </summary>
    private readonly TtsConfig _config;

    /// <summary>
    /// The TtsService class is responsible for handling text-to-speech (TTS) operations using ONNX models.
    /// It utilizes provided configuration settings for model path validation, token file validation,
    /// and hardware acceleration to initialize a TTS processing engine.
    /// </summary>
    public TtsService(
        ILogger<TtsService> logger,
        TtsConfig? config = null)
    {
        _config = config ?? new TtsConfig();
        _logger = logger;

        if (string.IsNullOrEmpty(_config.ModelPath))
        {
            throw new ArgumentNullException(nameof(_config.ModelPath), "Model path cannot be null or empty");
        }

        if (!File.Exists(_config.ModelPath))
        {
            throw new FileNotFoundException($"Model file not found at path: {_config.ModelPath}");
        }

        var tokensPath = Path.Combine(
            Path.GetDirectoryName(_config.ModelPath) ?? throw new InvalidOperationException(), "tokens.txt");
        var dataDirPath = Path.GetDirectoryName(_config.ModelPath);

        if (!File.Exists(tokensPath))
        {
            throw new FileNotFoundException($"Tokens file not found at path: {tokensPath}");
        }

        if (!Directory.Exists(dataDirPath))
        {
            throw new FileNotFoundException($"Data directory not found at path: {dataDirPath}");
        }

        _logger.LogInformation("Initializing TTS engine with model: {ModelPath}", _config.ModelPath);

        var ttsEngineConfig = new OfflineTtsConfig
        {
            Model = new OfflineTtsModelConfig
            {
                Vits = new OfflineTtsVitsModelConfig
                {
                    Model = _config.ModelPath,
                    Tokens = tokensPath,
                    DataDir = Path.Combine(
                        Path.GetDirectoryName(_config.ModelPath) ?? throw new InvalidOperationException(), "espeak-ng-data")
                },
                Debug = _config.Debug,
                NumThreads = _config.NumThreads,
                Provider = _config.CudaEnabled ? "cuda" : "cpu"
            }
        };
        _ttsEngine = new OfflineTts(ttsEngineConfig);
    }

    /// <summary>
    /// Asynchronously generates audio chunks in WAV format for a given text input, splitting the text into sentences and processing each sentence using text-to-speech synthesis.
    /// </summary>
    /// <param name="text">The input text to be converted into audio chunks. The text is split into sentences before processing.</param>
    /// <param name="speed">An optional parameter representing the speed adjustment for the synthesized speech. Defaults to 1.0f if not provided.</param>
    /// <returns>An asynchronous stream of byte arrays, where each byte array represents a WAV audio chunk corresponding to a processed sentence.</returns>
    public async IAsyncEnumerable<byte[]> GenerateAudioChunksAsync(string text, float? speed = 1.0f)
    {
        // Use regex to split the text while keeping the delimiters.
        var sentences = SentenceSplitterRegex().Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        var sentenceChunks = sentences.Select((s, i) => new { Sentence = s, Index = i })
            .GroupBy(x => x.Index / _config.MaxSentencesPerChunk)
            .Select(g => string.Join(" ", g.Select(x => x.Sentence)));

        foreach (var sentenceGroup in sentenceChunks)
        {
            // 'sentenceGroup' now includes a group of up to 3 sentences.
            var audio = _ttsEngine.Generate(sentenceGroup, speed: speed ?? _config.Speed, speakerId: _config.SpeakerId);

            // Use a MemoryStream to create the WAV file in memory
            await using var memoryStream = new MemoryStream();
            await using (var writer = new BinaryWriter(memoryStream, System.Text.Encoding.UTF8, true))
            {
                var samples = audio.Samples;
                var sampleRate = audio.SampleRate;
                const int numChannels = 1;
                const int bitsPerSample = 16;

                // Write WAV header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * numChannels * bitsPerSample / 8);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16); // Sub-chunk size
                writer.Write((short)1); // PCM
                writer.Write((short)numChannels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * numChannels * bitsPerSample / 8); // Byte rate
                writer.Write((short)(numChannels * bitsPerSample / 8)); // Block align
                writer.Write((short)bitsPerSample);
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * numChannels * bitsPerSample / 8);

                // Write audio samples
                foreach (var sample in samples)
                {
                    // Convert float sample to 16-bit PCM
                    writer.Write((short)(sample * 32767.0f));
                }
            }
            yield return memoryStream.ToArray();
        }
    }

    /// <summary>
    /// Represents a regular expression used to split text into sentences based on
    /// sentence-ending punctuation marks such as '.', '!', or '?'. The delimiters
    /// are retained to preserve sentence boundaries.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance configured to split text at sentence boundaries
    /// while keeping the sentence-ending punctuation.
    /// </returns>
    [GeneratedRegex(@"(?<=[.!?])")]
    private static partial Regex SentenceSplitterRegex();
}

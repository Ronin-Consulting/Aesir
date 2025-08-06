using System.Text.RegularExpressions;
using SherpaOnnx;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// The TtsService class implements text-to-speech (TTS) functionality, enabling the conversion
/// of text input into audio output. It supports chunked audio generation and is designed to
/// operate with an offline TTS engine using configurable settings such as model path and hardware acceleration.
/// </summary>
public partial class TtsService : ITtsService
{
    /// <summary>
    /// Provides logging capabilities for the <see cref="TtsService"/> class.
    /// </summary>
    /// <remarks>
    /// This variable is an instance of <see cref="ILogger{TtsService}"/> and is used to log
    /// informational messages, warnings, errors, and other diagnostic information related
    /// to the operation of the text-to-speech service.
    /// </remarks>
    private readonly ILogger<TtsService> _logger;

    /// <summary>
    /// Holds the instance of the text-to-speech engine utilized for converting text inputs into synthesized audio data.
    /// </summary>
    /// <remarks>
    /// This variable is initialized as an instance of <see cref="OfflineTts"/> and configured based on specified model
    /// and environment options. It serves as the core processing unit for generating audio from textual input, managing
    /// tasks such as applying language models and synthesizing speech.
    /// </remarks>
    private readonly OfflineTts _ttsEngine;

    /// Represents a Text-to-Speech (TTS) service.
    /// Provides functionality for converting text input into audio data in chunks using an offline TTS engine.
    public TtsService(ILogger<TtsService> logger, string? modelPath, bool useCuda)
    {
        _logger = logger;

        if (string.IsNullOrEmpty(modelPath))
        {
            throw new ArgumentNullException(nameof(modelPath), "Model path cannot be null or empty");
        }

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model file not found at path: {modelPath}");
        }
        
        var tokensPath = Path.Combine(
            Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(), "tokens.txt");
        var dataDirPath = Path.GetDirectoryName(modelPath);
        
        if (!File.Exists(tokensPath))
        {
            throw new FileNotFoundException($"Tokens file not found at path: {tokensPath}");
        }

        if (!Directory.Exists(dataDirPath))
        {
            throw new FileNotFoundException($"Data directory not found at path: {dataDirPath}");
        }
        
        _logger.LogInformation("Initializing TTS engine with model: {ModelPath}", modelPath);

        // C API Setup
        // config.model.vits.model = "vits-piper-en_US-joe-medium/en_US-joe-medium.onnx";
        // config.model.vits.tokens = "vits-piper-en_US-joe-medium/tokens.txt";
        // config.model.vits.data_dir = "vits-piper-en_US-joe-medium/espeak-ng-data";
        // config.model.num_threads = 1;
        // const SherpaOnnxOfflineTts *tts = SherpaOnnxCreateOfflineTts(&config);
        //
        // int sid = 0; // speaker id
        var config = new OfflineTtsConfig
        {
            Model = new OfflineTtsModelConfig
            {
                Vits = new OfflineTtsVitsModelConfig
                {
                    Model = modelPath,
                    Tokens = Path.Combine(
                        Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(), "tokens.txt"),
                    DataDir = Path.Combine(
                        Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(), "espeak-ng-data")
                },
                Debug = 1,
                NumThreads = 4,
                Provider = useCuda ? "cuda" : "cpu"
            }
        };
        _ttsEngine = new OfflineTts(config);
    }

    /// <summary>
    /// Asynchronously generates audio chunks in WAV format for a given text input, splitting the text into individual sentences
    /// and applying text-to-speech synthesis to each sentence.
    /// </summary>
    /// <param name="text">The text input to be processed and converted into audio chunks. Sentences are processed individually.</param>
    /// <param name="speed">A value representing the speed factor for the synthesized speech. The default value is 1.0f.</param>
    /// <returns>An asynchronous stream of byte arrays, each representing a WAV audio chunk corresponding to a processed sentence.</returns>
    public async IAsyncEnumerable<byte[]> GenerateAudioChunksAsync(string text, float speed = 1.0f)
    {
        // Use regex to split the text while keeping the delimiters.
        var sentences = SentenceSplitterRegex().Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        var sentenceChunks = sentences.Select((s, i) => new { Sentence = s, Index = i })
            .GroupBy(x => x.Index / 25)
            .Select(g => string.Join(" ", g.Select(x => x.Sentence)));

        foreach (var sentenceGroup in sentenceChunks)
        {
            // 'sentenceGroup' now includes a group of up to 3 sentences.
            var audio = _ttsEngine.Generate(sentenceGroup, speed: speed, speakerId: 0);

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
    
    /// Represents a regular expression used for splitting text into sentences
    /// based on sentence-ending punctuation marks like '.', '!', or '?' while retaining the delimiters.
    [GeneratedRegex(@"(?<=[.!?])")]
    private static partial Regex SentenceSplitterRegex();
}
using System.Text.RegularExpressions;
using SherpaOnnx;

namespace Aesir.Api.Server.Services.Implementations.Onnx;

/// <summary>
/// The TtsService class provides text-to-speech (TTS) functionality by converting
/// input text into corresponding audio data in chunks. It utilizes an offline TTS
/// engine for generating audio based on the provided model and configuration.
/// </summary>
public partial class TtsService : ITtsService
{
    /// <summary>
    /// Represents the internal text-to-speech engine used for generating audio data from text inputs.
    /// </summary>
    /// <remarks>
    /// This variable is an instance of <see cref="OfflineTts"/> and is configured during the initialization
    /// of the <see cref="TtsService"/> class. It is responsible for performing the core text-to-speech functionality,
    /// including processing input text and generating corresponding audio samples.
    /// </remarks>
    private readonly OfflineTts _ttsEngine;

    /// Represents a Text-to-Speech (TTS) service implementation.
    /// Provides functionality for generating audio data from text input asynchronously.
    public TtsService(string modelPath, bool useCuda)
    {
        var config = new OfflineTtsConfig
        {
            Model = new OfflineTtsModelConfig
            {
                Vits = new OfflineTtsVitsModelConfig
                {
                    Model = modelPath,  // "en_US-joe-medium.onnx"
                    Tokens = Path.Combine(
                        Path.GetDirectoryName(modelPath) ?? throw new InvalidOperationException(), "tokens.txt"),
                    DataDir = Path.GetDirectoryName(modelPath)
                },
                NumThreads = 4,
                Provider = useCuda ? "cuda" : "cpu"
            }
        };
        _ttsEngine = new OfflineTts(config);
    }

    /// <summary>
    /// Asynchronously generates audio chunks in WAV format for the given text, splitting it into sentences and applying text-to-speech synthesis for each sentence.
    /// </summary>
    /// <param name="text">The input text to be converted to audio. It will be split into sentences for processing.</param>
    /// <param name="speed">The speed factor for the generated speech. Default is 1.0f.</param>
    /// <returns>An asynchronous stream of byte arrays representing WAV audio data for each processed sentence.</returns>
    public async IAsyncEnumerable<byte[]> GenerateAudioChunksAsync(string text, float speed = 1.0f)
    {
        // Use regex to split the text while keeping the delimiters.
        var sentences = SentenceSplitterRegex().Split(text)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));
    
        foreach (var sentence in sentences)
        {
            // 'sentence' now includes its original punctuation.
            var audio = _ttsEngine.Generate(sentence, speed: speed, speakerId: 0);

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
    /// A method that generates and returns a regular expression used to split sentences
    /// at sentence-ending punctuation marks such as '.', '!', or '?' while retaining the delimiters.
    /// This regex is primarily used to segment text input into smaller chunks.
    /// </summary>
    /// <returns>
    /// A <see cref="Regex"/> instance configured to identify sentence-ending punctuation
    /// and split the input text accordingly.
    /// </returns>
    [GeneratedRegex(@"(?<=[.!?])")]
    private static partial Regex SentenceSplitterRegex();
}
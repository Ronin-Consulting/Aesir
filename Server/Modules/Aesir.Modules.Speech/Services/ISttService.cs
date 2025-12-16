using Aesir.Modules.Speech.Services.Audio;

namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Interface for Speech-to-Text (STT) services that handle audio input streams
/// and convert them into textual content asynchronously.
/// </summary>
public interface ISttService
{
    /// <summary>
    /// Gets the expected input sample rate for the STT engine.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Asynchronously generates transcribed text chunks from a stream of raw PCM audio data.
    /// </summary>
    /// <param name="audioStream">
    /// An asynchronous enumerable of byte arrays representing chunks of 16-bit PCM audio data.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional cancellation token to observe while processing the audio stream.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of transcribed text chunks as strings.
    /// </returns>
    IAsyncEnumerable<string> GenerateTextChunksAsync(
        IAsyncEnumerable<byte[]> audioStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously generates transcribed text chunks from a stream of Opus-encoded audio data.
    /// Opus audio is decoded to PCM before being processed by the STT engine.
    /// </summary>
    /// <param name="opusStream">
    /// An asynchronous enumerable of Opus-encoded audio frames.
    /// </param>
    /// <param name="opusConfig">
    /// Optional Opus codec configuration. If null, uses defaults (16kHz, mono, VoIP).
    /// </param>
    /// <param name="cancellationToken">
    /// An optional cancellation token to observe while processing the audio stream.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of transcribed text chunks as strings.
    /// </returns>
    IAsyncEnumerable<string> GenerateTextChunksFromOpusAsync(
        IAsyncEnumerable<byte[]> opusStream,
        OpusCodecConfig? opusConfig = null,
        CancellationToken cancellationToken = default);
}

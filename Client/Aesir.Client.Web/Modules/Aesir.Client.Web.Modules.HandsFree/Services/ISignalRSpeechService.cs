namespace Aesir.Client.Web.Modules.HandsFree.Services;

/// <summary>
/// Interface for SignalR-based speech services (STT and TTS hub connections).
/// </summary>
public interface ISignalRSpeechService : IAsyncDisposable
{
    /// <summary>
    /// Indicates whether the service is connected to the hubs.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Connect to the STT and TTS SignalR hubs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful.</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the SignalR hubs.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Stream audio chunks to the STT hub and receive transcribed text.
    /// </summary>
    /// <param name="audioChunks">Async stream of audio data chunks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of transcribed text chunks.</returns>
    IAsyncEnumerable<string> StreamSpeechToTextAsync(
        IAsyncEnumerable<byte[]> audioChunks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request TTS audio generation and receive audio chunks.
    /// </summary>
    /// <param name="text">The text to convert to speech.</param>
    /// <param name="speed">Speech speed (1.0 = normal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of audio data chunks (WAV format).</returns>
    IAsyncEnumerable<byte[]> StreamTextToSpeechAsync(
        string text,
        float speed = 1.0f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Request TTS audio generation with Opus encoding (smaller bandwidth).
    /// </summary>
    /// <param name="text">The text to convert to speech.</param>
    /// <param name="speed">Speech speed (1.0 = normal).</param>
    /// <param name="bitrate">Opus bitrate in bps (default 32000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async stream of Opus-encoded audio frames.</returns>
    IAsyncEnumerable<byte[]> StreamTextToSpeechOpusAsync(
        string text,
        float speed = 1.0f,
        int bitrate = 32000,
        CancellationToken cancellationToken = default);
}

namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Interface for Speech-to-Text (STT) services that handle audio input streams
/// and convert them into textual content asynchronously.
/// </summary>
public interface ISttService
{
    /// <summary>
    /// Asynchronously generates transcribed text chunks from a stream of audio data in byte array format.
    /// </summary>
    /// <param name="audioStream">
    /// An asynchronous enumerable of byte arrays representing chunks of audio data.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional cancellation token to observe while processing the audio stream. This allows cancellation of the operation.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of transcribed text chunks as strings.
    /// </returns>
    IAsyncEnumerable<string> GenerateTextChunksAsync(IAsyncEnumerable<byte[]> audioStream, CancellationToken cancellationToken = default);
}

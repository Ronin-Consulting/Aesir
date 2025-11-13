namespace Aesir.Modules.Speech.Services;

/// <summary>
/// Defines a contract for a Text-to-Speech (TTS) service that generates audio
/// data in chunks from the given text input. The service provides an asynchronous
/// method to produce byte arrays representing portions of audio output.
/// </summary>
public interface ITtsService
{
    /// <summary>
    /// Asynchronously generates audio chunks in WAV format from the provided text.
    /// The text is split into sentences, and text-to-speech synthesis is applied for each sentence individually.
    /// </summary>
    /// <param name="text">The input text to be converted to audio. It is automatically split into sentences for processing.</param>
    /// <param name="speed">The speed factor for speech synthesis. The default value is 1.0f, representing normal speed.</param>
    /// <returns>An asynchronous stream of byte arrays, where each byte array represents a WAV audio chunk for a processed sentence.</returns>
    IAsyncEnumerable<byte[]> GenerateAudioChunksAsync(string text, float? speed = 1.0f);
}

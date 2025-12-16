namespace Aesir.Modules.Speech.Services.Audio;

/// <summary>
/// Interface for Opus audio encoding and decoding operations.
/// </summary>
public interface IOpusCodec : IDisposable
{
    /// <summary>
    /// Gets the frame size in samples based on configuration.
    /// </summary>
    int FrameSizeSamples { get; }

    /// <summary>
    /// Gets the current encoder bitrate.
    /// </summary>
    int Bitrate { get; }

    /// <summary>
    /// Gets the sample rate.
    /// </summary>
    int SampleRate { get; }

    /// <summary>
    /// Gets the number of channels.
    /// </summary>
    int Channels { get; }

    /// <summary>
    /// Encodes PCM audio samples to an Opus frame.
    /// </summary>
    /// <param name="pcmSamples">16-bit PCM samples (length must equal FrameSizeSamples * Channels).</param>
    /// <returns>Opus-encoded frame.</returns>
    byte[] Encode(short[] pcmSamples);

    /// <summary>
    /// Encodes PCM audio from a byte array (16-bit little-endian).
    /// </summary>
    /// <param name="pcmBytes">PCM byte array (length must equal FrameSizeSamples * Channels * 2).</param>
    /// <returns>Opus-encoded frame.</returns>
    byte[] Encode(byte[] pcmBytes);

    /// <summary>
    /// Decodes an Opus frame to PCM samples.
    /// </summary>
    /// <param name="opusData">Opus-encoded frame.</param>
    /// <returns>16-bit PCM samples.</returns>
    short[] Decode(byte[] opusData);

    /// <summary>
    /// Decodes an Opus frame to a PCM byte array.
    /// </summary>
    /// <param name="opusData">Opus-encoded frame.</param>
    /// <returns>PCM bytes (16-bit little-endian).</returns>
    byte[] DecodeToBytes(byte[] opusData);
}

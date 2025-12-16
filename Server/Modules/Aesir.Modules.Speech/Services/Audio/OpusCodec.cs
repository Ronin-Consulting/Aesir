using Concentus.Enums;
using Concentus.Structs;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Speech.Services.Audio;

/// <summary>
/// Opus codec implementation using the Concentus library (pure C# Opus implementation).
/// Thread-safe for encoding and decoding operations.
/// </summary>
public class OpusCodec : IOpusCodec
{
    private readonly ILogger<OpusCodec> _logger;
    private readonly OpusCodecConfig _config;
    private readonly OpusEncoder _encoder;
    private readonly OpusDecoder _decoder;
    private readonly byte[] _encodedBuffer;
    private readonly short[] _decodedBuffer;
    private readonly object _encodeLock = new();
    private readonly object _decodeLock = new();
    private bool _disposed;

    /// <inheritdoc />
    public int FrameSizeSamples { get; }

    /// <inheritdoc />
    public int Bitrate => _config.Bitrate;

    /// <inheritdoc />
    public int SampleRate => _config.SampleRate;

    /// <inheritdoc />
    public int Channels => _config.Channels;

    /// <summary>
    /// Creates a new Opus codec with the specified configuration.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="config">Codec configuration. If null, uses default speech-optimized settings.</param>
    public OpusCodec(ILogger<OpusCodec> logger, OpusCodecConfig? config = null)
    {
        _logger = logger;
        _config = config ?? OpusCodecConfig.Default;

        FrameSizeSamples = _config.FrameSizeSamples;

        // Max Opus frame is typically around 1275 bytes, but use larger buffer for safety
        _encodedBuffer = new byte[4000];
        // Buffer for decoded samples
        _decodedBuffer = new short[FrameSizeSamples * _config.Channels];

        // Map our application type to Concentus enum
        var opusApplication = _config.Application switch
        {
            OpusApplicationType.VoIP => OpusApplication.OPUS_APPLICATION_VOIP,
            OpusApplicationType.Audio => OpusApplication.OPUS_APPLICATION_AUDIO,
            OpusApplicationType.LowDelay => OpusApplication.OPUS_APPLICATION_RESTRICTED_LOWDELAY,
            _ => OpusApplication.OPUS_APPLICATION_VOIP
        };

        // Create encoder
        _encoder = new OpusEncoder(
            _config.SampleRate,
            _config.Channels,
            opusApplication);

        _encoder.Bitrate = _config.Bitrate;
        _encoder.Complexity = _config.Complexity;
        _encoder.UseInbandFEC = _config.EnableFec;
        _encoder.UseDTX = _config.EnableDtx;

        // Create decoder
        _decoder = new OpusDecoder(_config.SampleRate, _config.Channels);

        _logger.LogInformation(
            "Opus codec initialized: {SampleRate}Hz, {Channels}ch, {Bitrate}bps, {FrameMs}ms frames ({FrameSamples} samples)",
            _config.SampleRate, _config.Channels, _config.Bitrate, _config.FrameSizeMs, FrameSizeSamples);
    }

    /// <inheritdoc />
    public byte[] Encode(short[] pcmSamples)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var expectedSamples = FrameSizeSamples * _config.Channels;
        if (pcmSamples.Length != expectedSamples)
        {
            _logger.LogWarning(
                "PCM sample count mismatch. Expected {Expected}, got {Actual}. Padding/truncating.",
                expectedSamples, pcmSamples.Length);
        }

        lock (_encodeLock)
        {
            try
            {
                // Create a correctly sized buffer if input doesn't match
                short[] inputSamples;
                if (pcmSamples.Length == expectedSamples)
                {
                    inputSamples = pcmSamples;
                }
                else
                {
                    inputSamples = new short[expectedSamples];
                    var copyLength = Math.Min(pcmSamples.Length, expectedSamples);
                    Array.Copy(pcmSamples, inputSamples, copyLength);
                }

                int encodedLength = _encoder.Encode(
                    inputSamples, 0, FrameSizeSamples,
                    _encodedBuffer, 0, _encodedBuffer.Length);

                if (encodedLength < 0)
                {
                    throw new OpusEncodingException($"Opus encoding failed with error code: {encodedLength}");
                }

                var result = new byte[encodedLength];
                Array.Copy(_encodedBuffer, result, encodedLength);

                _logger.LogDebug(
                    "Encoded {InputSamples} samples ({InputBytes} bytes) to {OutputBytes} bytes Opus",
                    inputSamples.Length, inputSamples.Length * 2, encodedLength);

                return result;
            }
            catch (Exception ex) when (ex is not OpusEncodingException)
            {
                _logger.LogError(ex, "Unexpected error during Opus encoding");
                throw new OpusEncodingException("Failed to encode audio to Opus format", ex);
            }
        }
    }

    /// <inheritdoc />
    public byte[] Encode(byte[] pcmBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Convert bytes to shorts (16-bit little-endian PCM)
        var samples = new short[pcmBytes.Length / 2];
        Buffer.BlockCopy(pcmBytes, 0, samples, 0, pcmBytes.Length);
        return Encode(samples);
    }

    /// <inheritdoc />
    public short[] Decode(byte[] opusData)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (opusData == null || opusData.Length == 0)
        {
            _logger.LogWarning("Empty Opus frame received, returning silence");
            return new short[FrameSizeSamples * _config.Channels];
        }

        lock (_decodeLock)
        {
            try
            {
                int decodedSamples = _decoder.Decode(
                    opusData, 0, opusData.Length,
                    _decodedBuffer, 0, FrameSizeSamples, false);

                if (decodedSamples < 0)
                {
                    throw new OpusDecodingException(
                        $"Opus decoding failed with error code: {decodedSamples}",
                        decodedSamples);
                }

                var result = new short[decodedSamples * _config.Channels];
                Array.Copy(_decodedBuffer, result, result.Length);

                _logger.LogDebug(
                    "Decoded {InputBytes} bytes Opus to {OutputSamples} samples ({OutputBytes} bytes)",
                    opusData.Length, result.Length, result.Length * 2);

                return result;
            }
            catch (Exception ex) when (ex is not OpusDecodingException)
            {
                _logger.LogError(ex, "Unexpected error during Opus decoding, returning silence");
                // Return silence frame on error to maintain stream continuity
                return new short[FrameSizeSamples * _config.Channels];
            }
        }
    }

    /// <inheritdoc />
    public byte[] DecodeToBytes(byte[] opusData)
    {
        var samples = Decode(opusData);
        var bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Disposes the codec and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        _logger.LogDebug("Opus codec disposed");
    }
}

/// <summary>
/// Exception thrown when Opus encoding fails.
/// </summary>
public class OpusEncodingException : Exception
{
    public OpusEncodingException(string message) : base(message) { }
    public OpusEncodingException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when Opus decoding fails.
/// </summary>
public class OpusDecodingException : Exception
{
    /// <summary>
    /// The Opus error code, if available.
    /// </summary>
    public int ErrorCode { get; }

    public OpusDecodingException(string message, int errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }
}

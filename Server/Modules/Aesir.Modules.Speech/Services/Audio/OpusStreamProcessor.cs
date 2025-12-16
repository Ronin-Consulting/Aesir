using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Aesir.Modules.Speech.Services.Audio;

/// <summary>
/// Handles streaming Opus encoding/decoding for chunked audio data.
/// Buffers partial frames and handles frame alignment automatically.
/// </summary>
public class OpusStreamProcessor : IDisposable
{
    private readonly ILogger _logger;
    private readonly IOpusCodec _codec;
    private readonly List<short> _sampleBuffer = new();
    private readonly object _bufferLock = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of samples currently buffered.
    /// </summary>
    public int BufferedSamples
    {
        get
        {
            lock (_bufferLock)
            {
                return _sampleBuffer.Count;
            }
        }
    }

    /// <summary>
    /// Creates a new Opus stream processor.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="codec">The Opus codec to use for encoding/decoding.</param>
    public OpusStreamProcessor(ILogger logger, IOpusCodec codec)
    {
        _logger = logger;
        _codec = codec;
    }

    /// <summary>
    /// Encodes streaming PCM data to Opus frames.
    /// Buffers samples until a complete frame is available.
    /// </summary>
    /// <param name="pcmChunks">Stream of PCM byte arrays (16-bit little-endian).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of Opus-encoded frames.</returns>
    public async IAsyncEnumerable<byte[]> EncodeStreamAsync(
        IAsyncEnumerable<byte[]> pcmChunks,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frameSizeSamples = _codec.FrameSizeSamples * _codec.Channels;

        await foreach (var chunk in pcmChunks.WithCancellation(cancellationToken))
        {
            if (chunk.Length == 0) continue;

            // Convert bytes to samples (16-bit little-endian)
            var samples = new short[chunk.Length / 2];
            Buffer.BlockCopy(chunk, 0, samples, 0, chunk.Length);

            lock (_bufferLock)
            {
                _sampleBuffer.AddRange(samples);

                // Yield complete frames
                while (_sampleBuffer.Count >= frameSizeSamples)
                {
                    var frameSamples = new short[frameSizeSamples];
                    _sampleBuffer.CopyTo(0, frameSamples, 0, frameSizeSamples);
                    _sampleBuffer.RemoveRange(0, frameSizeSamples);

                    byte[] opusFrame;
                    try
                    {
                        opusFrame = _codec.Encode(frameSamples);
                    }
                    catch (OpusEncodingException ex)
                    {
                        _logger.LogWarning(ex, "Failed to encode frame, skipping");
                        continue;
                    }

                    yield return opusFrame;
                }
            }
        }

        // Flush remaining samples (pad with silence if needed)
        byte[]? finalFrame = null;
        lock (_bufferLock)
        {
            if (_sampleBuffer.Count > 0)
            {
                _logger.LogDebug("Flushing {Count} remaining samples with silence padding", _sampleBuffer.Count);

                var remaining = _sampleBuffer.ToArray();
                var padded = new short[frameSizeSamples];
                Array.Copy(remaining, padded, Math.Min(remaining.Length, padded.Length));

                try
                {
                    finalFrame = _codec.Encode(padded);
                }
                catch (OpusEncodingException ex)
                {
                    _logger.LogWarning(ex, "Failed to encode final frame");
                }

                _sampleBuffer.Clear();
            }
        }

        if (finalFrame != null)
        {
            yield return finalFrame;
        }
    }

    /// <summary>
    /// Decodes streaming Opus frames to PCM data.
    /// </summary>
    /// <param name="opusFrames">Stream of Opus-encoded frames.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stream of PCM byte arrays (16-bit little-endian).</returns>
    public async IAsyncEnumerable<byte[]> DecodeStreamAsync(
        IAsyncEnumerable<byte[]> opusFrames,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await foreach (var frame in opusFrames.WithCancellation(cancellationToken))
        {
            if (frame.Length == 0) continue;

            byte[] pcmBytes;
            try
            {
                pcmBytes = _codec.DecodeToBytes(frame);
            }
            catch (OpusDecodingException ex)
            {
                _logger.LogWarning(ex, "Failed to decode frame, skipping");
                continue;
            }

            yield return pcmBytes;
        }
    }

    /// <summary>
    /// Clears the internal sample buffer.
    /// </summary>
    public void ClearBuffer()
    {
        lock (_bufferLock)
        {
            _sampleBuffer.Clear();
        }
    }

    /// <summary>
    /// Disposes the stream processor.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearBuffer();
        GC.SuppressFinalize(this);
    }
}

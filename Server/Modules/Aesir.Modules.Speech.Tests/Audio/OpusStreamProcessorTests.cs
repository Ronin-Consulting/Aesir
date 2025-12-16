using Aesir.Modules.Speech.Services.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Modules.Speech.Tests.Audio;

/// <summary>
/// Unit tests for the OpusStreamProcessor class.
/// </summary>
public class OpusStreamProcessorTests : IDisposable
{
    private readonly ILogger<OpusCodec> _codecLogger;
    private readonly ILogger _processorLogger;
    private OpusCodec? _codec;
    private OpusStreamProcessor? _processor;

    public OpusStreamProcessorTests()
    {
        _codecLogger = NullLogger<OpusCodec>.Instance;
        _processorLogger = NullLogger.Instance;
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _codec?.Dispose();
    }

    private void SetupProcessor(OpusCodecConfig? config = null)
    {
        _codec = new OpusCodec(_codecLogger, config);
        _processor = new OpusStreamProcessor(_processorLogger, _codec);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_InitializesCorrectly()
    {
        // Arrange & Act
        SetupProcessor();

        // Assert
        _processor.Should().NotBeNull();
        _processor!.BufferedSamples.Should().Be(0);
    }

    #endregion

    #region EncodeStreamAsync Tests

    [Fact]
    public async Task EncodeStreamAsync_SingleCompleteFrame_ReturnsOneOpusFrame()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var pcmSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            yield return pcmBytes;
            await Task.CompletedTask;
        }

        // Act
        var opusFrames = new List<byte[]>();
        await foreach (var frame in _processor!.EncodeStreamAsync(GetChunks()))
        {
            opusFrames.Add(frame);
        }

        // Assert
        opusFrames.Should().HaveCount(1);
        opusFrames[0].Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task EncodeStreamAsync_MultipleSmallChunks_BuffersAndEncodesFrames()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var totalSamples = frameSizeSamples * 3; // 3 complete frames
        var pcmSamples = GenerateSineWave(totalSamples, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        // Split into small chunks (50 samples each)
        var chunkSize = 100; // 50 samples * 2 bytes
        var chunks = new List<byte[]>();
        for (var i = 0; i < pcmBytes.Length; i += chunkSize)
        {
            var remaining = Math.Min(chunkSize, pcmBytes.Length - i);
            var chunk = new byte[remaining];
            Array.Copy(pcmBytes, i, chunk, 0, remaining);
            chunks.Add(chunk);
        }

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
            await Task.CompletedTask;
        }

        // Act
        var opusFrames = new List<byte[]>();
        await foreach (var frame in _processor!.EncodeStreamAsync(GetChunks()))
        {
            opusFrames.Add(frame);
        }

        // Assert - should get 3 frames (might be 4 if there's padding)
        opusFrames.Count.Should().BeGreaterThanOrEqualTo(3);
        opusFrames.Should().AllSatisfy(f => f.Length.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task EncodeStreamAsync_EmptyChunks_SkipsEmptyChunks()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var pcmSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            yield return [];
            yield return pcmBytes;
            yield return [];
            await Task.CompletedTask;
        }

        // Act
        var opusFrames = new List<byte[]>();
        await foreach (var frame in _processor!.EncodeStreamAsync(GetChunks()))
        {
            opusFrames.Add(frame);
        }

        // Assert
        opusFrames.Should().HaveCount(1);
    }

    [Fact]
    public async Task EncodeStreamAsync_PartialFrame_FlushesWithPadding()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var partialSamples = frameSizeSamples / 2; // Half a frame
        var pcmSamples = GenerateSineWave(partialSamples, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            yield return pcmBytes;
            await Task.CompletedTask;
        }

        // Act
        var opusFrames = new List<byte[]>();
        await foreach (var frame in _processor!.EncodeStreamAsync(GetChunks()))
        {
            opusFrames.Add(frame);
        }

        // Assert - should get 1 padded frame
        opusFrames.Should().HaveCount(1);
    }

    [Fact]
    public async Task EncodeStreamAsync_EarlyBreak_StopsProcessing()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;

        // Create 5 frames worth of data
        var pcmSamples = GenerateSineWave(frameSizeSamples * 5, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            // Send all data in one chunk
            yield return pcmBytes;
            await Task.CompletedTask;
        }

        // Act - only take 2 frames
        var opusFrames = new List<byte[]>();
        var frameCount = 0;
        await foreach (var frame in _processor!.EncodeStreamAsync(GetChunks()))
        {
            opusFrames.Add(frame);
            frameCount++;
            if (frameCount >= 2)
            {
                break; // Early exit without cancellation
            }
        }

        // Assert - we should have gotten exactly 2 frames
        opusFrames.Should().HaveCount(2);
    }

    #endregion

    #region DecodeStreamAsync Tests

    [Fact]
    public async Task DecodeStreamAsync_ValidOpusFrames_ReturnsPcmBytes()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var originalSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var opusFrame = _codec.Encode(originalSamples);

        async IAsyncEnumerable<byte[]> GetOpusFrames()
        {
            yield return opusFrame;
            await Task.CompletedTask;
        }

        // Act
        var pcmChunks = new List<byte[]>();
        await foreach (var chunk in _processor!.DecodeStreamAsync(GetOpusFrames()))
        {
            pcmChunks.Add(chunk);
        }

        // Assert
        pcmChunks.Should().HaveCount(1);
        pcmChunks[0].Length.Should().Be(frameSizeSamples * 2);
    }

    [Fact]
    public async Task DecodeStreamAsync_MultipleFrames_DecodesAll()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        const int frameCount = 5;
        var opusFrames = new List<byte[]>();

        for (var i = 0; i < frameCount; i++)
        {
            var samples = GenerateSineWave(frameSizeSamples, 220 + (i * 50), 16000);
            opusFrames.Add(_codec.Encode(samples));
        }

        async IAsyncEnumerable<byte[]> GetOpusFrames()
        {
            foreach (var frame in opusFrames)
            {
                yield return frame;
            }
            await Task.CompletedTask;
        }

        // Act
        var pcmChunks = new List<byte[]>();
        await foreach (var chunk in _processor!.DecodeStreamAsync(GetOpusFrames()))
        {
            pcmChunks.Add(chunk);
        }

        // Assert
        pcmChunks.Should().HaveCount(frameCount);
        pcmChunks.Should().AllSatisfy(c => c.Length.Should().Be(frameSizeSamples * 2));
    }

    [Fact]
    public async Task DecodeStreamAsync_EmptyFrames_SkipsEmptyFrames()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var samples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var opusFrame = _codec.Encode(samples);

        async IAsyncEnumerable<byte[]> GetOpusFrames()
        {
            yield return [];
            yield return opusFrame;
            yield return [];
            await Task.CompletedTask;
        }

        // Act
        var pcmChunks = new List<byte[]>();
        await foreach (var chunk in _processor!.DecodeStreamAsync(GetOpusFrames()))
        {
            pcmChunks.Add(chunk);
        }

        // Assert
        pcmChunks.Should().HaveCount(1);
    }

    #endregion

    #region ClearBuffer Tests

    [Fact]
    public void ClearBuffer_AfterAddingData_ClearsBuffer()
    {
        // Arrange
        SetupProcessor();

        // We can't easily add data to the buffer without encoding,
        // so just verify the method doesn't throw
        // Act & Assert
        var action = () => _processor!.ClearBuffer();
        action.Should().NotThrow();
        _processor!.BufferedSamples.Should().Be(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        SetupProcessor();

        // Act & Assert
        var action = () =>
        {
            _processor!.Dispose();
            _processor.Dispose();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public async Task EncodeStreamAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        SetupProcessor();
        _processor!.Dispose();

        async IAsyncEnumerable<byte[]> GetChunks()
        {
            yield return new byte[100];
            await Task.CompletedTask;
        }

        // Act & Assert
        var action = async () =>
        {
            await foreach (var _ in _processor.EncodeStreamAsync(GetChunks()))
            {
                // Should throw before we get here
            }
        };
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public async Task EncodeAndDecode_Roundtrip_ProducesValidOutput()
    {
        // Arrange
        SetupProcessor();
        var frameSizeSamples = _codec!.FrameSizeSamples;
        var originalSamples = GenerateSineWave(frameSizeSamples * 3, 440, 16000);
        var originalBytes = new byte[originalSamples.Length * 2];
        Buffer.BlockCopy(originalSamples, 0, originalBytes, 0, originalBytes.Length);

        async IAsyncEnumerable<byte[]> GetPcmChunks()
        {
            // Send in one chunk
            yield return originalBytes;
            await Task.CompletedTask;
        }

        // Act - Encode
        var opusFrames = new List<byte[]>();
        await foreach (var frame in _processor!.EncodeStreamAsync(GetPcmChunks()))
        {
            opusFrames.Add(frame);
        }

        // Create new processor for decode (reset state)
        _processor.Dispose();
        _processor = new OpusStreamProcessor(_processorLogger, _codec);

        async IAsyncEnumerable<byte[]> GetOpusFrames()
        {
            foreach (var frame in opusFrames)
            {
                yield return frame;
            }
            await Task.CompletedTask;
        }

        // Act - Decode
        var decodedChunks = new List<byte[]>();
        await foreach (var chunk in _processor.DecodeStreamAsync(GetOpusFrames()))
        {
            decodedChunks.Add(chunk);
        }

        // Assert
        opusFrames.Should().HaveCountGreaterThanOrEqualTo(3, "Should produce at least 3 Opus frames");
        decodedChunks.Should().HaveCountGreaterThanOrEqualTo(3, "Should decode all frames");

        // Verify decoded audio has content (not silence)
        var totalDecodedBytes = decodedChunks.Sum(c => c.Length);
        totalDecodedBytes.Should().BeGreaterThan(0, "Should produce decoded audio");

        // Combine and verify energy
        var decodedBytes = new byte[totalDecodedBytes];
        var offset = 0;
        foreach (var chunk in decodedChunks)
        {
            Array.Copy(chunk, 0, decodedBytes, offset, chunk.Length);
            offset += chunk.Length;
        }

        var decodedSamples = new short[decodedBytes.Length / 2];
        Buffer.BlockCopy(decodedBytes, 0, decodedSamples, 0, decodedBytes.Length);

        var decodedEnergy = CalculateEnergy(decodedSamples);
        decodedEnergy.Should().BeGreaterThan(0, "Decoded audio should not be silent");
    }

    #endregion

    #region Helper Methods

    private static short[] GenerateSineWave(int sampleCount, double frequencyHz, int sampleRate)
    {
        var samples = new short[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var time = (double)i / sampleRate;
            var value = Math.Sin(2 * Math.PI * frequencyHz * time);
            samples[i] = (short)(value * short.MaxValue * 0.8);
        }
        return samples;
    }

    private static double CalculateEnergy(short[] samples)
    {
        return samples.Sum(s => (double)s * s);
    }

    #endregion
}

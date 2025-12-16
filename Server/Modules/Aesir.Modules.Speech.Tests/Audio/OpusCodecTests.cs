using Aesir.Modules.Speech.Services.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Modules.Speech.Tests.Audio;

/// <summary>
/// Unit tests for the OpusCodec class.
/// </summary>
public class OpusCodecTests : IDisposable
{
    private readonly ILogger<OpusCodec> _logger;
    private OpusCodec? _codec;

    public OpusCodecTests()
    {
        _logger = NullLogger<OpusCodec>.Instance;
    }

    public void Dispose()
    {
        _codec?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaultConfig_InitializesCorrectly()
    {
        // Arrange & Act
        _codec = new OpusCodec(_logger);

        // Assert
        _codec.SampleRate.Should().Be(16000);
        _codec.Channels.Should().Be(1);
        _codec.Bitrate.Should().Be(32000);
        _codec.FrameSizeSamples.Should().Be(320); // 20ms at 16kHz
    }

    [Fact]
    public void Constructor_WithCustomConfig_InitializesCorrectly()
    {
        // Arrange
        var config = new OpusCodecConfig
        {
            SampleRate = 48000,
            Channels = 2,
            Bitrate = 64000,
            FrameSizeMs = 10
        };

        // Act
        _codec = new OpusCodec(_logger, config);

        // Assert
        _codec.SampleRate.Should().Be(48000);
        _codec.Channels.Should().Be(2);
        _codec.Bitrate.Should().Be(64000);
        _codec.FrameSizeSamples.Should().Be(480); // 10ms at 48kHz
    }

    [Theory]
    [InlineData(OpusApplicationType.VoIP)]
    [InlineData(OpusApplicationType.Audio)]
    [InlineData(OpusApplicationType.LowDelay)]
    public void Constructor_WithDifferentApplicationTypes_InitializesCorrectly(OpusApplicationType appType)
    {
        // Arrange
        var config = new OpusCodecConfig { Application = appType };

        // Act
        _codec = new OpusCodec(_logger, config);

        // Assert - no exception thrown
        _codec.Should().NotBeNull();
    }

    #endregion

    #region Encode Tests

    [Fact]
    public void Encode_ValidPcmSamples_ReturnsOpusData()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var pcmSamples = GenerateSineWave(frameSizeSamples, 440, 16000);

        // Act
        var opusData = _codec.Encode(pcmSamples);

        // Assert
        opusData.Should().NotBeNull();
        opusData.Length.Should().BeGreaterThan(0);
        opusData.Length.Should().BeLessThan(frameSizeSamples * 2); // Opus should compress
    }

    [Fact]
    public void Encode_SilentPcmSamples_ReturnsSmallOpusFrame()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var silence = new short[frameSizeSamples];

        // Act
        var opusData = _codec.Encode(silence);

        // Assert
        opusData.Should().NotBeNull();
        opusData.Length.Should().BeGreaterThan(0);
        // Silence should compress very well
        opusData.Length.Should().BeLessThan(50);
    }

    [Fact]
    public void Encode_PcmBytes_ReturnsOpusData()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var pcmSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var pcmBytes = new byte[pcmSamples.Length * 2];
        Buffer.BlockCopy(pcmSamples, 0, pcmBytes, 0, pcmBytes.Length);

        // Act
        var opusData = _codec.Encode(pcmBytes);

        // Assert
        opusData.Should().NotBeNull();
        opusData.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Encode_MismatchedSampleCount_PadsOrTruncates()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var shortSamples = new short[frameSizeSamples / 2]; // Half the expected size

        // Act - should not throw, should pad with zeros
        var opusData = _codec.Encode(shortSamples);

        // Assert
        opusData.Should().NotBeNull();
        opusData.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Encode_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var samples = new short[_codec.FrameSizeSamples];
        _codec.Dispose();

        // Act & Assert
        var action = () => _codec.Encode(samples);
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Decode Tests

    [Fact]
    public void Decode_ValidOpusData_ReturnsPcmSamples()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var originalSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var opusData = _codec.Encode(originalSamples);

        // Act
        var decodedSamples = _codec.Decode(opusData);

        // Assert
        decodedSamples.Should().NotBeNull();
        decodedSamples.Length.Should().Be(frameSizeSamples);
    }

    [Fact]
    public void Decode_EmptyOpusData_ReturnsSilence()
    {
        // Arrange
        _codec = new OpusCodec(_logger);

        // Act
        var decodedSamples = _codec.Decode([]);

        // Assert
        decodedSamples.Should().NotBeNull();
        decodedSamples.Length.Should().Be(_codec.FrameSizeSamples);
        decodedSamples.Should().AllSatisfy(s => s.Should().Be(0));
    }

    [Fact]
    public void Decode_NullOpusData_ReturnsSilence()
    {
        // Arrange
        _codec = new OpusCodec(_logger);

        // Act
        var decodedSamples = _codec.Decode(null!);

        // Assert
        decodedSamples.Should().NotBeNull();
        decodedSamples.Length.Should().Be(_codec.FrameSizeSamples);
    }

    [Fact]
    public void DecodeToBytes_ValidOpusData_ReturnsPcmBytes()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var originalSamples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var opusData = _codec.Encode(originalSamples);

        // Act
        var pcmBytes = _codec.DecodeToBytes(opusData);

        // Assert
        pcmBytes.Should().NotBeNull();
        pcmBytes.Length.Should().Be(frameSizeSamples * 2); // 16-bit = 2 bytes per sample
    }

    [Fact]
    public void Decode_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var samples = GenerateSineWave(_codec.FrameSizeSamples, 440, 16000);
        var opusData = _codec.Encode(samples);
        _codec.Dispose();

        // Act & Assert
        var action = () => _codec.Decode(opusData);
        action.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void EncodeAndDecode_Roundtrip_ProducesValidOutput()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var originalSamples = GenerateSineWave(frameSizeSamples, 440, 16000);

        // Act
        var opusData = _codec.Encode(originalSamples);
        var decodedSamples = _codec.Decode(opusData);

        // Assert
        decodedSamples.Length.Should().Be(originalSamples.Length);

        // Opus is lossy - verify that decoded audio has similar energy (not silence)
        var originalEnergy = CalculateEnergy(originalSamples);
        var decodedEnergy = CalculateEnergy(decodedSamples);

        // Decoded should have at least 50% of original energy (lossy compression)
        decodedEnergy.Should().BeGreaterThan(originalEnergy * 0.5,
            "Decoded audio should retain significant energy");

        // Decoded should not have more than 150% energy (no amplification expected)
        decodedEnergy.Should().BeLessThan(originalEnergy * 1.5,
            "Decoded audio should not be significantly louder");
    }

    [Fact]
    public void EncodeAndDecode_MultipleFrames_ProducesValidOutput()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        const int frameCount = 10;

        // Act & Assert
        for (var i = 0; i < frameCount; i++)
        {
            var frequency = 220 + (i * 50); // Vary frequency
            var originalSamples = GenerateSineWave(frameSizeSamples, frequency, 16000);
            var opusData = _codec.Encode(originalSamples);
            var decodedSamples = _codec.Decode(opusData);

            decodedSamples.Length.Should().Be(originalSamples.Length,
                $"Frame {i} should decode to correct length");

            // Verify decoded audio is not silence
            var decodedEnergy = CalculateEnergy(decodedSamples);
            decodedEnergy.Should().BeGreaterThan(0, $"Frame {i} should not be silent");
        }
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Encode_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        const int taskCount = 10;
        var tasks = new List<Task<byte[]>>();

        // Act
        for (var i = 0; i < taskCount; i++)
        {
            var freq = 220 + (i * 50);
            tasks.Add(Task.Run(() =>
            {
                var samples = GenerateSineWave(frameSizeSamples, freq, 16000);
                return _codec!.Encode(samples);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all tasks completed successfully with valid output
        results.Should().HaveCount(taskCount);
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r.Length.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    public async Task Decode_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        _codec = new OpusCodec(_logger);
        var frameSizeSamples = _codec.FrameSizeSamples;
        var samples = GenerateSineWave(frameSizeSamples, 440, 16000);
        var opusData = _codec.Encode(samples);
        const int taskCount = 10;

        // Act
        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() => _codec!.Decode(opusData)))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(taskCount);
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r.Length.Should().Be(frameSizeSamples);
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a sine wave for testing audio.
    /// </summary>
    private static short[] GenerateSineWave(int sampleCount, double frequencyHz, int sampleRate)
    {
        var samples = new short[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var time = (double)i / sampleRate;
            var value = Math.Sin(2 * Math.PI * frequencyHz * time);
            samples[i] = (short)(value * short.MaxValue * 0.8); // 80% amplitude to avoid clipping
        }
        return samples;
    }

    /// <summary>
    /// Calculates the energy (sum of squared samples) of an audio signal.
    /// Used to verify that decoded audio is not silent.
    /// </summary>
    private static double CalculateEnergy(short[] samples)
    {
        return samples.Sum(s => (double)s * s);
    }

    #endregion
}

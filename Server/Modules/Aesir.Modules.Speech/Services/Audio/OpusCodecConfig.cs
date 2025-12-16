namespace Aesir.Modules.Speech.Services.Audio;

/// <summary>
/// Configuration for Opus encoder/decoder settings.
/// </summary>
public record OpusCodecConfig
{
    /// <summary>
    /// Default configuration optimized for speech.
    /// </summary>
    public static OpusCodecConfig Default => new();

    /// <summary>
    /// Target bitrate in bits per second. Recommended values:
    /// - 24000 for speech (VoIP quality)
    /// - 32000 for better speech quality
    /// - 48000-64000 for high quality speech/music
    /// </summary>
    public int Bitrate { get; set; } = 32000;

    /// <summary>
    /// Sample rate in Hz. Must match input/output audio.
    /// Common values: 8000, 12000, 16000, 24000, 48000.
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// Number of audio channels (1=mono, 2=stereo).
    /// Speech typically uses mono.
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// Opus application type: VoIP, Audio, or LowDelay.
    /// VoIP is optimized for speech.
    /// </summary>
    public OpusApplicationType Application { get; set; } = OpusApplicationType.VoIP;

    /// <summary>
    /// Encoder complexity (0-10). Higher = better quality, more CPU.
    /// 5 is default, 10 is best quality.
    /// </summary>
    public int Complexity { get; set; } = 5;

    /// <summary>
    /// Frame size in milliseconds. Opus supports 2.5, 5, 10, 20, 40, 60ms.
    /// 20ms is standard for real-time speech.
    /// </summary>
    public int FrameSizeMs { get; set; } = 20;

    /// <summary>
    /// Enable Forward Error Correction for lossy networks.
    /// </summary>
    public bool EnableFec { get; set; } = false;

    /// <summary>
    /// Enable Discontinuous Transmission (silence suppression).
    /// </summary>
    public bool EnableDtx { get; set; } = false;

    /// <summary>
    /// Calculate the frame size in samples based on sample rate and frame duration.
    /// </summary>
    public int FrameSizeSamples => (SampleRate * FrameSizeMs) / 1000;
}

/// <summary>
/// Opus application type for encoder optimization.
/// </summary>
public enum OpusApplicationType
{
    /// <summary>
    /// Optimized for voice over IP and conferencing.
    /// Best for speech with low latency requirements.
    /// </summary>
    VoIP = 2048,

    /// <summary>
    /// Optimized for music and general audio.
    /// Better quality but higher latency.
    /// </summary>
    Audio = 2049,

    /// <summary>
    /// Optimized for lowest possible latency.
    /// Sacrifices some quality for reduced delay.
    /// </summary>
    LowDelay = 2051
}

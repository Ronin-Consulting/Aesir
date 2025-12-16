namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Represents the current state of audio playback.
/// </summary>
public enum AudioPlaybackState
{
    /// <summary>
    /// Service not initialized.
    /// </summary>
    NotInitialized,

    /// <summary>
    /// Ready to play (initialized, not playing).
    /// </summary>
    Ready,

    /// <summary>
    /// Buffering audio data.
    /// </summary>
    Buffering,

    /// <summary>
    /// Actively playing audio.
    /// </summary>
    Playing,

    /// <summary>
    /// Error state - playback failed.
    /// </summary>
    Error
}

/// <summary>
/// Interface for audio playback service that handles TTS audio output.
/// Used for playing AI responses in hands-free mode.
/// </summary>
public interface IAudioPlaybackService : IAsyncDisposable
{
    /// <summary>
    /// Indicates whether audio playback is supported on this platform/browser.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    AudioPlaybackState State { get; }

    /// <summary>
    /// Event raised when playback state changes.
    /// </summary>
    event EventHandler<AudioPlaybackState>? OnStateChanged;

    /// <summary>
    /// Event raised when playback completes (all queued audio has finished).
    /// </summary>
    event EventHandler? OnPlaybackComplete;

    /// <summary>
    /// Initialize the audio playback system.
    /// Must be called before queuing audio.
    /// </summary>
    /// <param name="sampleRate">Target sample rate (default 22050 for TTS output).</param>
    /// <returns>True if initialization succeeded.</returns>
    Task<bool> InitializeAsync(int sampleRate = 22050);

    /// <summary>
    /// Queue audio data for playback.
    /// Audio will be buffered and played in order.
    /// </summary>
    /// <param name="audioData">The audio data to queue.</param>
    /// <param name="format">Audio format: "wav", "opus", or "pcm".</param>
    /// <returns>True if audio was queued successfully.</returns>
    Task<bool> QueueAudioAsync(byte[] audioData, string format = "wav");

    /// <summary>
    /// Start playback of queued audio.
    /// Playback starts automatically when enough chunks are buffered.
    /// </summary>
    Task PlayAsync();

    /// <summary>
    /// Stop playback and clear the audio queue.
    /// Used to interrupt AI response when user starts speaking.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Check if audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }
}

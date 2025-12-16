namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Represents the current state of audio capture.
/// </summary>
public enum AudioCaptureState
{
    /// <summary>
    /// Service not initialized.
    /// </summary>
    NotInitialized,

    /// <summary>
    /// Ready to capture (initialized, not capturing).
    /// </summary>
    Ready,

    /// <summary>
    /// Actively capturing audio.
    /// </summary>
    Capturing,

    /// <summary>
    /// Error state - capture failed.
    /// </summary>
    Error
}

/// <summary>
/// Event arguments for audio chunk events.
/// </summary>
public class AudioChunkEventArgs : EventArgs
{
    /// <summary>
    /// The audio data chunk (format depends on capture settings).
    /// </summary>
    public byte[] Data { get; }

    /// <summary>
    /// The audio format (e.g., "webm/opus", "wav").
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Creates a new AudioChunkEventArgs.
    /// </summary>
    public AudioChunkEventArgs(byte[] data, string format)
    {
        Data = data;
        Format = format;
    }
}

/// <summary>
/// Event arguments for capture error events.
/// </summary>
public class CaptureErrorEventArgs : EventArgs
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Creates a new CaptureErrorEventArgs.
    /// </summary>
    public CaptureErrorEventArgs(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Interface for audio capture service that handles microphone input.
/// Used for push-to-talk functionality in hands-free mode.
/// </summary>
public interface IAudioCaptureService : IAsyncDisposable
{
    /// <summary>
    /// Indicates whether audio capture is supported on this platform/browser.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Indicates whether Opus codec is supported for capture.
    /// </summary>
    bool HasOpusSupport { get; }

    /// <summary>
    /// Gets the current capture state.
    /// </summary>
    AudioCaptureState State { get; }

    /// <summary>
    /// Event raised when an audio chunk is captured.
    /// </summary>
    event EventHandler<AudioChunkEventArgs>? OnAudioChunk;

    /// <summary>
    /// Event raised when a capture error occurs.
    /// </summary>
    event EventHandler<CaptureErrorEventArgs>? OnCaptureError;

    /// <summary>
    /// Event raised when capture state changes.
    /// </summary>
    event EventHandler<AudioCaptureState>? OnStateChanged;

    /// <summary>
    /// Initialize the audio capture system.
    /// Must be called before starting capture.
    /// </summary>
    /// <param name="sampleRate">Target sample rate (default 16000 for STT).</param>
    /// <returns>True if initialization succeeded.</returns>
    Task<bool> InitializeAsync(int sampleRate = 16000);

    /// <summary>
    /// Start capturing audio from the microphone.
    /// Requires user gesture in browser context.
    /// </summary>
    /// <returns>True if capture started successfully.</returns>
    Task<bool> StartCaptureAsync();

    /// <summary>
    /// Stop capturing audio and release microphone.
    /// </summary>
    Task StopCaptureAsync();

    /// <summary>
    /// Get the current microphone permission status.
    /// </summary>
    /// <returns>Permission status: "granted", "denied", or "prompt".</returns>
    Task<string> GetMicrophonePermissionAsync();
}

using System;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines the contract for hands-free voice interaction services.
/// Provides functionality to start/stop hands-free mode and manage voice-based conversations.
/// </summary>
public interface IHandsFreeService
{
    /// <summary>
    /// Gets whether hands-free mode is currently active.
    /// </summary>
    bool IsHandsFreeActive { get; }

    /// <summary>
    /// Gets the current state of the hands-free service.
    /// </summary>
    HandsFreeState CurrentState { get; }

    /// <summary>
    /// Starts hands-free mode with configurable silence detection.
    /// </summary>
    /// <param name="silenceTimeoutSeconds">Duration of silence before stopping listening (default: 5 seconds).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartHandsFreeMode(float silenceTimeoutSeconds = 5.0f);

    /// <summary>
    /// Stops hands-free mode and cleans up resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopHandsFreeMode();

    /// <summary>
    /// Event raised when the hands-free state changes.
    /// </summary>
    event EventHandler<HandsFreeStateChangedEventArgs> StateChanged;

    /// <summary>
    /// Event raised when audio level changes during TTS playback.
    /// Used for UI oscillation effects.
    /// </summary>
    event EventHandler<AudioLevelEventArgs> AudioLevelChanged;
}

/// <summary>
/// Represents the different states of the hands-free service.
/// </summary>
public enum HandsFreeState
{
    /// <summary>
    /// Service is inactive.
    /// </summary>
    Idle,

    /// <summary>
    /// Listening for user speech input.
    /// </summary>
    Listening,

    /// <summary>
    /// Processing speech-to-text and generating chat completion.
    /// </summary>
    Processing,

    /// <summary>
    /// Playing AI response via text-to-speech.
    /// </summary>
    Speaking,

    /// <summary>
    /// An error occurred during processing.
    /// </summary>
    Error
}

/// <summary>
/// Event arguments for hands-free state changes.
/// </summary>
public class HandsFreeStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previous state.
    /// </summary>
    public HandsFreeState PreviousState { get; init; }

    /// <summary>
    /// The new current state.
    /// </summary>
    public HandsFreeState CurrentState { get; init; }

    /// <summary>
    /// Optional error message if transitioning to Error state.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event arguments for audio level changes during TTS playback.
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    /// <summary>
    /// The current audio level (0.0 to 1.0).
    /// </summary>
    public float AudioLevel { get; init; }

    /// <summary>
    /// Whether audio is currently being produced.
    /// </summary>
    public bool IsAudioActive { get; init; }
}
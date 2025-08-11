using System;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Represents the interface for hands-free voice interaction services.
/// Provides methods to start and stop hands-free mode, access the current state,
/// and monitor audio levels and state changes through events.
/// </summary>
public interface IHandsFreeService
{
    /// <summary>
    /// Indicates whether hands-free mode is currently active.
    /// </summary>
    bool IsHandsFreeActive { get; }

    /// <summary>
    /// Gets the current state of the hands-free service.
    /// </summary>
    HandsFreeState CurrentState { get; }

    /// <summary>
    /// Initiates hands-free mode, enabling voice-based interaction with optional silence detection configuration.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartHandsFreeMode();

    /// <summary>
    /// Stops hands-free mode and performs necessary cleanup operations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopHandsFreeMode();

    /// <summary>
    /// Occurs when the hands-free state changes, providing information about the previous and current states.
    /// </summary>
    event EventHandler<HandsFreeStateChangedEventArgs> StateChanged;

    /// <summary>
    /// Occurs when the audio level changes during TTS playback or hands-free interaction.
    /// </summary>
    event EventHandler<AudioLevelEventArgs> AudioLevelChanged;
}

/// <summary>
/// Represents the potential operational states of the hands-free interaction service.
/// Used to track and manage transitions such as idle, listening for input, processing commands,
/// providing output, or encountering errors within the hands-free system.
/// </summary>
public enum HandsFreeState
{
    /// <summary>
    /// Indicates that the hands-free service is currently inactive.
    /// </summary>
    Idle,

    /// <summary>
    /// Indicates that the hands-free service is actively listening for user input.
    /// </summary>
    Listening,

    /// <summary>
    /// Indicates that the system is processing an incoming command or request.
    /// </summary>
    Processing,

    /// <summary>
    /// Service is actively speaking or providing audio output.
    /// </summary>
    Speaking,

    /// <summary>
    /// Indicates an error state in the hands-free service.
    /// This represents a failure or malfunction that prevents normal operation.
    /// </summary>
    Error
}

/// <summary>
/// Represents the arguments for the event raised when the state of a hands-free interaction changes.
/// Provides details about the previous and current states, as well as any error information if applicable.
/// </summary>
public class HandsFreeStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the state of the hands-free service prior to the most recent change.
    /// </summary>
    public HandsFreeState PreviousState { get; init; }

    /// <summary>
    /// Gets the current state of the hands-free service.
    /// </summary>
    public HandsFreeState CurrentState { get; init; }

    /// <summary>
    /// Gets the error message associated with the hands-free state transition,
    /// if the state change is due to an error.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Provides data for events related to audio level changes during TTS playback.
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    /// <summary>
    /// Represents the current audio level during TTS playback or voice interaction.
    /// </summary>
    public float AudioLevel { get; init; }

    /// <summary>
    /// Indicates whether audio is currently active during TTS playback or voice interaction.
    /// </summary>
    public bool IsAudioActive { get; init; }
}
using System;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines an interface for managing hands-free voice interaction functionalities within a service.
/// Enables controlling the activation state, retrieving the current hands-free state,
/// and handling associated events for state changes, audio levels, and recognized utterances.
/// </summary>
public interface IHandsFreeService
{
    /// <summary>
    /// Gets a value indicating whether hands-free mode is currently active.
    /// </summary>
    bool IsHandsFreeActive { get; }

    /// <summary>
    /// Represents the current operational state of the hands-free service.
    /// Provides information about whether the system is idle, listening, processing, speaking, or in an error state.
    /// </summary>
    HandsFreeState CurrentState { get; }

    /// <summary>
    /// Activates the hands-free mode, allowing for voice interaction and input processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of starting hands-free mode.</returns>
    Task StartHandsFreeMode();

    /// <summary>
    /// Terminates hands-free mode, disabling voice-based interaction and performing necessary resource management.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopHandsFreeMode();

    /// <summary>
    /// Occurs when the hands-free state changes, providing information about the new state.
    /// </summary>
    event EventHandler<HandsFreeStateChangedEventArgs> StateChanged;

    /// <summary>
    /// Event that occurs when the audio level changes during hands-free mode operation.
    /// </summary>
    event EventHandler<AudioLevelEventArgs> AudioLevelChanged;

    /// <summary>
    /// Event triggered when recognized text from a user's voice input is available.
    /// </summary>
    event EventHandler<UtteranceTextEventArgs> UtteranceTextRecognized;
}

/// <summary>
/// Defines the various states that the hands-free interaction service can operate in.
/// These states represent different phases of functionality such as being idle,
/// listening for user input, processing received commands, speaking a response,
/// or handling errors.
/// </summary>
public enum HandsFreeState
{
    /// <summary>
    /// Represents a state where the hands-free service is neither active nor processing any tasks.
    /// </summary>
    Idle,

    /// <summary>
    /// Indicates that the hands-free service is actively listening for user input or commands.
    /// </summary>
    Listening,

    /// <summary>
    /// Represents the state of the hands-free service actively processing
    /// received input or executing a command.
    /// </summary>
    Processing,

    /// <summary>
    /// Indicates that the hands-free service is actively delivering spoken output.
    /// </summary>
    Speaking,

    /// <summary>
    /// Represents a failed or problematic state in the hands-free service.
    /// Indicates that an error has occurred, preventing normal operation.
    /// </summary>
    Error
}

/// <summary>
/// Represents the arguments for the event triggered when the state of a hands-free interaction changes.
/// Encapsulates information about the previous state, the new current state,
/// and an optional error message providing additional context in case of issues.
/// </summary>
public class HandsFreeStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Represents the state of the hands-free interaction prior to the most recent state change.
    /// </summary>
    public HandsFreeState PreviousState { get; init; }

    /// <summary>
    /// Represents the current state of the hands-free operation.
    /// </summary>
    public HandsFreeState CurrentState { get; init; }

    /// <summary>
    /// Gets the error message associated with the transition to the error state, if applicable.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Represents the data associated with audio level changes during text-to-speech (TTS) playback.
/// Provides information about the current audio level and whether audio is actively playing.
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    /// <summary>
    /// Represents the current audio level, typically scaled or adjusted for visual or auditory representation.
    /// </summary>
    public float AudioLevel { get; init; }

    /// <summary>
    /// Determines whether the audio is currently active during TTS playback.
    /// </summary>
    public bool IsAudioActive { get; init; }
}

/// <summary>
/// Provides data for the event that is raised when an utterance text is recognized
/// during hands-free voice interactions.
/// </summary>
public class UtteranceTextEventArgs : EventArgs
{
    /// <summary>
    /// Represents the text content of a recognized utterance.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
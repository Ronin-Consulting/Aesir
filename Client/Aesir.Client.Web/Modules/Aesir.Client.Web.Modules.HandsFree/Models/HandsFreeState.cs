namespace Aesir.Client.Web.Modules.HandsFree.Models;

/// <summary>
/// Represents the current state of the hands-free voice interaction.
/// </summary>
public enum HandsFreeState
{
    /// <summary>
    /// Idle state - ready for push-to-talk interaction.
    /// Avatar shows subtle breathing animation.
    /// </summary>
    Idle,

    /// <summary>
    /// Listening state - actively capturing user's voice.
    /// Avatar shows pulsing glow effect.
    /// </summary>
    Listening,

    /// <summary>
    /// Processing state - audio sent for STT and chat processing.
    /// Avatar shows gentle rotation animation.
    /// </summary>
    Processing,

    /// <summary>
    /// Speaking state - playing TTS response from AI.
    /// Avatar shows expanding ripple wave animation.
    /// </summary>
    Speaking,

    /// <summary>
    /// Error state - something went wrong.
    /// Avatar shows red shake animation.
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
    public HandsFreeState PreviousState { get; }

    /// <summary>
    /// The new current state.
    /// </summary>
    public HandsFreeState CurrentState { get; }

    /// <summary>
    /// Optional error message (when state is Error).
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a new state changed event.
    /// </summary>
    public HandsFreeStateChangedEventArgs(
        HandsFreeState previousState,
        HandsFreeState currentState,
        string? errorMessage = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Event arguments for transcription updates.
/// </summary>
public class TranscriptionEventArgs : EventArgs
{
    /// <summary>
    /// The transcribed text (partial or final).
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Indicates if this is the final transcription.
    /// </summary>
    public bool IsFinal { get; }

    /// <summary>
    /// Creates a new transcription event.
    /// </summary>
    public TranscriptionEventArgs(string text, bool isFinal)
    {
        Text = text;
        IsFinal = isFinal;
    }
}

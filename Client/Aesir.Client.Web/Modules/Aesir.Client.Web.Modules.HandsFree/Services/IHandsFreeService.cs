using Aesir.Client.Web.Modules.HandsFree.Models;

namespace Aesir.Client.Web.Modules.HandsFree.Services;

/// <summary>
/// Interface for the hands-free voice interaction service.
/// Orchestrates the full push-to-talk voice conversation flow.
/// </summary>
public interface IHandsFreeService : IAsyncDisposable
{
    /// <summary>
    /// Gets the current state of the hands-free interaction.
    /// </summary>
    HandsFreeState State { get; }

    /// <summary>
    /// Gets the current agent ID for chat interactions.
    /// </summary>
    Guid? CurrentAgentId { get; set; }

    /// <summary>
    /// Gets the current conversation ID.
    /// </summary>
    Guid? CurrentConversationId { get; set; }

    /// <summary>
    /// Gets the current session title (AI-generated after first message).
    /// </summary>
    string? CurrentSessionTitle { get; }

    /// <summary>
    /// Gets whether any messages were actually exchanged in this session.
    /// Used to determine if the conversation should be persisted on exit.
    /// </summary>
    bool HasExchangedMessages { get; }

    /// <summary>
    /// Gets the last transcribed user speech.
    /// </summary>
    string? LastTranscription { get; }

    /// <summary>
    /// Gets the last assistant response text.
    /// </summary>
    string? LastResponse { get; }

    /// <summary>
    /// Gets the last error message (when in Error state).
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    event EventHandler<HandsFreeStateChangedEventArgs>? OnStateChanged;

    /// <summary>
    /// Event raised when transcription updates (partial or final).
    /// </summary>
    event EventHandler<TranscriptionEventArgs>? OnTranscription;

    /// <summary>
    /// Event raised when assistant response text is received.
    /// </summary>
    event EventHandler<string>? OnResponseText;

    /// <summary>
    /// Initialize the hands-free service.
    /// Must be called before using push-to-talk.
    /// </summary>
    /// <returns>True if initialization succeeded.</returns>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Start listening (push-to-talk button pressed).
    /// Begins capturing audio from the microphone.
    /// </summary>
    Task StartListeningAsync();

    /// <summary>
    /// Stop listening (push-to-talk button released).
    /// Stops audio capture and begins processing.
    /// </summary>
    Task StopListeningAsync();

    /// <summary>
    /// Interrupt the current response (e.g., user starts new push-to-talk).
    /// Stops TTS playback and returns to Idle state.
    /// </summary>
    Task InterruptAsync();

    /// <summary>
    /// Reset the service to idle state.
    /// Clears any errors and resets state.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Deactivate the service and release audio resources (microphone, SignalR connections).
    /// Call this when leaving the hands-free page to free hardware resources.
    /// The service can be reactivated by calling InitializeAsync() again.
    /// </summary>
    Task DeactivateAsync();
}

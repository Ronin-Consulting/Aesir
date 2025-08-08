using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines methods for handling speech synthesis and recognition functionalities.
/// </summary>
public interface ISpeechService
{
    /// Asynchronously speaks the provided text.
    /// The actual behavior depends on the implementation of the ISpeechService interface.
    /// <param name="text">The text to be synthesized and spoken.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task SpeakAsync(string text);

    /// <summary>
    /// Stops the active text-to-speech task or cancels the ongoing speech operation if applicable.
    /// </summary>
    /// <returns>A task representing the asynchronous stop speech operation.</returns>
    Task StopSpeaking();

    /// <summary>
    /// Starts listening for speech input and provides recognized speech text asynchronously
    /// as an enumeration of strings. Optionally detects periods of silence and executes a provided action.
    /// </summary>
    /// <param name="silenceDetectedAction">
    /// An optional action invoked when silence is detected. The action receives
    /// the duration of detected silence in milliseconds as its parameter.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of recognized speech text strings.
    /// </returns>
    IAsyncEnumerable<string> ListenAsync(Action<int>? silenceDetectedAction = null);

    /// <summary>
    /// Stops the speech-to-text listening operation.
    /// </summary>
    /// <remarks>
    /// This method halts any ongoing audio recording process and disconnects
    /// the speech-to-text service if it is currently active.
    /// It ensures that all resources and operations related to the listening process are properly terminated.
    /// </remarks>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopListening();
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines the interface for speech-related services, including synthesis and recognition.
/// </summary>
public interface ISpeechService
{
    /// Asynchronously speaks the provided text.
    /// The actual behavior depends on the implementation of the ISpeechService interface.
    /// <param name="text">The text to be synthesized and spoken.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    Task SpeakAsync(string text);

    /// Stops the active text-to-speech task or cancels the ongoing speech operation if applicable.
    /// The behavior of this method depends on the specific implementation of the ISpeechService interface.
    /// <returns>A Task representing the asynchronous stop speech operation.</returns>
    Task StopSpeakingAsync();

    /// Asynchronously listens for speech and returns a list of recognized phrases.
    /// The recognition process can be customized with a function to determine whether to pause on silence.
    /// <param name="shouldPauseOnSilence">A function that receives the duration of silence in milliseconds and returns a boolean indicating whether to pause the recognition process.</param>
    /// <returns>A Task representing the asynchronous operation, containing a list of recognized speech phrases.</returns>
    Task<IList<string>> ListenAsync(Func<int, bool>? shouldPauseOnSilence);

    /// Stops the speech-to-text listening operation asynchronously.
    /// Terminates ongoing audio recording and ensures the proper disconnection of the
    /// speech service resources related to listening tasks.
    /// <returns>A Task representing the asynchronous stop operation.</returns>
    Task StopListeningAsync();
}
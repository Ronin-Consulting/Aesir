using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Represents a service interface for enabling speech synthesis and recognition capabilities.
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
    
    Task<IList<string>> ListenAsync(Func<int, bool>? shouldPauseOnSilence);

    /// Stops the speech-to-text listening operation.
    /// Halts ongoing audio recording and disconnects the speech-to-text service if active,
    /// ensuring proper termination of all listening-related resources and tasks.
    /// <returns>A Task representing the asynchronous stop operation.</returns>
    Task StopListeningAsync();
}
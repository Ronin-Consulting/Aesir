using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services.Implementations.NoOp;

/// <summary>
/// A no-operation implementation of the <see cref="ISpeechService"/> interface.
/// </summary>
/// <remarks>
/// This class provides a simple implementation of the speech service that does not perform any actual speech synthesis operations.
/// It is primarily used as a placeholder or fallback when no concrete speech service is available.
/// </remarks>
public class NoOpSpeechService : ISpeechService
{
    /// Asynchronously simulates the process of speaking the provided text.
    /// This is a no-operation implementation and does not produce any actual speech output.
    /// <param name="text">The text that would be spoken in a real speech service.</param>
    /// <returns>A completed Task representing the operation.</returns>
    public Task SpeakAsync(string text)
    {
        throw new System.NotImplementedException();
    }

    public Task StopSpeakingAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IList<string>> ListenAsync(Func<int, bool>? shouldPauseOnSilence = null)
    {
        throw new NotImplementedException();
    }

    public Task StopListeningAsync()
    {
        throw new NotImplementedException();
    }
}
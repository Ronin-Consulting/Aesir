using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Defines the contract for speech services, enabling text-to-speech functionality within the application.
/// Custom implementations may be provided to integrate with specific platforms or technologies.
/// </summary>
public interface ISpeechService
{
    /// Asynchronously processes and vocalizes the provided text input using a speech synthesis mechanism.
    /// The actual behavior depends on the implementation of the ISpeechService.
    /// <param name="text">The text content to be spoken by the speech synthesis service.</param>
    /// <returns>A Task representing the asynchronous operation of speaking the provided text.</returns>
    Task SpeakAsync(string text);
}
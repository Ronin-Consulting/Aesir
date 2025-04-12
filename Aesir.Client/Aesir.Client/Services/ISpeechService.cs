using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// NOTE: need to get implementation of this that works with Jetson.  There are lots of options.
/// Like Jetson Voice.
/// </summary>
public interface ISpeechService
{
    Task SpeakAsync(string text);
}
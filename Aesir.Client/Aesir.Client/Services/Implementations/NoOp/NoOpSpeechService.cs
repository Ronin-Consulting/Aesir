using System.Threading.Tasks;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpSpeechService : ISpeechService
{
    public Task SpeakAsync(string text)
    {
        return Task.CompletedTask;
    }
}
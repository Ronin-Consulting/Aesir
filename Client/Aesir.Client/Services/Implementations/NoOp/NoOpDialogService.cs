using System.Threading.Tasks;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpDialogService : IDialogService
{
    public Task<string> ShowInputDialogAsync(string title, string inputValue, string label = "Value", string defaultValue = "")
    {
        return Task.FromResult(inputValue);
    }

    public Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        return Task.FromResult(true);
    }

    public Task ShowErrorDialogAsync(string title, string message)
    {
        return Task.CompletedTask;
    }
}
using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IDialogService
{
    Task<string> ShowInputDialogAsync(string title, string inputValue, string label = "Value", string defaultValue = "");
    Task<bool> ShowConfirmationDialogAsync(string title, string message);

    Task ShowErrorDialogAsync(string title, string message);
}
using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IDialogService
{
    Task<string> ShowInputDialogAsync(string title, string message, string defaultValue = "");
    Task<bool> ShowConfirmationDialogAsync(string title, string message);
}
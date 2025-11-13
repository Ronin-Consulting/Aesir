using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// <summary>
/// Provides a service for displaying various types of dialog boxes to the user,
/// such as input dialogs, confirmation dialogs, and error dialogs.
/// </summary>
public interface IDialogService
{
    /// Displays an input dialog to the user with a specified title, input placeholder,
    /// label, and default value.
    /// <param name="title">The title of the dialog displayed to the user.</param>
    /// <param name="inputValue">The initial value to be shown in the input field.</param>
    /// <param name="label">The text label displayed next to the input field. Defaults to "Value".</param>
    /// <param name="defaultValue">The value returned when the dialog is canceled or no input is provided. Defaults to an empty string.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the user input as a string, or the default value if no input was provided or the dialog was canceled.
    Task<string> ShowInputDialogAsync(string title, string inputValue, string label = "Value", string defaultValue = "");

    /// Asynchronously displays a confirmation dialog with the specified title and message.
    /// Offers options for the user to confirm or decline the action.
    /// <param name="title">The title of the confirmation dialog.</param>
    /// <param name="message">The message or question to display within the dialog.</param>
    /// <return>A task that resolves to a boolean value indicating whether the user confirmed (true) or declined (false) the action.</return>
    Task<bool> ShowConfirmationDialogAsync(string title, string message);

    /// <summary>
    /// Displays an error dialog with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the error dialog.</param>
    /// <param name="message">The message displayed in the error dialog.</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    Task ShowErrorDialogAsync(string title, string message);
}
namespace Aesir.Client.Services;

/// <summary>
/// Defines a contract for displaying notifications of various types to users.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Displays a success notification with the provided title and message.
    /// </summary>
    /// <param name="title">The title of the success notification.</param>
    /// <param name="message">The message content of the success notification.</param>
    void ShowSuccessNotification(string title, string message);

    /// <summary>
    /// Displays an information notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the information notification.</param>
    /// <param name="message">The message content of the information notification.</param>
    void ShowInformationNotification(string title, string message);

    /// <summary>
    /// Displays a warning notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the warning notification.</param>
    /// <param name="message">The message content of the warning notification.</param>
    void ShowWarningNotification(string title, string message);

    /// <summary>
    /// Displays an error notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the error notification.</param>
    /// <param name="message">The message content of the error notification.</param>
    void ShowErrorNotification(string title, string message);
}
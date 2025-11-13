using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Provides functionality to display various types of notifications to users.
/// </summary>
public class NotificationService : INotificationService
{
    /// <summary>
    /// Displays a success notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the notification to display.</param>
    /// <param name="message">The message content of the notification.</param>
    public void ShowSuccessNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Success);
    }

    /// <summary>
    /// Displays an information notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    public void ShowInformationNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Information);
    }

    /// <summary>
    /// Displays a warning notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the warning notification.</param>
    /// <param name="message">The message content of the warning notification.</param>
    public void ShowWarningNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Warning);
    }

    /// <summary>
    /// Displays an error notification with the specified title and message.
    /// </summary>
    /// <param name="title">The title of the error notification.</param>
    /// <param name="message">The message content of the error notification.</param>
    public void ShowErrorNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Error);
    }


    /// Retrieves an instance of the notification manager associated with the application's lifetime.
    /// Depending on the application lifetime, this method retrieves the appropriate notification manager,
    /// configures it for the desired notification position, and returns the manager. If no valid notification
    /// manager can be identified or created, null is returned.
    /// <returns>
    /// A configured instance of WindowNotificationManager for managing notifications, or null if no such
    /// instance is available for the current application lifetime.
    /// </returns>
    private WindowNotificationManager? GetNotificationManager()
    {
        var lifetime = Application.Current?.ApplicationLifetime;

        WindowNotificationManager? manager;
        switch (lifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
            {
                var mainWindow = desktop.MainWindow;     // or desktop.Windows for all
                WindowNotificationManager.TryGetNotificationManager(mainWindow, out manager);
                if (manager == null) return null;
                manager.Position = NotificationPosition.TopCenter;
                return manager;
            }
            case ISingleViewApplicationLifetime singleView:
            {
                var mainView = singleView.MainView;      // mobile / single-view hosts
                var topLevel = TopLevel.GetTopLevel(mainView);
                WindowNotificationManager.TryGetNotificationManager(topLevel, out manager);
                manager ??= new WindowNotificationManager(topLevel);
                manager.Position = NotificationPosition.TopCenter;
                return manager;
            }
            default:
                return null;
        }
    }
}
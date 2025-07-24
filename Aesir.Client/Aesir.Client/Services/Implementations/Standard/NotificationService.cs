using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;

namespace Aesir.Client.Services.Implementations.Standard;

using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

public class NotificationService : INotificationService
{   
    public void ShowSuccessNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Success);
    }

    public void ShowInformationNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Information);
    }

    public void ShowWarningNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Warning);
    }

    public void ShowErrorNotification(string title, string message)
    {
        GetNotificationManager()?.Show(
            new Notification(title, message),
            showIcon: true,
            showClose: true,
            type: NotificationType.Error);
    }
    
                
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
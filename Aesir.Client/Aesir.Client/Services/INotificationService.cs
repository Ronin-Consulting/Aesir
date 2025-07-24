namespace Aesir.Client.Services;

public interface INotificationService
{
    void ShowSuccessNotification(string title, string message);
    
    void ShowInformationNotification(string title, string message);
    
    void ShowWarningNotification(string title, string message);
    
    void ShowErrorNotification(string title, string message);
}
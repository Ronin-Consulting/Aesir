using System;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpNotificationService : INotificationService
{
    public void ShowSuccessNotification(string title, string message)
    {
        Console.WriteLine($"Success: {title}: {message}");
    }

    public void ShowInformationNotification(string title, string message)
    {
        Console.WriteLine($"Information: {title}: {message}");
    }

    public void ShowWarningNotification(string title, string message)
    {
        Console.WriteLine($"Warning: {title}: {message}");
    }

    public void ShowErrorNotification(string title, string message)
    {
        Console.WriteLine($"Error: {title}: {message}");
    }
}
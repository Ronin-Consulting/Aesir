using Aesir.Client.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.Services.Implementations.Standard;

public class NavigationService : INavigationService
{
    public void NavigateToChat()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Chat));
    }

    public void NavigateToTools()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Tools));
    }

    public void NavigateToAgents()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Agents));
    }
}
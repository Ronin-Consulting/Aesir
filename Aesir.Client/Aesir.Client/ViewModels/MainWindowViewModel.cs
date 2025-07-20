using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<NavigationMessage>
{
    [ObservableProperty] private ObservableRecipient _currentViewModel;

    public MainWindowViewModel()
    {
        CurrentViewModel = Ioc.Default.GetService<MainViewViewModel>()!;
        CurrentViewModel.IsActive = true;
        
        // Register for navigation messages
        IsActive = true;
    }

    public void Receive(NavigationMessage message)
    {
        NavigateToView(message.View);
    }

    private void NavigateToView(NavigationMessage.ViewType viewName)
    {
        // Deactivate current view model
        CurrentViewModel.IsActive = false;

        // Navigate to the requested view
        CurrentViewModel = viewName switch
        {
            NavigationMessage.ViewType.Chat => Ioc.Default.GetService<MainViewViewModel>()!,
            NavigationMessage.ViewType.Tools => Ioc.Default.GetService<ToolsViewViewModel>()!,
            NavigationMessage.ViewType.Agents => Ioc.Default.GetService<AgentsViewViewModel>()!,
            _ => CurrentViewModel
        };

        // Activate the new view model
        CurrentViewModel.IsActive = true;
    }
}
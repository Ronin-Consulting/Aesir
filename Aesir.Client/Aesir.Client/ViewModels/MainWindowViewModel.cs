using Aesir.Client.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the view model for the main application window, responsible for managing
/// the current active view and handling navigation commands between different views.
/// </summary>
/// <remarks>
/// This view model acts as a central mediator for the main window's state and navigation
/// logic, receiving navigation messages and updating the currently displayed view. It
/// activates the default view model upon initialization and ensures that views are properly
/// switched based on received commands.
/// </remarks>
public partial class MainWindowViewModel : ObservableRecipient, IRecipient<NavigationMessage>
{
    /// <summary>
    /// Represents the current ViewModel instance being displayed and managed in the context
    /// of the MainWindowViewModel. This property is updated to reflect navigation changes
    /// as dictated by incoming <see cref="NavigationMessage"/> messages.
    /// </summary>
    [ObservableProperty] private ObservableRecipient _currentViewModel = null!;

    /// <summary>
    /// Called when the MainWindowViewModel is activated. This method initializes the current view model
    /// to the instance of ChatViewViewModel retrieved from the dependency injection container
    /// and sets its activation state to active.
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();
        
        CurrentViewModel = Ioc.Default.GetService<ChatViewViewModel>()!;
        CurrentViewModel.IsActive = true;
    }

    /// <summary>
    /// Handles received navigation messages and updates the current view model
    /// to the view specified in the message.
    /// </summary>
    /// <param name="message">
    /// The navigation message containing details about the target view to navigate to.
    /// </param>
    public void Receive(NavigationMessage message)
    {
        NavigateToView(message.View);
    }

    /// <summary>
    /// Navigates to the specified view and updates the current active view model accordingly.
    /// </summary>
    /// <param name="viewName">
    /// The view to navigate to, specified by the <see cref="NavigationMessage.ViewType"/> enumeration.
    /// </param>
    private void NavigateToView(NavigationMessage.ViewType viewName)
    {
        // Deactivate current view model
        CurrentViewModel.IsActive = false;

        // Navigate to the requested view
        CurrentViewModel = viewName switch
        {
            NavigationMessage.ViewType.Chat => Ioc.Default.GetService<ChatViewViewModel>()!,
            NavigationMessage.ViewType.Tools => Ioc.Default.GetService<ToolsViewViewModel>()!,
            NavigationMessage.ViewType.Agents => Ioc.Default.GetService<AgentsViewViewModel>()!,
            _ => CurrentViewModel
        };

        // Activate the new view model
        CurrentViewModel.IsActive = true;
    }
}
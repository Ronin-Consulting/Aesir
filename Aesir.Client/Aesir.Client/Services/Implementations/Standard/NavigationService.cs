using Aesir.Client.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// A service responsible for handling navigation within the application.
/// Implements the <see cref="INavigationService"/> interface.
/// </summary>
public class NavigationService : INavigationService
{
    /// <summary>
    /// Navigates the application to the Chat view.
    /// </summary>
    /// <remarks>
    /// This method sends a navigation message to direct the application
    /// to display the Chat interface. It uses the <see cref="WeakReferenceMessenger"/>
    /// to broadcast a <see cref="NavigationMessage"/> with the target view set to <see cref="NavigationMessage.ViewType.Chat"/>.
    /// </remarks>
    public void NavigateToChat()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Chat));
    }

    /// <summary>
    /// Navigates the application to the MCP Servers view.
    /// Sends a <see cref="NavigationMessage"/> with the ViewType set to McpServers
    /// using the application's messaging system.
    /// </summary>
    public void NavigateToMcpServers()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.McpServers));
    }

    /// <summary>
    /// Navigates the application to the Tools view.
    /// Sends a <see cref="NavigationMessage"/> with the ViewType set to Tools
    /// using the application's messaging system.
    /// </summary>
    public void NavigateToTools()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Tools));
    }

    /// <summary>
    /// Navigates the user to the Agents section within the application.
    /// </summary>
    /// <remarks>
    /// This method sends a <see cref="NavigationMessage"/> with the view type set to <see cref="NavigationMessage.ViewType.Agents"/>.
    /// It utilizes the messaging infrastructure to handle navigation to the Agents view.
    /// </remarks>
    public void NavigateToAgents()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.Agents));
    }

    /// <summary>
    /// Navigates the user to the Inference Engines section within the application.
    /// </summary>
    /// <remarks>
    /// This method sends a <see cref="NavigationMessage"/> with the view type set to <see cref="NavigationMessage.ViewType.InferenceEngines"/>.
    /// It utilizes the messaging infrastructure to handle navigation to the Inference Engines view.
    /// </remarks>
    public void NavigateToInferenceEngines()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.InferenceEngines));
    }

    /// <summary>
    /// Navigates the application to the Hands-Free view by sending a navigation message.
    /// </summary>
    public void NavigateToHandsFree()
    {
        WeakReferenceMessenger.Default.Send(new NavigationMessage(NavigationMessage.ViewType.HandsFree));
    }
}
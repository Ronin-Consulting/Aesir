using Aesir.Client.Messages;

namespace Aesir.Client.Services;

/// <summary>
/// Defines navigation operations for directing the application to various views or features.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates the application to the Chat view.
    /// </summary>
    /// <remarks>
    /// This method is responsible for directing the application to display the Chat interface.
    /// It leverages a messaging framework to broadcast the navigation intent.
    /// Depending on the implementation, the actual process may vary; for example,
    /// some implementations might use the WeakReferenceMessenger to send a
    /// message specifying the target view.
    /// </remarks>
    void NavigateToChat();

    /// <summary>
    /// Navigates the application to the MCP Servers view.
    /// Utilizes the application's messaging system to send a
    /// <see cref="NavigationMessage"/> with the ViewType set to MCP Servers.
    /// </summary>
    void NavigateToMcpServers();
    
    void NavigateToLogs();

    /// <summary>
    /// Navigates the application to the Documents view.
    /// Utilizes the application's messaging system to send a
    /// <see cref="NavigationMessage"/> with the ViewType set to Documents.
    /// </summary>
    void NavigateToDocuments();

    /// <summary>
    /// Navigates the application to the Tools view.
    /// Utilizes the application's messaging system to send a
    /// <see cref="NavigationMessage"/> with the ViewType set to Tools.
    /// </summary>
    void NavigateToTools();

    /// <summary>
    /// Navigates the user to the Agents section within the application.
    /// </summary>
    /// <remarks>
    /// This method is typically utilized for navigating to the Agents page or component.
    /// The exact implementation depends on the specific class handling the navigation.
    /// </remarks>
    void NavigateToAgents();
    
    /// <summary>
    /// Navigates the user to the Inference Engines section within the application.
    /// </summary>
    /// <remarks>
    /// This method is typically utilized for navigating to the Inference Engines page or component.
    /// The exact implementation depends on the specific class handling the navigation.
    /// </remarks>
    void NavigateToInferenceEngines();

    /// <summary>
    /// Navigates to the Hands-Free view.
    /// This method is responsible for transitioning the application to a dedicated Hands-Free interface.
    /// The specific implementation and behavior may vary depending on the navigation service in use.
    /// </summary>
    void NavigateToHandsFree();
}
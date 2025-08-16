namespace Aesir.Client.Messages;

/// <summary>
/// Represents a message used for navigation between different views in the application.
/// </summary>
public class NavigationMessage
{
    /// <summary>
    /// Specifies the type of views available for navigation within the application.
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// Represents the "Chat" view
        Chat,
        
        /// <summary>
        /// Represents the "MCP Servers" view
        /// </summary>
        McpServers,

        /// <summary>
        /// Represents the "Tools" view
        Tools,

        /// <summary>
        /// Represents the "Agents" view
        Agents,

        /// <summary>
        /// Represents a hands-free view type within the navigation system.
        /// This view type is intended for scenarios where interaction
        /// is
        HandsFree
    }

    /// <summary>
    /// Gets or sets the current view to navigate to, represented by the <see cref="NavigationMessage.ViewType"/> enumeration.
    /// </summary>
    /// <remarks>
    /// This property indicates the target view within the application, which can be one of the following:
    /// Chat, Tools, Agents, or HandsFree. It is used in conjunction with navigation logic to switch
    /// between these views.
    /// </remarks>
    public ViewType View { get; set; }

    /// <summary>
    /// Represents a message used for navigation purposes, carrying information about the desired view to navigate to.
    /// </summary>
    public NavigationMessage(ViewType view)
    {
        View = view;;
    }
}
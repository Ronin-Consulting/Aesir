using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Aesir.Client.ViewModels.Interfaces;

/// <summary>
/// Represents the main view model interface for the application's primary interface.
/// This interface defines the properties, commands, and methods required to manage
/// the state and behavior of the main application view.
/// </summary>
public interface IMainViewViewModel
{
    /// <summary>
    /// Gets or sets a value that indicates whether the microphone is currently active.
    /// </summary>
    /// <remarks>
    /// This property is typically used to track the microphone's state within the application,
    /// allowing the user to turn the microphone on or off.
    /// </remarks>
    bool MicOn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the panel is open.
    /// Used to track the state of a UI panel within the application.
    /// Changing this property typically reflects updates to the UI display.
    /// </summary>
    bool PanelOpen { get; set; }

    /// <summary>
    /// Indicates whether the application is currently in the process of sending a chat message
    /// or handling a file-processing operation. This property can be used to track the active
    /// state of such operations and manage UI updates or other related logic.
    /// </summary>
    bool SendingChatOrProcessingFile { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a chat message exists in the current context.
    /// </summary>
    /// <remarks>
    /// This property is used to determine the presence of a chat message. It can be useful
    /// for enabling or disabling UI elements or triggering specific logic based on whether
    /// a chat message is present. The value is typically updated in response to user interaction
    /// or programmatic changes in the application.
    /// </remarks>
    bool HasChatMessage { get; set; }

    /// <summary>
    /// Indicates whether a conversation has been initiated.
    /// This property determines if a chat session is actively ongoing
    /// or has been started in the current context.
    /// </summary>
    bool ConversationStarted { get; set; }

    /// <summary>
    /// Gets or sets the name of the selected model.
    /// This property represents the model currently selected within the context
    /// of the main view, enabling functionalities that are dependent on the
    /// active model.
    /// </summary>
    string? SelectedModelName { get; set; }

    /// <summary>
    /// Gets or sets the chat message associated with the current conversation context.
    /// This property represents the input text entered by the user for communication
    /// or commands within the chat interface. It is expected to be updated when the user
    /// provides input or as part of the message interaction flow.
    /// </summary>
    string? ChatMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message associated with the application's current state or operation.
    /// This property is typically used to display relevant error information to the user,
    /// often in cases where an operation fails or encounters an issue.
    /// </summary>
    string? ErrorMessage { get; set; }

    /// <summary>
    /// Represents the collection of messages within a conversation.
    /// This property is used to store and manage the message data for the current chat session.
    /// </summary>
    /// <remarks>
    /// The collection is of type ObservableCollection, allowing dynamic updates to the message list,
    /// while providing notifications to the UI when changes occur. Each message is represented
    /// as a MessageViewModel, encapsulating the message's content, role, and other associated metadata.
    /// </remarks>
    ObservableCollection<MessageViewModel?> ConversationMessages { get; }

    /// <summary>
    /// Gets the command used to toggle the visibility of the chat history panel.
    /// </summary>
    /// <remarks>
    /// This command is intended to handle actions related to showing or hiding the chat history
    /// in the user interface. It is often bound to a UI element for toggling the chat history.
    /// </remarks>
    ICommand ToggleChatHistory { get; }

    /// <summary>
    /// Represents a command that toggles the functionality for starting a new chat.
    /// The implementation of the command is expected to handle the logic for initiating
    /// or preparing a new chat session.
    /// </summary>
    ICommand ToggleNewChat { get; }

    /// <summary>
    /// Represents a command to toggle the microphone state in the application's main view model.
    /// This command is typically used to enable or disable the microphone functionality during
    /// interaction, such as in a voice-based conversation or recording.
    /// </summary>
    ICommand ToggleMicrophone { get; }

    /// Sends a chat message asynchronously.
    /// This method handles the process of sending a message to a conversation.
    /// It takes appropriate actions to transmit the message and update the
    /// conversation state accordingly.
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendMessageAsync();

    /// Asynchronously displays a file selection dialog to the user, allowing them to select a file.
    /// This method is designed to handle interactions related to file selection within the application's user interface.
    /// <returns>
    /// A task that represents the asynchronous operation of showing the file selection dialog.
    /// </returns>
    Task ShowFileSelectionAsync();
}
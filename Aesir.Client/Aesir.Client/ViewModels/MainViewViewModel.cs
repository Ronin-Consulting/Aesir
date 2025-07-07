using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel for the main view that handles interaction logic, state management,
/// and communication with services for chat, file upload, and other related functionalities.
/// </summary>
/// <remarks>
/// This class integrates with MVVM principles and uses CommunityToolkit.Mvvm for state management and messaging.
/// It manages user interactions such as sending chat messages, toggling the microphone, handling chat history, and selecting files for upload.
/// </remarks>
public partial class MainViewViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>,
    IRecipient<FileUploadStatusMessage>, IRecipient<RegenerateMessageMessage>, IDisposable
{
    /// <summary>
    /// Indicates the current state of the microphone in the application.
    /// When true, the microphone is active or enabled; when false, it is inactive or disabled.
    /// Used for toggling microphone functionality within the ViewModel.
    /// </summary>
    [ObservableProperty] private bool _micOn;

    /// <summary>
    /// Indicates whether the panel is currently open or closed.
    /// </summary>
    [ObservableProperty] private bool _panelOpen;

    /// <summary>
    /// Represents a private boolean field indicating whether the application is currently
    /// sending a chat message or processing a file upload. This is typically used to manage
    /// UI states or operational logic during such activities.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private bool _sendingChatOrProcessingFile;

    /// <summary>
    /// Indicates whether there is a chat message currently present.
    /// Used to determine the state of chat interactions in the application.
    /// </summary>
    [ObservableProperty] private bool _hasChatMessage;

    /// <summary>
    /// Indicates whether a conversation has been started in the chat session.
    /// This variable is primarily used to manage the state of the chat application,
    /// enabling or disabling certain UI elements or commands based on
    /// whether a conversation is currently active.
    /// </summary>
    [ObservableProperty] private bool _conversationStarted;

    /// <summary>
    /// Represents the name of the model currently selected in the application.
    /// This value may be updated when a user selects or toggles to a different model.
    /// Defaults to "Select a model".
    /// </summary>
    [ObservableProperty] private string? _selectedModelName = "Select a model";

    /// <summary>
    /// Represents the currently selected file within the context of the MainViewViewModel.
    /// This is a bindable property used to reflect or manage the file selection state in the user interface.
    /// </summary>
    [ObservableProperty] private FileToUploadViewModel? _selectedFile;

    /// <summary>
    /// Represents whether the selected file functionality is enabled or disabled within the application.
    /// </summary>
    [ObservableProperty] private bool _selectedFileEnabled = true;

    /// <summary>
    /// Stores the error message that can be displayed to the user or logged for debugging purposes.
    /// </summary>
    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// Represents the private backing field for the <see cref="ChatMessage"/> property.
    /// Stores the current chat message content.
    /// </summary>
    private string? _chatMessage;

    /// <summary>
    /// Represents the message content to be sent or interacted with in the chat interface.
    /// </summary>
    /// <remarks>
    /// This property is bound to the chat input field in the user interface and is updated dynamically as
    /// the user types their input. Changes in this property also trigger a state update that determines
    /// whether a message exists or not.
    /// </remarks>
    /// <value>
    /// A nullable string representing the current chat message input. If the user input is empty or null,
    /// the associated flag for message existence is updated accordingly.
    /// </value>
    public string? ChatMessage
    {
        get => _chatMessage;
        set
        {
            SetProperty(ref _chatMessage, value);
            HasChatMessage = !string.IsNullOrWhiteSpace(value);
        }
    }

    /// A collection that holds the messages of a conversation.
    /// This property is an observable collection that is updated dynamically
    /// as messages are added or modified during a chat session. Instances
    /// of the collection contain messages represented by the `MessageViewModel`
    /// type or its derived types.
    /// The collection is used to maintain the state of a conversation and to
    /// synchronize updates to the user interface.
    /// Messages in this collection can be populated or modified by various
    /// methods such as sending new messages, regenerating existing messages,
    /// or refreshing the conversation.
    public ObservableCollection<MessageViewModel?> ConversationMessages { get; } = [];

    /// <summary>
    /// Command to toggle the visibility of the chat history panel.
    /// </summary>
    /// <remarks>
    /// This property is bound to the UI for toggling the chat history sidebar.
    /// Executing this command changes the state of the panel between open and closed.
    /// </remarks>
    public ICommand ToggleChatHistory { get; }

    /// Represents a command that toggles the creation of a new chat session.
    /// This property is used to bind functionality for starting a new chat,
    /// typically invoked through UI elements such as buttons.
    public ICommand ToggleNewChat { get; }

    /// Represents a command that toggles the microphone state.
    /// This property is bound to the UI and is typically used to enable or disable
    /// the microphone functionality by executing the associated logic.
    public ICommand ToggleMicrophone { get; }

    /// Gets the command that is triggered to send a chat message asynchronously.
    /// This command is bound to the "SendMessageButton" in the view and executes
    /// the `SendMessageAsync` method within the `MainViewViewModel`. It handles the process
    /// of sending chat messages while managing input validation and exceptions.
    /// The command implementation uses an asynchronous relay command, ensuring that
    /// the UI remains responsive during the execution of the send operation.
    /// Usage Context:
    /// - Should be triggered when the user attempts to send a message via the interface.
    /// - Responsible for processing the current chat message stored in the `ChatMessage` property.
    /// - Clears the input field (`ChatMessage`) upon execution.
    /// - Handles error scenarios such as invalid input or model selection.
    /// Dependencies:
    /// - Relies on the `SelectedModelName` property to ensure that a valid model is selected.
    /// - Utilizes exception handling to manage specific exceptions (e.g., ArgumentException, InvalidOperationException)
    /// and notify the user of errors.
    public IAsyncRelayCommand SendMessageCommand => _sendMessageCommand ??= new AsyncRelayCommand(SendMessageAsync);

    /// Represents an asynchronous command used to trigger the file selection dialog in the application.
    /// When executed, this command invokes the logic responsible for displaying the file selection interface.
    /// This command is bound to a UI element to enable user interaction for selecting a file.
    /// The command is initialized as an instance of AsyncRelayCommand and is backed by the private
    /// field `_showFileSelectionCommand`. Its asynchronous behavior ensures that the UI remains responsive
    /// while the file selection process takes place.
    /// The primary method executed by this command is `ShowFileSelectionAsync`, which encapsulates the logic
    /// for handling file selection. The command is particularly useful in scenarios requiring file uploads
    /// or related actions in the application.
    public IAsyncRelayCommand ShowFileSelectionCommand =>
        _showFileSelectionCommand ??= new AsyncRelayCommand(ShowFileSelectionAsync);

    /// <summary>
    /// Represents the asynchronous command responsible for sending a chat message.
    /// Executes the <c>SendMessageAsync</c> method to process and send the user's message.
    /// </summary>
    private IAsyncRelayCommand? _sendMessageCommand;

    /// <summary>
    /// Represents a private instance of an asynchronous command that triggers the file selection functionality
    /// within the application. If the command instance is not already initialized, it creates a new instance
    /// bound to the <c>ShowFileSelectionAsync</c> method. This ensures lazy initialization and optimal resource usage.
    /// </summary>
    private IAsyncRelayCommand? _showFileSelectionCommand;

    // Services
    /// <summary>
    /// Represents the backing field for the application state dependency.
    /// This variable holds an instance of the <see cref="ApplicationState"/> class,
    /// which is used to store and manage the application's shared state across different components.
    /// </summary>
    private readonly ApplicationState _appState;

    /// <summary>
    /// Provides speech-related functionalities, such as converting text to speech, for the application.
    /// This variable holds the reference to an implementation of the <see cref="ISpeechService"/> interface.
    /// It is used throughout the MainViewViewModel to handle speech-related actions, including speaking text aloud
    /// and controlling other speech-dependent operations.
    /// </summary>
    private readonly ISpeechService? _speechService;

    /// <summary>
    /// Manages interactions with the chat session, including loading sessions,
    /// processing chat requests, and maintaining the state related to the conversation flow.
    /// </summary>
    private readonly IChatSessionManager _chatSessionManager;

    /// <summary>
    /// Represents the logger instance used for logging messages within the <see cref="MainViewViewModel"/> class.
    /// This logger is utilized to log informational messages, warnings, or errors that occur during
    /// various operations such as starting a new chat, toggling the microphone, or loading selected models.
    /// </summary>
    private readonly ILogger<MainViewViewModel> _logger;

    /// Represents the view model for the main view of the application.
    /// Manages the state, commands, and interactions for key functionalities such as chat history, new chat creation, and microphone control.
    public MainViewViewModel(
        ApplicationState appState,
        ISpeechService speechService,
        IChatSessionManager chatSessionManager,
        ILogger<MainViewViewModel> logger,
        FileToUploadViewModel fileToUploadViewModel)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _speechService = speechService;
        _chatSessionManager = chatSessionManager ?? throw new ArgumentNullException(nameof(chatSessionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ToggleChatHistory = new RelayCommand(() => PanelOpen = !PanelOpen);
        ToggleNewChat = new RelayCommand(ExecuteNewChat);
        ToggleMicrophone = new RelayCommand(ExecuteToggleMicrophone);

        SelectedFile = fileToUploadViewModel ?? throw new ArgumentNullException(nameof(fileToUploadViewModel));
        SelectedFile.IsActive = true;
    }

    /// <summary>
    /// Resets the current chat session by clearing the selected chat session ID
    /// in the application state and resetting any error messages.
    /// </summary>
    /// <remarks>
    /// This method is typically bound to a command that initiates a new chat session,
    /// allowing the user to start fresh without any previously selected chat session.
    /// If an error occurs during execution, an appropriate error message is logged and displayed.
    /// </remarks>
    /// <exception cref="Exception">
    /// Captures and logs any exceptions that occur during the process of starting a new chat session.
    /// </exception>
    private void ExecuteNewChat()
    {
        try
        {
            _appState.SelectedChatSessionId = null;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new chat");
            ErrorMessage = "Failed to start new chat. Please try again.";
        }
    }

    /// <summary>
    /// Toggles the state of the microphone. This method enables or disables the microphone and provides
    /// feedback to the user through error messages or confirmation messages.
    /// </summary>
    /// <remarks>
    /// If the microphone is turned on and the speech service is unavailable, the microphone will be turned off,
    /// and an error message will be logged and displayed. This method also interacts with the speech service to
    /// provide audible feedback when the microphone is activated.
    /// </remarks>
    /// <exception cref="Exception">
    /// An exception is logged if there is an error while toggling the microphone.
    /// </exception>
    private async void ExecuteToggleMicrophone()
    {
        try
        {
            MicOn = !MicOn;
            ErrorMessage = null;

            if (MicOn)
            {
                if (_speechService == null)
                {
                    _logger.LogWarning("Speech service is not available");
                    ErrorMessage = "Speech recognition is not available on this platform.";
                    MicOn = false;
                    return;
                }

                await _speechService.SpeakAsync("Aesir is listening.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle microphone");
            ErrorMessage = "Failed to toggle microphone. Please try again.";
            MicOn = false;
        }
    }

    /// <summary>
    /// Executes when the ViewModel is activated. Performs initialization logic by loading application state asynchronously.
    /// This includes invoking methods to load the selected model and the chat session.
    /// </summary>
    protected override async void OnActivated()
    {
        base.OnActivated();

        await LoadApplicationStateAsync();
    }

    /// <summary>
    /// Handles property changes on ApplicationState related to the selected chat session ID.
    /// </summary>
    /// <param name="message">
    /// A message indicating a change in a property, containing the name of the property and its new value.
    /// </param>
    /// <remarks>
    /// This method checks whether the currently loaded chat session matches the new selected chat session ID.
    /// If they do not match, it triggers the asynchronous loading of the correct chat session.
    /// </remarks>
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if (message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            if (_appState.ChatSession?.Id != _appState.SelectedChatSessionId)
                Dispatcher.UIThread.InvokeAsync(async () => await LoadChatSessionAsync());
        }
    }

    /// <summary>
    /// Handles the received <see cref="FileUploadStatusMessage"/> and updates the state relevant to file upload processing.
    /// </summary>
    /// <param name="message">The <see cref="FileUploadStatusMessage"/> instance containing the information about the file upload status and properties.</param>
    public void Receive(FileUploadStatusMessage message)
    {
        SelectedFileEnabled = !message.IsProcessing;
        SendingChatOrProcessingFile = message.IsProcessing;
    }

    /// Handles a RegenerateMessageMessage by processing it based on the type of message provided.
    /// <param name="message">
    /// An instance of RegenerateMessageMessage containing the message to be processed.
    /// Depending on the type of the message contained in the message parameter, it will either
    /// regenerate the user message or process an assistant message.
    /// </param>
    public void Receive(RegenerateMessageMessage message)
    {
        var messageViewModel = message.Value;

        switch (messageViewModel)
        {
            case UserMessageViewModel userMessage:
                _ = RegenerateMessageAsync(userMessage);
                break;
            case AssistantMessageViewModel assistantMessage:
                _ = RegenerateFromAssistantMessageAsync(assistantMessage);
                break;
        }
    }

    /// Asynchronously loads the application state, including the selected model and chat session.
    /// This method is executed on activation and is responsible for initializing the necessary
    /// state information required for the application's functionality.
    /// <returns>
    /// A task that represents the asynchronous load operation.
    /// </returns>
    private async Task LoadApplicationStateAsync()
    {
        await LoadSelectedModelAsync();
        await LoadChatSessionAsync();
    }

    /// Asynchronously loads the selected model's data into the application state.
    /// Updates the `SelectedModelName` and handles any errors that occur during the process.
    /// If successful, the selected model's name is displayed. In case of an error, an appropriate error message is set.
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadSelectedModelAsync()
    {
        try
        {
            if (_appState.SelectedModel == null)
            {
                await _appState.LoadAvailableModelsAsync();
                _appState.SelectedModel = _appState.AvailableModels.FirstOrDefault(m=> m.IsChatModel);
            }

            SelectedModelName = _appState.SelectedModel?.Id ?? "No model available";
            ErrorMessage = null;

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models");
            ErrorMessage = "Failed to load available models. Please check your connection.";
            SelectedModelName = "Error loading models";
        }
    }

    /// Loads the current chat session asynchronously.
    /// This method retrieves the chat session data using the chat session manager
    /// and updates the conversation messages. Handles any exceptions that occur
    /// during the process by logging the error and setting an appropriate error message.
    /// <returns>
    /// A Task that represents the asynchronous operation.
    /// </returns>
    private async Task LoadChatSessionAsync()
    {
        try
        {
            await _chatSessionManager.LoadChatSessionAsync();
            ConversationStarted = _appState.SelectedChatSessionId != null;
            await RefreshConversationMessagesAsync();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat session");
            ErrorMessage = "Failed to load chat session. Please try again.";
        }
    }

    /// Asynchronously refreshes and updates the list of conversation messages.
    /// Clears the current collection of conversation messages and processes messages in batches to prevent UI freezing.
    /// Exceptions encountered during this process are logged and an error message is displayed to the user.
    /// <returns>
    /// A task that represents the asynchronous operation of refreshing conversation messages.
    /// </returns>
    private async Task RefreshConversationMessagesAsync()
    {
        try
        {
            ConversationMessages.Clear();

            if (_appState.ChatSession != null)
            {
                const int batchSize = 10;
                var messages = _appState.ChatSession.GetMessages().ToList();

                // Process messages in batches to avoid UI blocking
                for (var i = 0; i < messages.Count; i += batchSize)
                {
                    var batch = messages.Skip(i).Take(batchSize).ToList();

                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        foreach (var message in batch)
                        {
                            await AddMessageToConversationAsync(message);
                        }
                    });

                    // Allow UI thread to breathe between batches
                    if (i + batchSize < messages.Count)
                    {
                        await Task.Delay(10); // Slightly longer delay to prevent UI freezing
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh conversation messages");
            ErrorMessage = "Failed to load conversation messages.";
        }
    }

    /// <summary>
    /// Asynchronously adds a message to the conversation and associates it with the appropriate message view model
    /// based on the role of the message (user, assistant, or system).
    /// </summary>
    /// <param name="message">The chat message to be added to the conversation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task AddMessageToConversationAsync(AesirChatMessage message)
    {
        MessageViewModel? messageViewModel = null;

        switch (message.Role)
        {
            case "user":
                messageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
                if (messageViewModel != null)
                    await messageViewModel.SetMessage(message);
                break;

            case "assistant":
                messageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
                if (messageViewModel != null)
                    await messageViewModel.SetMessage(message);
                break;

            case "system":
                messageViewModel = Ioc.Default.GetService<SystemMessageViewModel>();
                if (messageViewModel != null)
                    // always reset the system message
                    await messageViewModel.SetMessage(AesirChatMessage.NewSystemMessage());
                break;
        }

        if (messageViewModel != null)
        {
            ConversationMessages.Add(messageViewModel);
        }
    }

    /// Sends a chat message asynchronously.
    /// This method validates the input message and ensures that a model is selected
    /// before attempting to send the message. It updates the conversation messages
    /// with the user input and placeholder for assistant response, processes the
    /// chat request, and handles any errors during the operation.
    /// <returns>A Task that represents the asynchronous operation.</returns>
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedModelName) || SelectedModelName == "Select a model")
        {
            ErrorMessage = "Please select a model before sending a message.";
            return;
        }

        var currentMessage = ChatMessage;
        ChatMessage = null;
        ErrorMessage = null;

        try
        {
            ConversationStarted = true;
            SendingChatOrProcessingFile = true;

            // Add user message to UI
            var userMessage = AesirChatMessage.NewUserMessage(currentMessage);

            if (SelectedFile?.IsVisible == true)
            {
                userMessage.AddFile(SelectedFile.FileName);
                SelectedFile.ClearFile();
            }

            await AddMessageToConversationAsync(userMessage);

            // Add placeholder for assistant response
            var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
            if (assistantMessageViewModel == null)
            {
                throw new InvalidOperationException("Could not resolve AssistantMessageViewModel");
            }

            ConversationMessages.Add(assistantMessageViewModel);

            // Process the chat request
            await _chatSessionManager.ProcessChatRequestAsync(SelectedModelName, ConversationMessages);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when sending message");
            ErrorMessage = "Invalid input. Please check your message and try again.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when sending message");
            ErrorMessage = "Unable to send message. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message");
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            SendingChatOrProcessingFile = false;
        }
    }

    /// Asynchronously opens a file selection dialog for the user to select a PDF file.
    /// If a file is selected, it sets the conversation ID for the selected file and initiates a file upload process.
    /// Logs errors and updates the error message in case of failure.
    /// <return>Returns a Task representing the asynchronous operation.</return>
    private async Task ShowFileSelectionAsync()
    {
        try
        {
            var files = await OpenPdfFilePickerAsync();

            if (files.Count >= 1)
            {
                SelectedFile!.SetConversationId(_appState.ChatSession!.Conversation.Id);
                RequestFileUpload(files[0].Path.LocalPath);
                ErrorMessage = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file selection dialog");
            ErrorMessage = "Failed to open file selection dialog. Please try again.";
        }
    }

    /// Opens a PDF file picker dialog and allows the user to select a PDF file from the file system.
    /// The method uses the application's storage provider to display a file picker window where the user can
    /// browse and select PDF files. It only allows selection of files with a ".pdf" extension and the MIME type
    /// "application/pdf". If no storage provider is available, the method logs a warning and returns an empty
    /// list. Any exceptions during the process are logged, and an empty list is returned in such cases.
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list of the
    /// selected storage files. If no file is selected, or an error occurs, an empty list is returned.
    /// </returns>
    private async Task<IReadOnlyList<IStorageFile>> OpenPdfFilePickerAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(GetTopLevelControl());
            if (topLevel?.StorageProvider == null)
            {
                _logger.LogWarning("Storage provider not available");
                return Array.Empty<IStorageFile>();
            }

            return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Upload PDF",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("PDF Documents")
                    {
                        Patterns = ["*.pdf"],
                        MimeTypes = ["application/pdf"],
                        AppleUniformTypeIdentifiers = ["com.adobe.pdf"]
                    }
                ]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open PDF file picker");
            return Array.Empty<IStorageFile>();
        }
    }

    /// <summary>
    /// Requests the upload of a file to the server. Sends a file upload request message with
    /// the specified file path and associated conversation ID.
    /// </summary>
    /// <param name="filePath">The file path of the file to be uploaded. Must not be null, empty, or consist only of whitespace.</param>
    private void RequestFileUpload(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Attempted to upload file with empty path");
                ErrorMessage = "Invalid file path.";
                return;
            }

            if (_appState.ChatSession == null)
            {
                _logger.LogWarning("Attempted to upload file without an active chat session");
                ErrorMessage = "Please start a chat session before uploading files.";
                return;
            }

            WeakReferenceMessenger.Default.Send(new FileUploadRequestMessage()
            {
                ConversationId = _appState.ChatSession.Conversation.Id,
                FilePath = filePath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request file upload for path: {FilePath}", filePath);
            ErrorMessage = "Failed to upload file. Please try again.";
        }
    }

    /// Asynchronously regenerates the conversation by resending the specified user message and removing subsequent messages.
    /// This method finds the specified user message in the conversation, removes all messages following it (including
    /// the assistant response), and sends the user message again.
    /// <param name="userMessageViewModel">The instance of <see cref="UserMessageViewModel"/> representing the user message to be regenerated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RegenerateMessageAsync(UserMessageViewModel userMessageViewModel)
    {
        // Find the index of the user message in the conversation
        var messageIndex = ConversationMessages.IndexOf(userMessageViewModel);
        if (messageIndex == -1) return;

        // Remove all messages after this user message (including the assistant response)
        for (var i = ConversationMessages.Count - 1; i > messageIndex; i--)
        {
            ConversationMessages.RemoveAt(i);
        }

        // Also remove messages from the chat session
        var messagesToRemove = _appState.ChatSession!.GetMessages().Skip(messageIndex).ToList();
        foreach (var msg in messagesToRemove)
        {
            _appState.ChatSession.RemoveMessage(msg);
        }

        // Re-send the user message by simulating the send process
        await ResendUserMessage(userMessageViewModel);
    }

    /// <summary>
    /// Regenerates the assistant's message and updates the conversation history by processing related user and assistant messages.
    /// </summary>
    /// <param name="assistantMessageViewModel">The assistant message to be regenerated and used as a reference for conversation updates.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RegenerateFromAssistantMessageAsync(AssistantMessageViewModel assistantMessageViewModel)
    {
        // Find the index of the assistant message in the conversation
        var assistantIndex = ConversationMessages.IndexOf(assistantMessageViewModel);
        if (assistantIndex is -1 or 0) return;

        // Find the preceding user message
        UserMessageViewModel? userMessage = null;
        for (var i = assistantIndex - 1; i >= 0; i--)
        {
            if (ConversationMessages[i] is UserMessageViewModel user)
            {
                userMessage = user;
                break;
            }
        }

        if (userMessage == null) return;

        // Remove the assistant message and all messages after it
        for (var i = ConversationMessages.Count - 1; i >= assistantIndex; i--)
        {
            ConversationMessages.RemoveAt(i);
        }

        // Also remove messages from the chat session (from assistant message onwards)
        var messagesToRemove = _appState.ChatSession!.GetMessages().Skip(assistantIndex).ToList();
        foreach (var msg in messagesToRemove)
        {
            _appState.ChatSession.RemoveMessage(msg);
        }

        // Re-send the user message by simulating the send process
        await ResendUserMessage(userMessage);
    }

    // ReSharper disable once UnusedParameter.Local
    /// Resends a user message by simulating the message sending process.
    /// This function performs necessary pre-checks, updates chat state, and handles message processing.
    /// <param name="userMessageViewModel">The user message view model representing the message to be resent.</param>
    /// <returns>A task representing the asynchronous operation of resending the user message.</returns>
    private async Task ResendUserMessage(UserMessageViewModel userMessageViewModel)
    {
        if (string.IsNullOrWhiteSpace(SelectedModelName) || SelectedModelName == "Select a model")
        {
            ErrorMessage = "Please select a model before sending a message.";
            return;
        }

        ConversationStarted = true;
        SendingChatOrProcessingFile = true;

        // Create new assistant message view model
        var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
        if (assistantMessageViewModel == null)
        {
            throw new InvalidOperationException("Could not resolve AssistantMessageViewModel");
        }

        ConversationMessages.Add(assistantMessageViewModel);

        await _chatSessionManager.ProcessChatRequestAsync(SelectedModelName, ConversationMessages);

        SendingChatOrProcessingFile = false;
    }

    /// Releases the resources used by the MainViewViewModel instance.
    /// <param name="disposing">
    /// A boolean value indicating whether the method is being called explicitly
    /// to release both managed and unmanaged resources. If true, the method disposes
    /// managed resources; if false, only unmanaged resources are released.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Unregister messaging
                IsActive = false;

                // Dispose any managed resources if needed
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (_speechService is IDisposable disposableSpeechService)
                {
                    disposableSpeechService.Dispose();
                }

                // Clear collections to help with memory management
                ConversationMessages.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }

    /// Releases the resources used by the MainViewViewModel instance. This includes
    /// managed resources to ensure proper cleanup and reduce memory usage. Calls
    /// Dispose(bool) method to handle the managed resources, and suppresses finalization
    /// to prevent the garbage collector from calling the finalizer for this object.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// Retrieves the top-level control of the application based on the current application lifetime type.
    /// This method determines whether the application is running in a desktop or single-view application lifetime.
    /// Depending on the lifetime, it returns either the main window (for desktop applications) or the main view as a ContentControl (for single-view applications).
    /// If no lifetime matches the expected types, or an exception occurs, it returns null.
    /// <returns>
    /// The ContentControl representing the top-level control, or null if it cannot be determined.
    /// </returns>
    private ContentControl? GetTopLevelControl()
    {
        try
        {
            return Application.Current?.ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
                ISingleViewApplicationLifetime singleView => singleView.MainView as ContentControl,
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top level control");
            return null;
        }
    }
}
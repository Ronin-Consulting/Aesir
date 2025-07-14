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
/// ViewModel for managing the main application's functionality, interactions, and state transitions.
/// </summary>
/// <remarks>
/// This class implements MVVM design patterns and utilizes CommunityToolkit.Mvvm for state tracking, messaging,
/// and command execution. It facilitates operations such as chat management, speech processing, file uploads,
/// and lifecycle management while integrating with other services and view models.
/// </remarks>
public partial class MainViewViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>,
    IRecipient<FileUploadStatusMessage>, IRecipient<RegenerateMessageMessage>, IDisposable
{
    /// <summary>
    /// Represents the default value used for model selection in the user interface.
    /// This value is utilized when no specific model has been chosen by the user.
    /// Serves as an indicator for uninitialized or default states related to model selection.
    /// </summary>
    private const string DefaultModelIdValue = "Select a model";

    /// <summary>
    /// Represents the current operational status of the microphone within the application.
    /// A value of true indicates that the microphone is turned on and active, while a value of
    /// false signifies that it is turned off or inactive.
    /// This property is integral for managing microphone toggling functionality in the ViewModel.
    /// </summary>
    [ObservableProperty] private bool _micOn;

    /// <summary>
    /// Represents the state of the panel within the main view.
    /// When true, the panel is open; when false, it is closed.
    /// Used to control the visibility or expanded state of the panel in the MainViewViewModel.
    /// </summary>
    [ObservableProperty] private bool _panelOpen;

    /// <summary>
    /// Indicates whether the application is currently engaged in sending a chat message
    /// or processing a file operation. This boolean value can be used to control user interface
    /// behavior and manage application logic during these activities.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private bool _sendingChatOrProcessingFile;

    /// <summary>
    /// Represents whether a chat message is currently available or present.
    /// This property is utilized for managing the state of chat messages
    /// within the application's ViewModel.
    /// </summary>
    [ObservableProperty] private bool _hasChatMessage;

    /// <summary>
    /// Represents the state indicating if a conversation has been initiated in the chat session.
    /// Used to control application logic, such as enabling or disabling features
    /// or UI elements tied to the active conversation state.
    /// </summary>
    [ObservableProperty] private bool _conversationStarted;

    /// <summary>
    /// Stores the name of the model currently selected by the user in the application.
    /// This property is initialized with a default value of "Select a model" and
    /// updated whenever a user selects a specific model from the available options.
    /// </summary>
    [ObservableProperty] private string? _selectedModelName = DefaultModelIdValue;

    /// <summary>
    /// Stores the identifier of the currently selected model in the application.
    /// The value is updated based on user selection or default settings.
    /// Used for referencing the selected model in operations such as processing chat requests.
    /// </summary>
    private string? _selectedModelId = DefaultModelIdValue;

    /// <summary>
    /// Represents the currently selected file in the MainViewViewModel.
    /// This property is used to handle and track the file selection state
    /// within the application's user interface and functionality.
    /// </summary>
    [ObservableProperty] private FileToUploadViewModel? _selectedFile;

    /// <summary>
    /// Determines whether the file selection functionality is enabled or disabled in the application.
    /// Used to control the state of user interactions related to file selection.
    /// </summary>
    [ObservableProperty] private bool _selectedFileEnabled = true;

    /// <summary>
    /// Represents the error message encountered within the application.
    /// Used for displaying errors to the user or logging for diagnostic purposes.
    /// </summary>
    [ObservableProperty] private string? _errorMessage;

    /// <summary>
    /// Represents the current text content of the chat message in the application's ViewModel.
    /// Serves as the backing field for the <see cref="ChatMessage"/> property.
    /// Used to store user input for sending messages in the chat interface.
    /// </summary>
    private string? _chatMessage;

    /// <summary>
    /// Represents the text content of the chat message being composed or edited by the user.
    /// This property supports data binding to the user interface and is updated in real-time
    /// as the user types. It also checks if the input is non-empty to toggle related functionality.
    /// </summary>
    public string? ChatMessage
    {
        get => _chatMessage;
        set
        {
            SetProperty(ref _chatMessage, value);
            HasChatMessage = !string.IsNullOrWhiteSpace(value);
        }
    }

    /// <summary>
    /// Represents a collection of messages exchanged during a conversation.
    /// This property is an observable collection containing instances of <see cref="MessageViewModel"/> that serve as the ViewModels for individual chat messages.
    /// It is utilized to populate the UI with the conversation messages and is updated as new messages are added or removed during the session.
    /// </summary>
    public ObservableCollection<MessageViewModel?> ConversationMessages { get; } = [];

    /// <summary>
    /// Command that toggles the visibility of the chat history panel in the application.
    /// When executed, it switches the panel's state between open and closed, improving user control over the interface layout.
    /// </summary>
    public ICommand ToggleChatHistory { get; }

    /// <summary>
    /// Represents a command to initiate or toggle a new chat session in the application.
    /// Typically bound to user interface elements to trigger the creation or reset of a chat thread.
    /// </summary>
    public ICommand ToggleNewChat { get; }

    /// <summary>
    /// Represents a command used to toggle the state of the microphone in the application.
    /// When executed, it enables or disables the microphone, altering its active operational state.
    /// </summary>
    public ICommand ToggleMicrophone { get; }

    /// <summary>
    /// Command that triggers the action to send a chat message asynchronously.
    /// Used by the ViewModel to handle the process of validating, preparing, and dispatching the message to the appropriate service or layer.
    /// This command supports asynchronous operations and integrates with the UI to enable or disable based on the execution status.
    /// </summary>
    public IAsyncRelayCommand SendMessageCommand => _sendMessageCommand ??= new AsyncRelayCommand(SendMessageAsync);

    /// <summary>
    /// A command used to initiate the file selection process within the file upload functionality of the application.
    /// Triggers an asynchronous operation to display a file picker or dialog, allowing users to select a file for upload.
    /// </summary>
    public IAsyncRelayCommand ShowFileSelectionCommand =>
        _showFileSelectionCommand ??= new AsyncRelayCommand(ShowFileSelectionAsync);

    /// <summary>
    /// Stores the asynchronous command instance used to send chat messages within the application.
    /// Associated with the <c>SendMessageAsync</c> method to handle the user's input and transmit messages.
    /// Ensures actions tied to sending messages are executed asynchronously to maintain UI responsiveness.
    /// </summary>
    private IAsyncRelayCommand? _sendMessageCommand;

    /// <summary>
    /// A private, nullable instance of an asynchronous command tied to the file selection functionality
    /// of the application. This command is initialized lazily to execute the <c>ShowFileSelectionAsync</c>
    /// method, ensuring efficient memory management and delayed resource allocation.
    /// </summary>
    private IAsyncRelayCommand? _showFileSelectionCommand;

    // Services
    /// <summary>
    /// A readonly field representing an instance of the application's shared state framework,
    /// utilized to manage and maintain cohesive state across various components of the application.
    /// </summary>
    private readonly ApplicationState _appState;

    /// <summary>
    /// Handles speech-related functionalities, such as converting text to speech, within the application.
    /// This variable holds a reference to an implementation of the <see cref="ISpeechService"/> interface,
    /// enabling the ViewModel to interact with speech-related operations, including speaking aloud and managing speech interactions.
    /// </summary>
    private readonly ISpeechService? _speechService;

    /// <summary>
    /// Handles chat session management operations such as loading chat sessions,
    /// processing chat requests, and ensuring the continuity of conversation flow.
    /// Acts as a key component for coordinating interactions and maintaining
    /// the state of chat-related data within the application.
    /// </summary>
    private readonly IChatSessionManager _chatSessionManager;

    /// <summary>
    /// Represents the logger instance used for capturing and recording log messages
    /// within the context of the MainViewViewModel class. This includes logging
    /// errors, warnings, and informational messages during the execution of
    /// various operations such as handling chat functionality, toggling features,
    /// or reporting application states.
    /// </summary>
    private readonly ILogger<MainViewViewModel> _logger;

    /// Represents the view model for the main view in the application.
    /// Handles core application state and provides commands for toggling chat history, starting a new chat, and controlling the microphone.
    /// Integrates services for speech recognition, chat session management, and file upload interactions.
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

    /// Resets the current chat session, clearing the selected chat session ID and removing any error messages.
    /// This method is used to initiate a new chat session, allowing the user to start fresh.
    /// Logs and displays an error message if an exception occurs during execution.
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

    /// Toggles the microphone's active state asynchronously.
    /// Controls enabling or disabling the microphone functionality and interacts with other components like the speech service
    /// to provide necessary user feedback. Handles any errors gracefully by logging and displaying an appropriate error message.
    /// Exceptions:
    /// Throws an exception if there is an unexpected error during the process, which will be logged accordingly.
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
    /// Triggered when the ViewModel is activated.
    /// Handles any setup or initialization required at the point of activation,
    /// including asynchronous tasks such as loading application state or configuring resources.
    /// </summary>
    protected override async void OnActivated()
    {
        base.OnActivated();

        await LoadApplicationStateAsync();
    }

    /// Handles the receipt of a property change message.
    /// Updates the application state based on the current and newly selected chat session IDs.
    /// Triggers a process to asynchronously load the appropriate chat session if the IDs do not match.
    /// <param name="message">The property change message containing the property name and its new value.</param>
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if (message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            if (_appState.ChatSession?.Id != _appState.SelectedChatSessionId)
                Dispatcher.UIThread.InvokeAsync(async () => await LoadChatSessionAsync());
        }
    }

    /// Handles the received <see cref="FileUploadStatusMessage"/> instance to update the file upload processing state.
    /// <param name="message">The <see cref="FileUploadStatusMessage"/> containing the current status and processing state of the file upload.</param>
    public void Receive(FileUploadStatusMessage message)
    {
        SelectedFileEnabled = !message.IsProcessing;
        SendingChatOrProcessingFile = message.IsProcessing;
    }

    /// Processes a RegenerateMessageMessage by determining the type of the contained message
    /// and invoking the appropriate message regeneration logic.
    /// <param name="message">
    /// An instance of RegenerateMessageMessage that contains the message to be regenerated.
    /// The message can either be a user message or an assistant message, and corresponding
    /// handling logic is executed based on its type.
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

    /// Asynchronously loads the application state, including configurations and session data,
    /// essential for initializing and preparing the application for interaction.
    /// <returns>
    /// A task that represents the asynchronous operation of loading the application state.
    /// </returns>
    private async Task LoadApplicationStateAsync()
    {
        await LoadSelectedModelAsync();
        await LoadChatSessionAsync();
    }

    /// Asynchronously loads the selected model's data, updating the application state with the model's details.
    /// Manages error handling by setting appropriate error messages or updating the selected model name in case of success.
    /// <returns>A task representing the completion of the asynchronous model loading operation.</returns>
    private async Task LoadSelectedModelAsync()
    {
        try
        {
            if (_appState.SelectedModel == null)
            {
                await _appState.LoadAvailableModelsAsync();
                _appState.SelectedModel = _appState.AvailableModels.FirstOrDefault(m => m.IsChatModel);
            }

            _selectedModelId = _appState.SelectedModel?.Id ?? "No model available";
            SelectedModelName = _selectedModelId.Split(':')[0];
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
    /// Retrieves the chat session data using the chat session manager, updates the conversation messages,
    /// and manages the state of the application accordingly. Handles exceptions by logging errors and
    /// updating the error message if the operation fails.
    /// <returns>
    /// A Task that represents the asynchronous operation of loading the chat session.
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
    /// Clears the existing conversation messages, processes new messages in batches to optimize performance,
    /// and ensures the UI remains responsive during the operation.
    /// Logs errors and displays an appropriate error message to the user if the operation fails.
    /// <returns>
    /// A task that represents the ongoing asynchronous operation for refreshing conversation messages.
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

    /// Asynchronously adds a chat message to the conversation and associates it with the corresponding message view model.
    /// The association is determined by the message role (e.g., user, assistant, or system).
    /// <param name="message">The chat message object to be added to the conversation.</param>
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
    /// Validates the input message, ensures a model is selected, and handles the entire
    /// process of adding the user message to the conversation, providing a placeholder
    /// for the assistant response, and processing the chat request. Handles errors and
    /// updates relevant state properties during execution.
    /// <returns>A Task representing the asynchronous operation.</returns>
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedModelId) || _selectedModelId == DefaultModelIdValue)
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
            await _chatSessionManager.ProcessChatRequestAsync(_selectedModelId, ConversationMessages);
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

    /// Asynchronously opens a file selection dialog that allows the user to select a PDF file.
    /// If a file is selected, it associates the file with the current conversation, initiates the upload process,
    /// and handles any errors that occur during the process, including logging and updating the error state.
    /// <return>Returns a Task that represents the asynchronous operation of displaying the file selection dialog and handling the selected file.</return>
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

    /// Opens a PDF file picker dialog asynchronously, allowing the user to select PDF files from the file system.
    /// Utilizes the application's storage provider to present a file picker that filters for files with a ".pdf" extension
    /// and corresponding MIME type. In cases where the storage provider is unavailable or an error occurs during the operation,
    /// an empty list of files is returned, and the error is logged.
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a read-only list of selected storage files.
    /// Returns an empty list if no files are selected or if an error occurs during the process.
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
    /// Initiates a request to upload a file to the server.
    /// Sends a message containing the file path and associated conversation ID for processing.
    /// </summary>
    /// <param name="filePath">The full path of the file to be uploaded. Must be a non-null, non-empty string.</param>
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

    /// Asynchronously regenerates the conversation by resending a specified user message and removing all subsequent messages.
    /// This method identifies the given user message in the conversation, removes all messages that follow it (including the assistant's response),
    /// and resends the user message to restart the conversation flow.
    /// <param name="userMessageViewModel">An instance of <see cref="UserMessageViewModel"/> representing the user message to be regenerated.</param>
    /// <returns>A task that represents the asynchronous operation of regenerating the message.</returns>
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
    /// Regenerates a specified assistant message in the conversation history by removing related messages and
    /// re-sending the preceding user message to generate a new response.
    /// </summary>
    /// <param name="assistantMessageViewModel">The instance of the assistant message that needs to be regenerated.</param>
    /// <returns>A task that represents the asynchronous operation of regenerating the assistant message.</returns>
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
        if (string.IsNullOrWhiteSpace(_selectedModelId) || _selectedModelId == DefaultModelIdValue)
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

        await _chatSessionManager.ProcessChatRequestAsync(_selectedModelId, ConversationMessages);

        SendingChatOrProcessingFile = false;
    }

    /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
    /// Implements the IDisposable interface to allow resources to be released when the instance is no longer needed.
    /// <param name="disposing">
    /// A flag indicating whether to release both managed and unmanaged resources.
    /// If true, both managed and unmanaged resources are released; if false, only unmanaged resources are released.
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

    /// Releases all resources used by the instance of MainViewViewModel.
    /// This method ensures proper cleanup of managed resources, explicitly
    /// invoking the disposal process to free memory or other resources.
    /// Invokes the Dispose(bool) method and suppresses finalization to
    /// optimize garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// Retrieves the top-level control of the application based on the current application lifetime.
    /// This method evaluates the application's lifetime type to identify whether it is running in a classic desktop environment
    /// or a single-view application environment, and accordingly returns the top-level control.
    /// Logs an error and returns null if an exception occurs during execution.
    /// <returns>
    /// The ContentControl representing the top-level control if successfully retrieved, or null if it cannot be determined.
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
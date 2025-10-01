using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.FileTypes;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
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
using Humanizer;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// <summary>
/// ViewModel responsible for managing chat functionality, user interactions, and application state specific to chat operations.
/// </summary>
/// <remarks>
/// This class adheres to the MVVM pattern and integrates with the CommunityToolkit.Mvvm framework for observable properties and messaging.
/// It provides commands for sending messages, toggling panels, and managing files while coordinating with services such as navigation,
/// speech processing, and chat session management. It also handles lifecycle and resource management.
/// </remarks>
public partial class ChatViewViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>,
    IRecipient<FileUploadStatusMessage>, IRecipient<RegenerateMessageMessage>, IRecipient<FileDownloadRequestMessage>,IDisposable
{
    /// <summary>s
    /// The predefined constant value representing the default display name for an agent in the selection process.
    /// Used to indicate that no agent has been explicitly chosen, often serving as a placeholder or default state.
    /// </summary>
    private const string DefaultAgentNameValue = "Select an agent";

    [ObservableProperty] private AesirConfigurationReadinessBase _configurationReadiness = new() { IsReady = false, Reasons = [] };
    
    /// <summary>
    /// Indicates whether the panel is currently open in the ChatViewViewModel.
    /// A value of true signifies that the panel is open, while false means it is closed.
    /// This variable is used to manage the open or closed state of the panel in the user interface.
    /// </summary>
    [ObservableProperty] private bool _panelOpen;

    /// <summary>
    /// Represents the state indicating whether the application is either in the process of sending a chat message
    /// or handling a file operation. This property is intended to facilitate synchronization between application logic
    /// and user interface behaviors during these operations.
    /// </summary>
    [ObservableProperty] [NotifyPropertyChangedRecipients]
    private bool _sendingChatOrProcessingFile;

    /// <summary>
    /// Indicates whether there is an existing chat message.
    /// This variable serves as a flag to determine the presence
    /// of a chat message in the current context or session.
    /// </summary>
    [ObservableProperty] private bool _hasChatMessage;

    /// <summary>
    /// Indicates whether a conversation has been initiated in the current chat session.
    /// This property is used to manage chat-related logic, such as enabling certain controls
    /// or user interactions that depend on the active state of a conversation.
    /// </summary>
    [ObservableProperty] private bool _conversationStarted;

    /// <summary>
    /// Provides a collection of agents currently available for assignment or interaction.
    /// This property is utilized to determine the active agents that can be selected
    /// or utilized for various operations within the system.
    /// </summary>
    public ObservableCollection<AesirAgentBase> AvailableAgents { get; set; }

    /// <summary>
    /// Stores the currently selected agent within the application context.
    /// This variable is used to track and manage the agent chosen by the user or system process.
    /// It acts as a key element in operations that require agent-specific data or functionality.
    /// </summary>
    [ObservableProperty] private AesirAgentBase? _selectedAgent;

    /// <summary>
    /// Represents the name of the agent currently selected by the user within the chat interface.
    /// This field is initialized with a default placeholder value and is updated to reflect
    /// the name of the specified agent whenever a selection is made.
    /// </summary>
    [ObservableProperty] private string? _selectedAgentName = DefaultAgentNameValue;

    /// <summary>
    /// Holds a reference to the currently selected file within the application.
    /// Used to keep track of the file chosen by the user for operations such as viewing, editing, or processing.
    /// Serves as an indicator of the active file in context.
    /// </summary>
    [ObservableProperty] private FileToUploadViewModel? _selectedFile;

    /// <summary>
    /// Indicates whether the file selection functionality is activated within the application.
    /// This variable controls the enabling or disabling of file selection options,
    /// ensuring proper interaction flow based on application state or user actions.
    /// </summary>
    [ObservableProperty] private bool _selectedFileEnabled = true;

    /// <summary>
    /// Stores the raw text input representing the content of a chat message before it is sent.
    /// Managed internally within the ViewModel as the backing field for the <see cref="ChatMessage"/> property.
    /// Tracks user-typed message text and supports application logic related to chat message preparation.
    /// </summary>
    private string? _chatMessage;

    /// <summary>
    /// Represents the text content of the user's input message in the chat interface.
    /// This property is bound to the message input box in the user interface and is updated as the user types.
    /// Modifying this property also triggers logic to determine whether the message input is empty or contains text.
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
    /// Holds the collection of messages exchanged during a conversation.
    /// This property tracks the dialogue between participants, providing
    /// a detailed record of the interaction.
    /// </summary>
    public ObservableCollection<MessageViewModel?> ConversationMessages { get; } = [];
    
    [ObservableProperty]
    private ICollection<ToolRequest> _toolRequests = new HashSet<ToolRequest>();
    
    [ObservableProperty]
    private ICollection<ToolRequest> _toolsAvailable = new HashSet<ToolRequest>();

    [ObservableProperty]
    private bool _thinkingToggleVisible;
    
    [ObservableProperty]
    private ICollection<string> _thinkValues = new List<string>();
    
    [ObservableProperty]
    private string? _selectedThinkValue;
    
    public ICommand ToggleToolRequest { get; }
    
    /// <summary>
    /// Represents a command to toggle the visibility of the left panel in the user interface.
    /// This is typically used to show or hide a sidebar or navigation panel based on the user's interaction.
    /// Primarily bound to UI elements like buttons for controlling the display state of the left pane.
    /// </summary>
    public ICommand ToggleLeftPane { get; }

    /// <summary>
    /// Represents a command to initiate a new chat session within the user interface.
    /// This command is bound to actions, such as a button click, to allow users to start a new conversation.
    /// Typically toggles the necessary state or functionality to prepare the system for a new chat context.
    /// </summary>
    public ICommand ToggleNewChat { get; }

    /// <summary>
    /// Defines the currently selected agent within the application.
    /// This property is used to identify and manage interactions with the chosen agent.
    /// It determines the agent that will respond or perform actions as required by the system.
    /// </summary>
    public ICommand SelectAgent { get; }

    /// <summary>
    /// Represents a command used to enable or toggle the "Hands Free" mode within the application.
    /// Typically associated with functionalities like voice commands or other hands-free interactions.
    /// When executed, this command activates the corresponding logic to facilitate a hands-free user experience.
    /// </summary>
    public ICommand ShowHandsFree { get; }

    /// <summary>
    /// Command responsible for handling the action of sending a message.
    /// Encapsulates the logic required to process and dispatch user messages
    /// within the application, ensuring proper execution and state management
    /// during the operation.
    /// </summary>
    public ICommand SendMessageCommand { get; }

    /// <summary>
    /// Encapsulates the command functionality to display a file selection dialog to the user.
    /// This property is typically bound to the user interface elements that trigger file selection operations.
    /// Facilitates interaction to select and retrieve file paths for further processing.
    /// </summary>
    public ICommand ShowFileSelectionCommand { get; }
    
    public ICommand SelectThinkValueCommand  { get; }
    
    // Services
    /// <summary>
    /// A readonly field leveraging the application's centralized state management framework,
    /// designed to ensure synchronized and consistent state updates across multiple
    /// components and services within the application.
    /// </summary>
    private readonly ApplicationState _appState;

    /// <summary>
    /// Provides access to speech-related functionalities within the application.
    /// Represents an implementation of the <see cref="ISpeechService"/> interface,
    /// supporting operations such as text-to-speech conversion and handling of speech interactions.
    /// It enables the ViewModel to manage auditory features and integrate speech capabilities
    /// into the application's user experience.
    /// </summary>
    private readonly ISpeechService? _speechService;

    /// <summary>
    /// Manages the lifecycle and state of chat sessions within the application.
    /// Provides functionality to create, track, and control active chat sessions,
    /// ensuring proper handling of session-related operations.
    /// </summary>
    private readonly IChatSessionManager _chatSessionManager;

    /// <summary>
    /// An instance of the ILogger interface specifically configured for the ChatViewViewModel class.
    /// Utilized to log errors, warnings, and informational messages relevant to processes
    /// such as chat operations, agent management, and error handling within the view model.
    /// Helps in diagnosing issues and tracking application behavior during runtime.
    /// </summary>
    private readonly ILogger<ChatViewViewModel> _logger;

    /// <summary>
    /// Provides a navigation service used to facilitate the navigation between different views or components
    /// within the application. Enables seamless transitions and control over view management.
    /// </summary>
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Encapsulates functionality for displaying notification messages to the user.
    /// This service is used across the view model to inform users about errors, updates,
    /// or statuses regarding various operations, such as chat initialization or data loading.
    /// </summary>
    private readonly INotificationService _notificationService;

    /// <summary>
    /// The service responsible for managing document collections and providing access to their content.
    /// Utilized for operations such as retrieving document streams, handling file content, and other
    /// document-specific tasks within the ChatView context.
    /// </summary>
    private readonly IDocumentCollectionService _documentCollectionService;

    private readonly IConfigurationService _configurationService;

    /// Encapsulates the logic and state management for the chat view in the application.
    /// Coordinates user interactions such as managing chat history, initiating new conversations, and handling speech input.
    /// Collaborates with underlying services to facilitate real-time chat operations and file handling functionality.
    public ChatViewViewModel(
        ApplicationState appState,
        ISpeechService speechService,
        IChatSessionManager chatSessionManager,
        ILogger<ChatViewViewModel> logger,
        FileToUploadViewModel fileToUploadViewModel,
        INavigationService navigationService,
        INotificationService notificationService,
        IDocumentCollectionService documentCollectionService,
        IConfigurationService configurationService)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _speechService = speechService;
        _chatSessionManager = chatSessionManager ?? throw new ArgumentNullException(nameof(chatSessionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService;
        _notificationService = notificationService;
        _documentCollectionService = documentCollectionService;
        _configurationService = configurationService;

        ToggleLeftPane = new RelayCommand(() => PanelOpen = !PanelOpen);
        ToggleNewChat = new RelayCommand(ExecuteNewChat);
        SelectAgent = new AsyncRelayCommand<AesirAgentBase>(ExecuteSelectAgentAsync);
        ShowHandsFree = new RelayCommand(ExecuteShowHandsFree);
        SendMessageCommand = new AsyncRelayCommand(ExecuteSendMessageAsync);
        ShowFileSelectionCommand = new AsyncRelayCommand(ExecuteShowFileSelectionAsync);
        SelectThinkValueCommand = new RelayCommand<string>(ExecuteSelectThinkValue);
        ToggleToolRequest = new RelayCommand<string?>(ExecuteToggleToolRequest);
        
        SelectedFile = fileToUploadViewModel ?? throw new ArgumentNullException(nameof(fileToUploadViewModel));
        SelectedFile.IsActive = true;

        AvailableAgents = [];
    }

    private void ExecuteSelectThinkValue(string? thinkValue)
    {
        SelectedThinkValue = thinkValue;
    }

    private void ExecuteToggleToolRequest(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName)) return;
        
        // does ToolRequests contain the tool with the same name?
        if (ToolRequests.Any(x => x.ToolName == toolName))
        {
            // yes, remove it
            ToolRequests.Remove(ToolRequests.First(x => x.ToolName == toolName));
        }
        else
        {
            // no, add it
            ToolRequests.Add(new ToolRequest
            {
                ToolName = toolName
            });
        }
        
        ToolRequests = new HashSet<ToolRequest>(ToolRequests);
    }

    /// Executes the command to start a new chat session.
    /// Resets the current chat state and initializes a fresh session for user interaction.
    private void ExecuteNewChat()
    {
        try
        {
            _appState.SelectedChatSessionId = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new chat");
            _notificationService.ShowErrorNotification("Error", "Failed to start new chat. Please try again.");
        }
    }

    /// Executes the logic for selecting an agent from the available agents list.
    /// Updates the application state with the selected agent and modifies properties tied to the selection.
    /// <param name="agent">The agent to be selected. If null, no action is performed.</param>
    private async Task ExecuteSelectAgentAsync(AesirAgentBase? agent)
    {
        if (agent != null)
        {
            SelectedAgent = agent;
            SelectedAgentName = agent.Name;
            _appState.SelectedAgent = agent;

            ToolsAvailable =
                (await _configurationService.GetToolsForAgentAsync(agent.Id!.Value)).Select(t =>
                new ToolRequest()
                    {
                        ToolName = t.ToolName!
                    }
            ).ToHashSet();
            
            ToolRequests = new HashSet<ToolRequest>(ToolsAvailable);

            if ((agent.AllowThinking ?? false) && 
                agent.ThinkValue.HasValue && !agent.ThinkValue.Value.IsBoolean())
            {
                ThinkingToggleVisible = true;
                var thinkValueString = (string?)agent.ThinkValue.Value;

                if (thinkValueString != null)
                {
                    var thinkValues = thinkValueString.Split(',',  StringSplitOptions.RemoveEmptyEntries);
                    ThinkValues.Clear();
                    foreach (var thinkValue in thinkValues)
                    {
                        ThinkValues.Add(thinkValue.Trim().Transform(To.TitleCase));
                    }

                    if (thinkValues.Length < 1)
                    {
                        ThinkingToggleVisible = false;
                        return;
                    }
                    
                    SelectedThinkValue = ThinkValues.First();
                }
            }
            else
            {
                ThinkingToggleVisible = false;
                ThinkValues.Clear();
                SelectedThinkValue = null;
            }
        }
    }

    /// Executes the command to toggle the hands-free mode functionality.
    /// Alters the application state to enable or disable hands-free operations, which can include voice activation or other hands-free capabilities.
    private async void ExecuteShowHandsFree()
    {
        _navigationService.NavigateToHandsFree();
    }

    /// Triggered when the application or component is activated.
    /// Typically used to handle initialization logic, resource allocation, or state updates necessary when focusing or resuming activity.
    protected override async void OnActivated()
    {
        base.OnActivated();

        await LoadApplicationStateAsync();
    }

    /// Handles the receipt of a property change message related to the application state.
    /// Updates the internal chat session state when the selected chat session ID changes.
    /// Initiates asynchronous loading of the corresponding chat session if the current and new session IDs mismatch.
    /// <param name="message">The property change message containing the property name and the new selected chat session ID.</param>
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if (message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            if (_appState.ChatSession?.Id != _appState.SelectedChatSessionId)
                Dispatcher.UIThread.InvokeAsync(async () => await LoadChatSessionAsync());
        }
    }

    /// Processes the received <see cref="FileUploadStatusMessage"/> to update the state of the file upload and related UI operations.
    /// <param name="message">The <see cref="FileUploadStatusMessage"/> instance representing the current processing state of the file upload.</param>
    public void Receive(FileUploadStatusMessage message)
    {
        SelectedFileEnabled = !message.IsProcessing;
        SendingChatOrProcessingFile = message.IsProcessing;
    }

    /// Processes a message of type FileDownloadRequestMessage, triggering actions for downloading
    /// a file associated with the current conversation.
    /// <param name="message">The message containing details about the file requested for download, including its name.</param>
    public async void Receive(FileDownloadRequestMessage message)
    {   
        await SaveFilePickerAsync(_appState.ChatSession!.Conversation.Id, message.FileName);
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

    /// Asynchronously loads the application state, ensuring necessary configurations,
    /// session data, and related resources are prepared for application functionality.
    /// <returns>
    /// A task representing the asynchronous operation of application state loading.
    /// </returns>
    private async Task LoadApplicationStateAsync()
    {
        await LoadIsSystemConfigurationReadyAsync();
        
        if (ConfigurationReadiness?.IsReady == true)
        {
            await LoadSelectedAgentAsync();
            await LoadChatSessionAsync();
        }
    }

    /// Asynchronously verifies and updates the state of system configuration readiness.
    /// Invokes the application state service to determine if the system setup requirements are satisfied.
    /// In case of an error, logs and reports the failure through the notification service.
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task LoadIsSystemConfigurationReadyAsync()
    {
        try
        {
            ConfigurationReadiness = await _appState.CheckSystemConfigurationReady();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if system is ready");
            _notificationService.ShowErrorNotification("Error", "Failed to check if system is ready. Please check your connection.");
        }
    }

    /// Loads the agent details for the currently selected agent asynchronously.
    /// Retrieves and processes agent data from the associated data source or service.
    /// Ensures the details are updated and ready for further operations related to the selected agent.
    private async Task LoadSelectedAgentAsync()
    {
        try
        {
            await _appState.LoadAvailableAgentsAsync();

            AvailableAgents.Clear();
            foreach (var agent in _appState.AvailableAgents)
                AvailableAgents.Add(agent);

            if (AvailableAgents.Count == 0)
                SelectedAgentName = "No agent available";
            else if (AvailableAgents.Count == 1)
                await ExecuteSelectAgentAsync(AvailableAgents.First());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agents");
            _notificationService.ShowErrorNotification("Error", "Failed to load available agents. Please check your connection.");
            SelectedAgentName = "Error loading agents";
        }
    }

    /// Loads the current chat session asynchronously.
    /// Uses the chat session manager to load session data, updates the conversation message collection,
    /// and sets the relevant application state. In the event of a failure, logs errors and notifies the user
    /// of the issue through the notification service.
    /// <returns>
    /// A Task representing the asynchronous operation of loading the chat session.
    /// </returns>
    private async Task LoadChatSessionAsync()
    {
        try
        {
            await _chatSessionManager.LoadChatSessionAsync();
            ConversationStarted = _appState.SelectedChatSessionId != null;
            await RefreshConversationMessagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat session");
            _notificationService.ShowErrorNotification("Error", "Failed to load chat session. Please try again.");
        }
    }

    /// Asynchronously refreshes and updates the list of conversation messages in the chat session.
    /// This method ensures efficient management by creating ViewModels off the UI thread and performing
    /// batch updates to the UI, improving performance and responsiveness.
    /// <return>
    /// A task that represents the asynchronous operation for refreshing the conversation messages.
    /// </return>
    private async Task RefreshConversationMessagesAsync()
    {
        try
        {
            ConversationMessages.Clear();

            if (_appState.ChatSession != null)
            {
                var messages = _appState.ChatSession.GetMessages().ToList();

                // Create all ViewModels off the UI thread for better performance
                var messageViewModels = await Task.Run(async () =>
                {
                    var viewModels = new List<MessageViewModel?>();
                    
                    foreach (var message in messages)
                    {
                        var viewModel = await CreateMessageViewModelAsync(message);
                        viewModels.Add(viewModel);
                    }
                    
                    return viewModels;
                });

                // Single UI thread update with all ViewModels
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    foreach (var viewModel in messageViewModels.Where(vm => vm != null))
                    {
                        ConversationMessages.Add(viewModel);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh conversation messages");
            _notificationService.ShowErrorNotification("Error", "Failed to load conversation messages.");
        }
    }

    /// Asynchronously creates a new message view model instance.
    /// This method is responsible for initializing and preparing the message view model
    /// for use in the application's messaging workflow, incorporating necessary data and dependencies.
    /// <param name="messageId">The unique identifier of the message to create a view model for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests, allowing the operation to be terminated prematurely.</param>
    /// <returns>A task that represents the asynchronous operation, containing the initialized message view model.</returns>
    private async Task<MessageViewModel?> CreateMessageViewModelAsync(AesirChatMessage message)
    {
        MessageViewModel? messageViewModel = null;
        
        // Create ViewModel instance on background thread
        switch (message.Role)
        {
            case "user":
                messageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
                break;

            case "assistant":
                messageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
                break;

            case "system":
                messageViewModel = Ioc.Default.GetService<SystemMessageViewModel>();
                break;
        }

        // Set message content (this includes markdown rendering which is now optimized to run off UI thread)
        if (messageViewModel != null)
        {
            if (message.Role == "system")
            {
                var promptPersona = _appState.SelectedAgent?.ChatPromptPersona;
                string? customContent = null;

                if (promptPersona == PromptPersona.Custom)
                    customContent = _appState.SelectedAgent?.ChatCustomPromptContent;
                    
                // always reset the system message
                await messageViewModel.SetMessage(AesirChatMessage.NewSystemMessage(promptPersona, customContent));
            }
            else
            {
                await messageViewModel.SetMessage(message);
                
                messageViewModel.ChatSessionId = _appState.SelectedChatSessionId;
            }
        }

        return messageViewModel;
    }

    /// Adds a new message to the conversation asynchronously.
    /// Handles integration with the conversation service to update the current session with the provided message content.
    /// Ensures the message is formatted and processed according to application rules.
    /// <param name="messageContent">The content of the message to be added to the conversation.</param>
    /// <param name="conversationId">The unique identifier of the conversation where the message is being added.</param>
    /// <return>Task representing the asynchronous operation of adding the message to the conversation.</return>
    private async Task AddMessageToConversationAsync(AesirChatMessage message)
    {
        var messageViewModel = await CreateMessageViewModelAsync(message);

        if (messageViewModel != null)
        {
            ConversationMessages.Add(messageViewModel);
        }
    }

    /// Sends a message asynchronously to the specified recipient or service.
    /// Handles the message delivery process, including validation and response processing.
    /// Utilized for interactions that require asynchronous communication workflows.
    /// Ensures appropriate handling of network or service-related exceptions.
    /// Returns a task representing the completion of the message-sending operation.
    /// <return> A task representing the asynchronous operation of sending the message.</return>
    private async Task ExecuteSendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        if (SelectedAgent == null)
        {
            _notificationService.ShowInformationNotification("Info",
                "Please select an agent before sending a message.");
            return;
        }

        var currentMessage = ChatMessage;
        ChatMessage = null;

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
            await _chatSessionManager.ProcessChatRequestAsync(SelectedAgent.Id!.Value, 
                ConversationMessages, ToolRequests, SelectedAgent.AllowThinking, SelectedThinkValue?.Transform(To.LowerCase));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when sending message");
            _notificationService.ShowErrorNotification("Error", "Invalid input. Please check your message and try again.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when sending message");
            _notificationService.ShowErrorNotification("Error", "Unable to send message. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message");
            _notificationService.ShowErrorNotification("Error", "An unexpected error occurred. Please try again.");
        }
        finally
        {
            SendingChatOrProcessingFile = false;
        }
    }

    /// Displays a file selection dialog to the user asynchronously.
    /// Allows users to browse and select files from the local file system.
    /// Handles user interaction and returns the result of the file selection.
    private async Task ExecuteShowFileSelectionAsync()
    {
        try
        {
            var files = await OpenFilePickerAsync();

            if (files.Count >= 1)
            {
                SelectedFile!.SetConversationId(_appState.ChatSession!.Conversation.Id);
                await RequestFileUpload(files[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file selection dialog");
            _notificationService.ShowErrorNotification("Error", "Failed to open file selection dialog. Please try again.");
        }
    }

    /// Opens the file picker dialog and allows the user to select one or more files.
    /// Returns a read-only list of files selected by the user. If the storage provider is unavailable or an error occurs,
    /// the method will return an empty list.
    private async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(GetTopLevelControl());
            if (topLevel?.StorageProvider == null)
            {
                _logger.LogWarning("Storage provider not available");
                return [];
            }

            return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Upload File",
                AllowMultiple = false,
                FileTypeFilter = CreateDocumentFileTypeFilter()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file picker");
            return [];
        }
    }

    /// <summary>
    /// Initiates a request to upload a file to the server.
    /// Sends a message containing the file and associated conversation ID for further processing.
    /// </summary>
    /// <param name="file">The file to be uploaded.</param>
    /// <returns>A task that represents the asynchronous operation.</>
    private async Task RequestFileUpload(IStorageFile file)
    {
        try
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
            }
            catch (Exception e)
            {
                _logger.LogWarning("Attempted to upload inaccessible file");
                _notificationService.ShowErrorNotification("Error", "Invalid file path.");
                return;
            }

            if (_appState.ChatSession == null)
            {
                _logger.LogWarning("Attempted to upload file without an active chat session");
                _notificationService.ShowInformationNotification("Info", "Please start a chat session before uploading files.");
                return;
            }

            WeakReferenceMessenger.Default.Send(new FileUploadRequestMessage()
            {
                ConversationId = _appState.ChatSession.Conversation.Id,
                File = file
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request file upload for: {FileName}", file.Name);
            _notificationService.ShowErrorNotification("Error", "Failed to upload file. Please try again.");
        }
    }

    /// Opens a file picker dialog to allow the user to select a location and file name for saving a file.
    /// Validates the availability of the required storage provider.
    /// Constructs the file path using the conversation ID and the provided file name before initiating download.
    /// <param name="convId">The unique identifier for the conversation used to construct the file path.</param>
    /// <param name="fileName">The name of the file to be saved, suggested to the user in the file picker dialog.</param>
    /// <return>A task that represents the asynchronous operation of showing the file picker dialog and handling file saving logic.</return>
    private async Task SaveFilePickerAsync(string convId, string fileName)
    {
        // Get the top-level window or control to access the StorageProvider
        var topLevel = TopLevel.GetTopLevel(GetTopLevelControl());
        if (topLevel?.StorageProvider == null)
        {
            _logger.LogWarning("Storage provider not available");
        }

        if (topLevel != null)
        {
            // Open a Save File dialog
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Download File",
                SuggestedFileName = fileName, 
                ShowOverwritePrompt = true
            });

            if (file != null)
            {
                // database filename is the conversation id + filename
                var fullFileName = $"/{convId}/{fileName}";
                
                var localFilePath = file.Path.LocalPath;
                await DownloadFileAsync(fullFileName, localFilePath);
                
                // Optionally, show a success message to the user
            }
        }
    }
    
    /// Downloads a file from a remote source and saves it to a specified local file path.
    /// <param name="fileName">The name of the file to download from the remote source.</param>
    /// <param name="localFilePath">The local file path where the downloaded file will be saved.</param>
    /// <returns>A task that represents the asynchronous file download operation. It completes when the file has been fully downloaded and saved.</returns>
    private async Task DownloadFileAsync(string fileName, string localFilePath)
    {
        try
        {
            await using var contentStream = await _documentCollectionService.GetFileContentStreamAsync(fileName);
            // Create a FileStream to write the downloaded content to a local file
            await using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            // Copy the content from the network stream to the file stream
            await contentStream.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            // Handle other potential errors during file writing
            _logger.LogError(ex, "Error saving file to {LocalPath}", localFilePath);
            throw;
        }
    }
    
    /// Asynchronously regenerates the conversation by resending a specific user message and clearing all subsequent messages.
    /// This method locates the target user message within the conversation, removes all messages following it,
    /// and restarts the conversation by reprocessing the user message.
    /// <param name="userMessageViewModel">The user message to be regenerated, represented as an instance of <see cref="UserMessageViewModel"/>.</param>
    /// <returns>A task that represents the asynchronous operation of message regeneration.</returns>
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
    /// Regenerates the specified assistant message in the conversation history by removing related messages and
    /// re-sending the preceding user message to generate a new response.
    /// </summary>
    /// <param name="assistantMessageViewModel">The assistant message view model instance that needs to be regenerated.</param>
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
    /// This method verifies prerequisites, updates the conversation state, and processes the message appropriately.
    /// <param name="userMessageViewModel">The user message view model representing the message to be resent.</param>
    /// <returns>A task representing the asynchronous operation of resending the user message.</returns>
    private async Task ResendUserMessage(UserMessageViewModel userMessageViewModel)
    {
        if (SelectedAgent == null)
        {
            _notificationService.ShowInformationNotification("Info",
                "Please select an agent before sending a message.");
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

        await _chatSessionManager.ProcessChatRequestAsync(
            SelectedAgent.Id!.Value, ConversationMessages, ToolRequests, SelectedAgent.AllowThinking, SelectedThinkValue?.Transform(To.LowerCase));

        SendingChatOrProcessingFile = false;
    }

    /// Releases the resources used by the ChatViewViewModel and performs cleanup operations.
    /// Implements the IDisposable interface to properly manage resource disposal.
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

    /// Releases all resources used by the current instance of the class.
    /// Implements the IDisposable interface to allow for deterministic cleanup of unmanaged resources and proper disposal of managed resources.
    /// Once disposed, the object cannot be used and should be released for garbage collection.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// Retrieves the top-level control of the application based on the application's lifetime configuration.
    /// Determines whether the application is running with a classic desktop style or in a single-view environment
    /// and returns the respective top-level control if available.
    /// Logs an error and returns null in case of any exceptions during retrieval.
    /// <returns>
    /// The ContentControl representing the top-level application control, or null if it cannot be determined.
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

    /// Provides functionality to create and manage a filter for document file types.
    /// Enables the specification of supported file extensions to streamline document management workflows and ensure compatibility checks.
    private static List<FilePickerFileType> CreateDocumentFileTypeFilter()
    {
        // Generate patterns (*.ext format)
        var patterns = FileTypeManager.DocumentProcessingExtensions
            .Select(ext => $"*{ext}")
            .ToArray();

        // Get corresponding MIME types
        var mimeTypes = FileTypeManager.DocumentProcessingMimeTypes;

        // Generate Apple UTIs for common document processing types
        var appleUtis = new List<string>
        {
            FileTypeManager.AppleUTIs.Csv,
            FileTypeManager.AppleUTIs.Jpeg,
            FileTypeManager.AppleUTIs.Pdf,
            FileTypeManager.AppleUTIs.Html,
            FileTypeManager.AppleUTIs.Markdown,
            FileTypeManager.AppleUTIs.Png,
            FileTypeManager.AppleUTIs.PlainText,
            FileTypeManager.AppleUTIs.Tiff,
            FileTypeManager.AppleUTIs.Xml,
            FileTypeManager.AppleUTIs.Json
        };

        var extensionsAllowed = string.Join(",", patterns);
        return
        [
            new FilePickerFileType(extensionsAllowed)
            {
                Patterns = patterns,
                MimeTypes = mimeTypes,
                AppleUniformTypeIdentifiers = appleUtis.ToArray()
            }
        ];
    }
}

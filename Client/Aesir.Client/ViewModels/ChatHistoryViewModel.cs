using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Humanizer;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the ViewModel responsible for managing and presenting chat history grouped by date.
/// </summary>
/// <remarks>
/// This ViewModel interacts with the application state and chat history service to load, group, and refresh
/// chat history data. It also listens for property changes and messages related to changes in chat history
/// to ensure the UI remains in sync with the underlying data. Proper resource management is facilitated via
/// the implementation of the IDisposable interface for cleanup purposes.
/// </remarks>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IRecipient{TMessage}" />
/// <seealso cref="IDisposable" />
public partial class ChatHistoryViewModel
    : ObservableRecipient, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ChatHistoryChangedMessage>, IDisposable
{
    /// Represents a collection of chat history items grouped by date.
    /// This property holds an `ObservableGroupedCollection` where each group is identified by a
    /// human-readable date string (e.g., "Today", "Yesterday"). Each group contains a set of
    /// `ChatHistoryButtonViewModel` objects representing individual chat session entries.
    /// Automatically refreshed when chat session data changes, it enables efficient display
    /// and categorization of chat history in the user interface.
    public ObservableGroupedCollection<string, ChatHistoryButtonViewModel> ChatHistoryByDate { get; } = [];

    /// <summary>
    /// Represents a command that shows a general settings view
    /// </summary>
    public ICommand ShowGeneralSettings { get; set; }
    
    /// <summary>
    /// Represents a command that shows a inference engines view
    /// </summary>
    public ICommand ShowInferenceEngines { get; }

    /// <summary>
    /// Represents a command that shows a agents view
    /// </summary>
    public ICommand ShowAgents { get; }

    /// <summary>
    /// Represents a command that shows a documents view
    /// </summary>
    public ICommand ShowDocuments { get; }

    /// <summary>
    /// Represents a command that shows a logs view
    /// </summary>
    public ICommand ShowLogs { get; }

    /// <summary>
    /// Represents a command that shows a tools view
    /// </summary>
    public ICommand ShowTools { get; }

    /// <summary>
    /// Represents a command that shows an MCP Servers view
    /// </summary>
    public ICommand ShowMcpServers { get; }

    /// <summary>
    /// Manages the cancellation of debounce operations for handling user input changes,
    /// such as search functionality in the <see cref="ChatHistoryViewModel"/>.
    /// </summary>
    /// <remarks>
    /// This <see cref="CancellationTokenSource"/> instance is used to ensure efficient processing by allowing
    /// the cancellation of pending tasks when the input changes rapidly. It helps prevent redundant
    /// computations and ensures only the most recent input triggers the desired operations.
    /// The token source is re-initialized whenever new input occurs to support timely task cancellation.
    /// </remarks>
    private CancellationTokenSource _debounceTokenSource = null!;

    /// <summary>
    /// Represents the delay duration, in milliseconds, used for debouncing certain operations
    /// within the ViewModel, such as reacting to changes in the search text input.
    /// This value helps reduce unnecessary frequent executions by waiting for a brief period
    /// of inactivity before triggering the associated operation, improving performance and responsiveness.
    /// </summary>
    private const int DebounceDelayMs = 300; // 300ms delay

    /// Represents the minimum number of characters required in a search string
    /// before a search operation is triggered.
    /// This constant is used to prevent search queries from being executed for
    /// inputs that are too short, thereby improving performance and avoiding
    /// unnecessary processing or network requests.
    private const int MinSearchLength = 3; // Minimum characters to start searching

    /// Represents the text input used to search within the chat history.
    /// This variable is updated dynamically as the user enters or modifies the search query.
    /// The search operation is initiated only when the input text length satisfies the minimum required characters.
    [ObservableProperty] private string _searchText = string.Empty;

    private readonly ApplicationState _appState;
    
    private readonly IChatHistoryService _chatHistoryService;

    /// <summary>
    /// Represents a service to aid in navigation
    /// </summary>
    private readonly INavigationService _navigationService;

    public ChatHistoryViewModel(
        ApplicationState appState,
        IChatHistoryService chatHistoryService,
        INavigationService navigationService)
    {
        _appState = appState;
        _chatHistoryService = chatHistoryService;
        _navigationService = navigationService;

        ShowGeneralSettings = new RelayCommand(ExecuteShowGeneralSettings);
        ShowInferenceEngines = new RelayCommand(ExecuteShowInferenceEngines);
        ShowAgents = new RelayCommand(ExecuteShowAgents);
        ShowDocuments = new RelayCommand(ExecuteShowDocuments);
        ShowTools = new RelayCommand(ExecuteShowTools);
        ShowMcpServers = new RelayCommand(ExecuteShowMcpServers);
        ShowLogs = new RelayCommand(ExecuteShowLogs);
    }

    /// Handles the event when the search text is modified. This method incorporates debounce logic
    /// to delay search execution and cancels any ongoing search operations if necessary.
    /// <param name="value">The newly updated search text input. If the input is blank or does not meet
    /// the minimum required length for a search, the entire chat history is reloaded.</param>
    partial void OnSearchTextChanged(string value)
    {
        // Cancel any pending search
        if (_debounceTokenSource is { IsCancellationRequested: false })
        {
            _debounceTokenSource.Cancel();
            _debounceTokenSource.Dispose();
        }

        _debounceTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(value) || value.Length < MinSearchLength)
        {
            // If search is cleared or too short, reload full history
            Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
            return;
        }
        
        // Debounce the search
        var token = _debounceTokenSource.Token;
        _ = Task.Delay(DebounceDelayMs, token)
            .ContinueWith(t => 
            {
                if (!t.IsCanceled && !token.IsCancellationRequested)
                {
                    Dispatcher.UIThread.InvokeAsync(() => SearchChatHistoryAsync(value));
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    /// Handles activation of the ViewModel. This method initializes the view model state by
    /// clearing existing chat session data and resetting the grouped chat history collection.
    /// It also schedules the asynchronous loading of the chat history data on the UI thread.
    protected override void OnActivated()
    {
        base.OnActivated();

        ChatHistoryByDate.Clear();
        _appState.ChatSessions.Clear();

        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }

    /// Searches the chat history with the specified search query, retrieves matching chat sessions,
    /// and updates the display with the results.
    /// <param name="searchQuery">The search query used to filter and retrieve relevant chat history items.</param>
    /// <returns>A task that represents the asynchronous search operation.</returns>
    private async Task SearchChatHistoryAsync(string searchQuery)
    {
        var foundChatSessions =
            await _chatHistoryService.SearchChatSessionsAsync("Unknown", searchQuery);

        RefreshChatHistoryDisplay(foundChatSessions ?? []);
    }

    /// Refreshes the chat history display by organizing and grouping chat sessions by date,
    /// and populating the grouped results into the observable collection for viewing.
    /// <param name="chatSessions">The collection of chat sessions to display. The sessions are grouped by date
    /// in descending order and added to the observable grouped collection for presentation.</param>
    private void RefreshChatHistoryDisplay(IEnumerable<AesirChatSessionItem> chatSessions)
    {
        ChatHistoryByDate.Clear();

        foreach (var chatSessionGroup in chatSessions.GroupBy(cs =>
                     DateOnly.FromDateTime(cs.UpdatedAt.ToLocalTime().DateTime)))
        {
            foreach (var chatSession in chatSessionGroup)
            {
                if (_appState.ChatSessions.All(cs => cs.Id != chatSession.Id))
                    _appState.ChatSessions.Add(chatSession);
            }

            // stuff like this we may need to set TZ of the container for backend
            var groupKey = chatSessionGroup.Key.Humanize().Humanize(LetterCasing.Sentence).Replace("Now", "Today");
            
            var historyItems = chatSessionGroup.ToList()
                .OrderByDescending(cs => cs.UpdatedAt)
                .Select(CreateChatHistoryButtonViewModel);
            
            ChatHistoryByDate.AddGroup(
                groupKey,
                historyItems
            );  
        }
    }


    /// Creates a new instance of the ChatHistoryButtonViewModel and initializes it with the provided chat session item.
    /// <param name="cs">The chat session item to associate with the ChatHistoryButtonViewModel.</param>
    /// <returns>A fully initialized ChatHistoryButtonViewModel instance with its properties set based on the provided chat session item.</returns>
    private ChatHistoryButtonViewModel CreateChatHistoryButtonViewModel(AesirChatSessionItem cs)
    {
        var chatHistoryButtonViewModel = Ioc.Default.GetService<ChatHistoryButtonViewModel>();

        if (chatHistoryButtonViewModel == null)
            throw new InvalidOperationException("Could not resolve ChatHistoryButtonViewModel");

        chatHistoryButtonViewModel.SetChatSessionItem(cs);
        chatHistoryButtonViewModel.IsActive = true;
        chatHistoryButtonViewModel.IsChecked = _appState.SelectedChatSessionId == cs.Id;

        return chatHistoryButtonViewModel;
    }

    /// Asynchronously loads the chat history for the current user and updates the user interface to reflect
    /// the retrieved data. This method retrieves the chat sessions from the chat history service, processes
    /// the data, and updates the grouped collection for display. It includes error handling to manage
    /// potential exceptions during the data retrieval process.
    /// <returns>A task representing the asynchronous execution of loading and updating the chat history display.</returns>
    private async Task LoadChatHistoryAsync()
    {
        try
        {
            var chatSessions = await _chatHistoryService.GetChatSessionsAsync();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (chatSessions != null)
            {
                RefreshChatHistoryDisplay(chatSessions);
            }
        }
        catch (Exception ex)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Error loading chat history: {ex.Message}");
        }
    }

    /// Receives a PropertyChangedMessage indicating changes in the specified property and handles the operation
    /// to update the chat history asynchronously when certain conditions are met.
    /// <param name="message">
    /// The PropertyChangedMessage<bool> instance containing details about the property change.
    /// Expected to provide information about the property 'SendingChatOrProcessingFile' from ChatViewViewModel.
    /// </param>
    public void Receive(PropertyChangedMessage<bool> message)
    {
        if(message is { PropertyName: nameof(ChatViewViewModel.SendingChatOrProcessingFile), NewValue: false })
        {
            Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
        }
    }

    /// Handles the receipt of a `ChatHistoryChangedMessage` to trigger a reload of the chat history.
    /// <param name="message">The `ChatHistoryChangedMessage` instance containing the notification data
    /// that initiates the chat history reload process.</param>
    public void Receive(ChatHistoryChangedMessage message)
    {
        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }

    private void ExecuteShowGeneralSettings()
    {
        WeakReferenceMessenger.Default.Send(new ShowGeneralSettingsMessage());
    }

    private void ExecuteShowInferenceEngines()
    {
        _navigationService.NavigateToInferenceEngines();   
    }

    private void ExecuteShowAgents()
    {
        _navigationService.NavigateToAgents();
    }

    private void ExecuteShowDocuments()
    {
        _navigationService.NavigateToDocuments();
    }

    private void ExecuteShowTools()
    {
        _navigationService.NavigateToTools();
    }

    private void ExecuteShowMcpServers()
    {
        _navigationService.NavigateToMcpServers();
    }

    private void ExecuteShowLogs()
    {
        _navigationService.NavigateToLogs();
    }

    /// Releases all resources used by the ChatHistoryViewModel instance.
    /// This method cancels any ongoing operations associated with the debounce token,
    /// disposes of the CancellationTokenSource to release managed resources,
    /// and suppresses finalization of the instance to optimize garbage collection.
    /// Proper disposal of this method is required to prevent resource leaks and
    /// to ensure that no background operations persist after the object is no longer in use.
    public void Dispose()
    {
        if (_debounceTokenSource is { IsCancellationRequested: false })
        {
            _debounceTokenSource.Cancel();
            _debounceTokenSource.Dispose();
        }

        _debounceTokenSource = null!;
        GC.SuppressFinalize(this);
    }
}
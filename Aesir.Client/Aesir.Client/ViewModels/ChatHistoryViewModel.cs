using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Humanizer;

namespace Aesir.Client.ViewModels;

/// <summary>
/// Represents the ViewModel responsible for managing and displaying the chat history in a grouped collection.
/// </summary>
/// <remarks>
/// This ViewModel interacts with the application's state and chat history service to load and refresh
/// the chat history. It listens to property changes and specific messages related to the chat history status
/// to update the user interface accordingly. It also handles resource disposal to ensure proper cleanup.
/// </remarks>
/// <seealso cref="ObservableRecipient" />
/// <seealso cref="IRecipient{TMessage}" />
/// <seealso cref="IDisposable" />
public partial class ChatHistoryViewModel(
    ApplicationState appState,
    IChatHistoryService chatHistoryService)
    : ObservableRecipient, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ChatHistoryChangedMessage>, IDisposable
{
    /// Represents a grouped collection of chat history items organized by date.
    /// This property contains an `ObservableGroupedCollection` where each group is identified
    /// by a string representing a human-readable date (e.g., "Today" or "Yesterday"). Each group
    /// contains a collection of `ChatHistoryButtonViewModel` items that represent individual chat
    /// session entries.
    /// The collection is dynamically populated and updated based on changes in chat session data.
    public ObservableGroupedCollection<string, ChatHistoryButtonViewModel> ChatHistoryByDate { get; } = [];

    /// <summary>
    /// A <see cref="CancellationTokenSource"/> instance used to manage and control the cancellation of
    /// debounce operations for search functionality within the <see cref="ChatHistoryViewModel"/>.
    /// </summary>
    /// <remarks>
    /// This variable is essential for debouncing search input to minimize redundant operations and improve performance.
    /// It is re-initialized whenever the search input changes, ensuring timely cancellation of ongoing tasks.
    /// </remarks>
    private CancellationTokenSource _debounceTokenSource = null!;

    /// <summary>
    /// Specifies the delay duration, in milliseconds, used for debouncing operations such as search text input.
    /// This delay allows the system to wait for a brief period of inactivity before executing the associated task,
    /// thereby reducing unnecessary repetitive operations.
    /// </summary>
    private const int DebounceDelayMs = 300; // 300ms delay

    /// <summary>
    /// Represents the minimum length a search string must have before initiating a search operation.
    /// </summary>
    /// <remarks>
    /// Used to ensure that search operations are not performed with excessively short inputs,
    /// which helps optimize performance and avoid redundant or unnecessary queries.
    /// </remarks>
    private const int MinSearchLength = 3; // Minimum characters to start searching

    /// <summary>
    /// Represents the text entered by the user to search within the chat history.
    /// This property is bound to the UI and updated as the user types input.
    /// </summary>
    /// <remarks>
    /// The search functionality is enabled when the length of the input text
    /// meets or exceeds the defined minimum search length.
    /// </remarks>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// Handles the event when the search text is modified. This method handles debounce logic for
    /// search execution and manages cancellation of previous searches if applicable.
    /// <param name="value">The updated search text input. If empty or shorter than the minimum
    /// search length, the full chat history will be reloaded.</param>
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

    /// <summary>
    /// Invoked when the view model is activated. This method is responsible for initializing
    /// and refreshing the state related to chat history. It clears existing chat session data
    /// and updates the grouped chat history list. Additionally, it triggers the asynchronous
    /// loading of chat history data on the UI thread.
    /// </summary>
    protected override void OnActivated()
    {
        base.OnActivated();

        ChatHistoryByDate.Clear();
        appState.ChatSessions.Clear();
        
        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }

    /// Searches the chat history using the provided search query and updates the display with the results.
    /// <param name="searchQuery">The search query used to filter chat history items.</param>
    /// <returns>A task that represents the asynchronous search operation.</returns>
    private async Task SearchChatHistoryAsync(string searchQuery)
    {
        var foundChatSessions = 
            await chatHistoryService.SearchChatSessionsAsync("Unknown", searchQuery);
        
        RefreshChatHistoryDisplay(foundChatSessions ?? Array.Empty<AesirChatSessionItem>());
    }

    /// <summary>
    /// Refreshes the chat history display by organizing and grouping chat sessions by date,
    /// and populating them into the observable collection for display.
    /// </summary>
    /// <param name="chatSessions">The collection of chat sessions to be displayed, grouped, and ordered by date.</param>
    private void RefreshChatHistoryDisplay(IEnumerable<AesirChatSessionItem> chatSessions)
    {
        ChatHistoryByDate.Clear();
        
        foreach (var chatSessionGroup in chatSessions.GroupBy(
                     cs => DateOnly.FromDateTime(cs.UpdatedAt.ToLocalTime().DateTime)))
        {
            foreach (var chatSession in chatSessionGroup)
            {
                if(appState.ChatSessions.All(cs => cs.Id != chatSession.Id))
                    appState.ChatSessions.Add(chatSession);
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


    /// Creates a new instance of the ChatHistoryButtonViewModel and initializes it with the given chat session item.
    /// <param name="cs">The chat session item to associate with the ChatHistoryButtonViewModel.</param>
    /// <returns>A fully initialized ChatHistoryButtonViewModel instance.</returns>
    private ChatHistoryButtonViewModel CreateChatHistoryButtonViewModel(AesirChatSessionItem cs)
    {
        var chatHistoryButtonViewModel = Ioc.Default.GetService<ChatHistoryButtonViewModel>();

        if (chatHistoryButtonViewModel == null) throw new InvalidOperationException("Could not resolve ChatHistoryButtonViewModel");

        chatHistoryButtonViewModel.SetChatSessionItem(cs);
        chatHistoryButtonViewModel.IsActive = true;
        chatHistoryButtonViewModel.IsChecked = appState.SelectedChatSessionId == cs.Id;

        return chatHistoryButtonViewModel;
    }

    /// <summary>
    /// Asynchronously loads the chat history for the current user and updates the chat history display.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation of loading chat history.</returns>
    private async Task LoadChatHistoryAsync()
    {
        try
        {
            var chatSessions = await chatHistoryService.GetChatSessionsAsync("Unknown");
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
    /// Expected to provide information about the property 'SendingChatOrProcessingFile' from MainViewViewModel.
    /// </param>
    public void Receive(PropertyChangedMessage<bool> message)
    {
        if(message is { PropertyName: nameof(MainViewViewModel.SendingChatOrProcessingFile), NewValue: false })
        {
            Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
        }
    }

    /// Handles the receipt of a `ChatHistoryChangedMessage`.
    /// <param name="message">The `ChatHistoryChangedMessage` instance containing the notification data to process.</param>
    public void Receive(ChatHistoryChangedMessage message)
    {
        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }

    /// Releases all resources used by the ChatHistoryViewModel instance.
    /// This method cancels any ongoing operations associated with the debounce token,
    /// disposes of the CancellationTokenSource, and suppresses finalization of the object
    /// to ensure that resources are properly released and garbage collection is optimized.
    /// It is important to call this method when the ChatHistoryViewModel is no longer needed
    /// to avoid resource leaks and ensure proper disposal of unmanaged resources.
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
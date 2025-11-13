using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Common.Models;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

/// Represents the view model for a chat history button in the application.
/// This class is responsible for managing the state and behavior of an individual chat session button,
/// including binding the chat session's title and managing its selection state in relation to the application's overall state.
/// Provides functionality to set the associated chat session and to respond to property change messages regarding the session.
/// Inherits from:
/// - ObservableRecipient
/// - Implements IRecipient<PropertyChangedMessage<Guid?>> for handling application state updates.
/// - Implements IDisposable for resource cleanup.
public partial class ChatHistoryButtonViewModel(
    ILogger<ChatHistoryButtonViewModel> logger,
    ApplicationState appState)
    : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>, IDisposable
{
    /// <summary>
    /// Backing field for the Title property. Stores the title associated with the chat history button in the view model.
    /// </summary>
    private string _title = "Chat History";

    /// <summary>
    /// Gets or sets the title associated with the chat history button.
    /// This property represents the display name for the chat session in the user interface and supports data binding.
    /// </summary>
    [MinLength(10)]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Backing field for the ChatSessionId property. Represents the unique identifier for the chat session
    /// associated with this button in the chat history.
    /// </summary>
    /// <remarks>
    /// Used internally to manage and reference the specific chat session linked to the view model.
    /// This field supports property binding and change notifications.
    /// </remarks>
    [ObservableProperty] private Guid? _chatSessionId;

    /// <summary>
    /// Backing field for the property indicating whether the chat history button is checked.
    /// Represents a boolean flag used to track the selection or activation state of the button.
    /// </summary>
    [ObservableProperty] private bool _isChecked;

    /// <summary>
    /// Backing field for the associated AesirChatSessionItem instance within the ViewModel.
    /// Represents the chat session data, including details such as session ID, title, and last updated timestamp.
    /// </summary>
    private AesirChatSessionItem? _chatSessionItem;

    /// <summary>
    /// Sets the chat session item for the view model and updates associated properties.
    /// </summary>
    /// <param name="chatSessionItem">The chat session item to be set. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="chatSessionItem"/> is null.</exception>
    public void SetChatSessionItem(AesirChatSessionItem chatSessionItem)
    {
        _chatSessionItem = chatSessionItem ?? throw new ArgumentNullException(nameof(chatSessionItem));

        Title = chatSessionItem.Title;
        ChatSessionId = chatSessionItem.Id;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    /// <summary>
    /// Handles changes to the IsChecked property. This method is invoked whenever the value
    /// of the IsChecked property changes, enabling specific operations to execute based on
    /// the old and new values.
    /// </summary>
    /// <param name="oldValue">The previous value of the IsChecked property.</param>
    /// <param name="newValue">The new value of the IsChecked property.</param>
    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue == false && appState.SelectedChatSessionId == ChatSessionId)
            appState.SelectedChatSessionId = null;

        if (newValue && appState.SelectedChatSessionId != ChatSessionId)
            appState.SelectedChatSessionId = ChatSessionId;
    }

    /// <summary>
    /// Handles the receipt of a property change message related to the chat session selection.
    /// </summary>
    /// <param name="message">
    /// The property change message containing information about the changed property
    /// and its new value. Specifically observes changes to the SelectedChatSessionId
    /// property in the ApplicationState.
    /// </param>
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if (message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsChecked = message.NewValue == ChatSessionId;

                //_logger.LogDebug("ChatSessionId: {ChatSessionId}, IsChecked: {IsChecked}", ChatSessionId, IsChecked);
            });
        }
    }

    /// <summary>
    /// Renames the current chat session by prompting the user for a new title.
    /// This method utilizes a dialog service to display an input dialog where
    /// the user can enter a new name for the chat session. If a new title is provided,
    /// it updates the current title and persists the change in the underlying data store.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task RenameAsync()
    {
        var dialogService = Ioc.Default.GetService<IDialogService>();
        if (dialogService == null) return;
        var newTitle = await dialogService.ShowInputDialogAsync(
            "Rename Chat", Title, "Enter new name"
        );

        if (!string.IsNullOrWhiteSpace(newTitle))
        {
            // Update the title
            Title = newTitle;
            
            // Update the chat session title
            if (_chatSessionItem != null)
            {
                _chatSessionItem.Title = newTitle;
                
                // Save changes to your data store
                var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
                await chatHistoryService?.UpdateChatSessionTitleAsync(_chatSessionItem.Id, newTitle)!;
            }
        }
    }

    /// <summary>
    /// Asynchronously deletes the currently selected chat session after user confirmation.
    /// Displays a confirmation dialog to the user. If the user confirms, the method deletes the chat session
    /// using the appropriate service, updates the application state, and notifies subscribers of the change.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation of deleting the selected chat session.
    /// </returns>
    [RelayCommand]
    private async Task DeleteAsync()
    {
        var dialogService = Ioc.Default.GetService<IDialogService>();
        if (dialogService == null) return;

        var result = await dialogService.ShowConfirmationDialogAsync(
            "Delete Chat",
            $"Are you sure you want to delete this chat?");
        if (result)
        {
            // Delete the chat session
            if (ChatSessionId.HasValue)
            {
                var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
                await chatHistoryService?.DeleteChatSessionAsync(ChatSessionId.Value)!;
                
                appState.SelectedChatSessionId = null;
                
                // Update UI
                WeakReferenceMessenger.Default.Send(new ChatHistoryChangedMessage());
            }
        }
    }
    
    // This is the handler for right-clicks, which is now simplified
    // since the context menu is defined in XAML
    /// <summary>
    /// Handles the right-click action for a chat session in the view model.
    /// </summary>
    /// <remarks>
    /// This method logs a debug message indicating that a right-click interaction occurred for the associated
    /// chat session. It is used along with a context menu defined in the XAML file to facilitate user interaction.
    /// </remarks>
    [RelayCommand]
    private void RightClick()
    {
        logger.LogDebug("Right click handled in ViewModel for chat: {Title}", Title);
    }

    /// <summary>
    /// Releases the resources used by the instance of <see cref="ChatHistoryButtonViewModel"/>.
    /// </summary>
    /// <param name="disposing">A boolean value indicating whether the method is called explicitly (true) or by the garbage collector (false).</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Disposes of resources used by the ChatHistoryButtonViewModel instance.
    /// </summary>
    /// <remarks>
    /// Calls the protected Dispose method with a disposing flag set to true and suppresses finalization to optimize resource cleanup.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
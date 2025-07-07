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
/// This class is responsible for managing the data and behavior for an individual chat session button. It handles
/// binding the session's title and maintaining the selected state of the button based on application state.
/// Inherits from:
/// - ObservableRecipient
/// - Implements IRecipient for handling property change messages.
public partial class ChatHistoryButtonViewModel(
    ILogger<ChatHistoryButtonViewModel> logger,
    ApplicationState appState)
    : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>
{
    /// <summary>
    /// Backing field for the Title property. Represents the title associated with the chat history button.
    /// </summary>
    private string _title = "Chat History";

    /// Represents the title of the chat history button.
    /// The property is bound to the UI and displays the current title associated with
    /// the chat session. It can be updated programmatically or via user interaction.
    /// A minimum length of 10 characters is required when setting this property.
    [MinLength(10)]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    /// Represents the unique identifier of the current chat session.
    /// </summary>
    /// <remarks>
    /// This variable is used to track and reference the active chat session within the view model.
    /// It is marked as observable, allowing binding and notification of changes to the UI or other components.
    /// </remarks>
    [ObservableProperty]
    private Guid? _chatSessionId;

    /// <summary>
    /// Represents the state indicating whether the chat history button is checked.
    /// This property is used as a flag to determine the selection or activation state of the button.
    /// </summary>
    [ObservableProperty]
    private bool _isChecked;

    /// <summary>
    /// Represents the associated chat session data within the context of the ViewModel.
    /// This variable holds an instance of the <see cref="AesirChatSessionItem"/> class,
    /// representing the underlying chat session details such as ID, title, and update timestamp.
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
    /// Handles changes to the IsChecked property. This method is invoked whenever the value
    /// of the IsChecked property changes, allowing for specific logic to be executed based
    /// on the old and new values.
    /// <param name="oldValue">The previous value of the IsChecked property.</param>
    /// <param name="newValue">The new value of the IsChecked property.</param>
    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue == false && appState.SelectedChatSessionId == ChatSessionId)
            appState.SelectedChatSessionId = null;
        
        if (newValue && appState.SelectedChatSessionId != ChatSessionId)
            appState.SelectedChatSessionId = ChatSessionId;
    }

    /// Handles the receipt of a property change message related to the chat session selection.
    /// <param name="message">
    /// The property change message containing information about the changed property
    /// and its new value. In this context, it observes changes to the SelectedChatSessionId
    /// in the ApplicationState.
    /// </param>
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if(message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IsChecked = message.NewValue == ChatSessionId;
                
                //_logger.LogDebug("ChatSessionId: {ChatSessionId}, IsChecked: {IsChecked}", ChatSessionId, IsChecked);
            });
        }
    }

    /// Renames the current chat session by prompting the user for a new title.
    /// This method utilizes a dialog service to display an input dialog where
    /// the user can enter a new name for the chat session. If a new title is provided,
    /// it updates the current title and persists the change in the underlying data store.
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

    /// Asynchronously deletes the currently selected chat session after user confirmation.
    /// Displays a confirmation dialog to the user. If the user confirms, the method deletes the chat session
    /// using the appropriate service, updates the application state, and sends a message to notify subscribers.
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
    /// Handles the right-click action in the view model for a chat session.
    /// This method logs a debug message indicating that a right-click interaction was handled
    /// for the associated chat session, identified by its title. It is primarily used in
    /// conjunction with a context menu defined in the XAML file.
    [RelayCommand]
    private void RightClick()
    {
        logger.LogDebug("Right click handled in ViewModel for chat: {Title}", Title);
    }

}
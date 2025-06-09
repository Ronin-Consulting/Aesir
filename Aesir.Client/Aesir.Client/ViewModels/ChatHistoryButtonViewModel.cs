using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.ViewModels;

public partial class ChatHistoryButtonViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>
{
    private readonly ILogger<ChatHistoryButtonViewModel> _logger;
    private readonly ApplicationState _appState;
    
    private string _title = "Chat History";
    [MinLength(10)]
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    
    [ObservableProperty]
    private Guid? _chatSessionId;
    [ObservableProperty]
    private bool _isChecked;
    
    private AesirChatSessionItem? _chatSessionItem;
    
    public ChatHistoryButtonViewModel(
        ILogger<ChatHistoryButtonViewModel> logger, 
        ApplicationState appState)
    {
        _logger = logger;
        _appState = appState;
    }
    
    public void SetChatSessionItem(AesirChatSessionItem chatSessionItem)
    {
        _chatSessionItem = chatSessionItem ?? throw new ArgumentNullException(nameof(chatSessionItem));
        
        Title = chatSessionItem.Title;
        ChatSessionId = chatSessionItem.Id;
    }
    
    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue == false && _appState.SelectedChatSessionId == ChatSessionId)
            _appState.SelectedChatSessionId = null;
        
        if (newValue == true && _appState.SelectedChatSessionId != ChatSessionId)
            _appState.SelectedChatSessionId = ChatSessionId;
    }

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
    
    [RelayCommand]
    private async Task RenameAsync()
    {
        // // You can implement a dialog to get the new name
        // // For now, let's assume you have an IDialogService
        // var dialogService = Ioc.Default.GetService<IDialogService>();
        // if (dialogService == null) return;
        //
        // var newTitle = await dialogService.ShowInputDialogAsync("Rename Chat", "Enter new name:", Title);
        //
        // if (!string.IsNullOrWhiteSpace(newTitle))
        // {
        //     // Update the title
        //     Title = newTitle;
        //     
        //     // Update the chat session title
        //     if (_chatSessionItem != null)
        //     {
        //         _chatSessionItem.Title = newTitle;
        //         
        //         // Save changes to your data store
        //         var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
        //         await chatHistoryService?.UpdateChatSessionTitleAsync(_chatSessionItem.Id, newTitle)!;
        //     }
        // }

        if (!string.IsNullOrWhiteSpace(Title))
        {
            // Update the chat session title
            if (_chatSessionItem != null)
            {
                _chatSessionItem.Title = Title;
                
                // Save changes to your data store
                var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
                await chatHistoryService?.UpdateChatSessionTitleAsync(_chatSessionItem.Id, Title)!;
            }
        }
        else
        {
            if (_chatSessionItem != null)
            {
                Title = _chatSessionItem.Title;
            }
        }
    }

    [RelayCommand]
    private async Task CancelRenameAsync()
    {
        if (_chatSessionItem != null)
        {
            Title = _chatSessionItem.Title;
        }
        
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        // // Confirm deletion
        // var dialogService = Ioc.Default.GetService<IDialogService>();
        // if (dialogService == null) return;
        //
        // var result = await dialogService.ShowConfirmationDialogAsync(
        //     "Delete Chat", 
        //     $"Are you sure you want to delete this chat? This action cannot be undone.");
        //
        // if (result)
        // {
        //     // Delete the chat session
        //     if (ChatSessionId.HasValue)
        //     {
        //         var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
        //         await chatHistoryService?.DeleteChatSessionAsync(ChatSessionId.Value)!;
        //         
        //         _appState.SelectedChatSessionId = null;
        //         
        //         // Update UI
        //         WeakReferenceMessenger.Default.Send(new ChatHistoryChangedMessage());
        //     }
        // }
        // Delete the chat session
        if (ChatSessionId.HasValue)
        {
            var chatHistoryService = Ioc.Default.GetService<IChatHistoryService>();
            await chatHistoryService?.DeleteChatSessionAsync(ChatSessionId.Value)!;
                
            _appState.SelectedChatSessionId = null;
                
            // Update UI
            WeakReferenceMessenger.Default.Send(new ChatHistoryChangedMessage());
        }
    }
    
    // [RelayCommand]
    // private void CopyChatId()
    // {
    //     if (ChatSessionId.HasValue)
    //     {
    //         // Copy to clipboard
    //         //Application.Current?.Clipboard?.SetTextAsync(ChatSessionId.Value.ToString());
    //         this.GetClipboard().SetTextAsync(ChatSessionId.Value.ToString());
    //         
    //         // Show toast notification
    //         _logger.LogInformation("Chat ID copied to clipboard");
    //         // Implement notification if you have a notification service
    //     }
    // }
    
    // This is the handler for right-clicks, which is now simplified
    // since the context menu is defined in XAML
    [RelayCommand]
    private void RightClick()
    {
        _logger.LogDebug("Right click handled in ViewModel for chat: {Title}", Title);
    }

}
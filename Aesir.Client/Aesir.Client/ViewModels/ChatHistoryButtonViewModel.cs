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

public partial class ChatHistoryButtonViewModel(
    ILogger<ChatHistoryButtonViewModel> logger,
    ApplicationState appState)
    : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>
{
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

    public void SetChatSessionItem(AesirChatSessionItem chatSessionItem)
    {
        _chatSessionItem = chatSessionItem ?? throw new ArgumentNullException(nameof(chatSessionItem));
        
        Title = chatSessionItem.Title;
        ChatSessionId = chatSessionItem.Id;
    }
    
    partial void OnIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (newValue == false && appState.SelectedChatSessionId == ChatSessionId)
            appState.SelectedChatSessionId = null;
        
        if (newValue == true && appState.SelectedChatSessionId != ChatSessionId)
            appState.SelectedChatSessionId = ChatSessionId;
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
    [RelayCommand]
    private void RightClick()
    {
        logger.LogDebug("Right click handled in ViewModel for chat: {Title}", Title);
    }

}
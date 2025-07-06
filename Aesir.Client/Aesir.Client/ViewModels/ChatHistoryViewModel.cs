using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Humanizer;

namespace Aesir.Client.ViewModels;

public partial class ChatHistoryViewModel(
    ApplicationState appState,
    IChatHistoryService chatHistoryService)
    : ObservableRecipient, IRecipient<PropertyChangedMessage<bool>>, IRecipient<ChatHistoryChangedMessage>
{
    public ObservableGroupedCollection<string, ChatHistoryButtonViewModel> ChatHistoryByDate { get; } = [];

    private CancellationTokenSource _debounceTokenSource;
    private const int DebounceDelayMs = 300; // 300ms delay
    private const int MinSearchLength = 3; // Minimum characters to start searching

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        // Cancel any pending search
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();
        
        if (string.IsNullOrWhiteSpace(value) || value.Length < MinSearchLength)
        {
            // If search is cleared or too short, reload full history
            Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
            return;
        }
        
        // Debounce the search
        var token = _debounceTokenSource.Token;
        Task.Delay(DebounceDelayMs, token).ContinueWith(t => 
        {
            if (!t.IsCanceled)
            {
                Dispatcher.UIThread.InvokeAsync(() => SearchChatHistoryAsync(value));
            }
        }, token);
    }
    
    protected override void OnActivated()
    {
        base.OnActivated();

        ChatHistoryByDate.Clear();
        appState.ChatSessions.Clear();
        
        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }
    
    private async Task SearchChatHistoryAsync(string searchQuery)
    {
        var foundChatSessions = 
            await chatHistoryService.SearchChatSessionsAsync("Unknown", searchQuery);
        
        RefreshChatHistoryDisplay(foundChatSessions);
    }

    private void RefreshChatHistoryDisplay(IEnumerable<AesirChatSessionItem> chatSessions)
    {
        ChatHistoryByDate.Clear();
        
        foreach (var chatSessionGroup in chatSessions.GroupBy(
                     cs => DateOnly.FromDateTime(cs.UpdatedAt.ToLocalTime().DateTime)))
        {
            foreach (var chatSession in chatSessionGroup)
            {
                if(appState.ChatSessions.Any(cs => cs.Id != chatSession.Id))
                    appState.ChatSessions.Add(chatSession);
            }

            var groupKey = chatSessionGroup.Key.Humanize().Humanize(LetterCasing.Sentence).Replace("Now", "Today");
            
            ChatHistoryButtonViewModel GetViewModel(AesirChatSessionItem cs)
            {
                var chatHistoryButtonViewModel = Ioc.Default.GetService<ChatHistoryButtonViewModel>();

                if (chatHistoryButtonViewModel == null) throw new InvalidOperationException("Could not resolve ChatHistoryButtonViewModel");

                chatHistoryButtonViewModel.SetChatSessionItem(cs);

                chatHistoryButtonViewModel.IsActive = true;

                chatHistoryButtonViewModel.IsChecked = appState.SelectedChatSessionId == cs.Id;
                
                return chatHistoryButtonViewModel;
            }

            var historyItems = chatSessionGroup.ToList()
                .OrderByDescending(cs => cs.UpdatedAt)
                .Select((Func<AesirChatSessionItem, ChatHistoryButtonViewModel>)GetViewModel);
            
            ChatHistoryByDate.AddGroup(
                groupKey,
                historyItems
            );  
        }
    }

    
    private async Task LoadChatHistoryAsync()
    {
        var chatSessions = await chatHistoryService.GetChatSessionsAsync("Unknown");
        RefreshChatHistoryDisplay(chatSessions);

    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if(message is { PropertyName: nameof(MainViewViewModel.SendingChatOrProcessingFile), NewValue: false })
        {
            Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
        }
    }

    public void Receive(ChatHistoryChangedMessage message)
    {
        Dispatcher.UIThread.InvokeAsync(LoadChatHistoryAsync);
    }
}
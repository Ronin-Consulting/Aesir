using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aesir.Client.ViewModels;

public partial class MainViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>, IDisposable
{
    [ObservableProperty] 
    private bool _micOn;
    [ObservableProperty] 
    private bool _panelOpen;
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private bool _sendingChat;
    [ObservableProperty] 
    private bool _hasChatMessage;
    [ObservableProperty] 
    private bool _conversationStarted;
    [ObservableProperty] 
    private string? _selectedModelName = "Select a model";
    
    private string? _chatMessage;
    public string? ChatMessage
    {
        get => _chatMessage;
        set
        {
            SetProperty(ref _chatMessage, value);
            HasChatMessage = !string.IsNullOrWhiteSpace(value);
        }
    }
    
    public ObservableCollection<MessageViewModel?> ConversationMessages { get; } = [];
    
    public ICommand ToggleChatHistory { get; }
    public ICommand ToggleNewChat { get; }
    public ICommand ToggleMicrophone { get; }
    public ICommand SendMessage => new AsyncRelayCommand(SendMessageAsync);
    
    private readonly ApplicationState _appState;
    private readonly ISpeechService? _speechService;
    private readonly IChatService _chatService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IModelService _modelService;

    public MainViewModel(
        ApplicationState appState,
        ISpeechService speechService,
        IChatService chatService,
        IChatHistoryService chatHistoryService,
        IModelService modelService)
    {
        _appState = appState;
        _speechService = speechService;
        _chatService = chatService;
        _chatHistoryService = chatHistoryService;
        _modelService = modelService;

        ToggleChatHistory = new RelayCommand(() => PanelOpen = !PanelOpen);
        ToggleNewChat = new RelayCommand(() => _appState.SelectedChatSessionId = null);
        ToggleMicrophone = new RelayCommand(() =>
        {
            MicOn = !MicOn;
            
            // if(_micOn)
            //     _speechService?.SpeakAsync("Aesir is listening.");
        });
        
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        
        LoadApplicationStateAsync();
    }
    
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if(message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            if(_appState.ChatSession?.Id != _appState.SelectedChatSessionId)
                Dispatcher.UIThread.InvokeAsync(LoadChatSessionAsync);
        }
    }
    
    public async Task LoadApplicationStateAsync()
    {
        // load the selected model... for now just 1
        _appState.SelectedModel = 
            (await _modelService.GetModelsAsync()).FirstOrDefault(m => m.IsChatModel);
        SelectedModelName = _appState.SelectedModel?.Id!;
        
        // load chat session
        await LoadChatSessionAsync();
    }

    private async Task LoadChatSessionAsync()
    {
        var currentId = _appState.SelectedChatSessionId;
        if (currentId == null)
        {
            ConversationStarted = false;
            _appState.ChatSession = new AesirChatSession();
        }
        else
        {
            ConversationStarted = true;
            _appState.ChatSession = await _chatHistoryService.GetChatSessionAsync(currentId.Value);
        }
        
        ConversationMessages.Clear();
        foreach(var message in _appState.ChatSession!.GetMessages())
        {
            MessageViewModel? messageViewModel = null;
            switch (message.Role)
            {
                case "user":
                    messageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
                
                    messageViewModel!.SetMessage(message);
                    ConversationMessages.Add(messageViewModel);
                    break;
                case "assistant":
                    messageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
                
                    messageViewModel!.SetMessage(message);
                    ConversationMessages.Add(messageViewModel);
                    break;
                case "system":
                    messageViewModel = Ioc.Default.GetService<SystemMessageViewModel>();
                
                    // always reset the system message
                    messageViewModel!.SetMessage(AesirChatMessage.NewSystemMessage());
                    ConversationMessages.Add(messageViewModel);
                    break;
            }
        }
    }
    
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
            await Task.CompletedTask;
        
        ConversationStarted = true;
        
        var message = AesirChatMessage.NewUserMessage(ChatMessage!);
        
        SendingChat = true;
        
        _appState.ChatSession!.AddMessage(message);
        
        var userMessageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
        userMessageViewModel!.SetMessage(message);
        ConversationMessages.Add(userMessageViewModel);
        
        var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
        ConversationMessages.Add(assistantMessageViewModel);
        
        ChatMessage = null;
        
        var chatRequest = AesirChatRequest.NewWithDefaults();
        chatRequest.Model = SelectedModelName;
        chatRequest.Conversation = _appState.ChatSession.Conversation;
        chatRequest.ChatSessionId = _appState.ChatSession.Id;
        chatRequest.Title = _appState.ChatSession.Title;
        chatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;
        
        var result = _chatService.ChatCompletionsStreamedAsync(chatRequest);
        var title = await assistantMessageViewModel!.SetStreamedMessageAsync(result).ConfigureAwait(false);
        
        _appState.ChatSession.Title = title;
        _appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());
        
        _appState.SelectedChatSessionId = _appState.ChatSession.Id;
        
        SendingChat = false;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // clean up stuff here
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
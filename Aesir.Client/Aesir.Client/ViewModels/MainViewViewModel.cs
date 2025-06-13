using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
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

namespace Aesir.Client.ViewModels;

public partial class MainViewViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>, IRecipient<FileUploadStatusMessage>, IRecipient<RegenerateMessageMessage>, IDisposable
{
    [ObservableProperty] 
    private bool _micOn;
    [ObservableProperty] 
    private bool _panelOpen;
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private bool _sendingChatOrProcessingFile;
    [ObservableProperty] 
    private bool _hasChatMessage;
    [ObservableProperty] 
    private bool _conversationStarted;
    [ObservableProperty] 
    private string? _selectedModelName = "Select a model";
    [ObservableProperty] 
    private FileToUploadViewModel _selectedFile;
    [ObservableProperty] 
    private bool _selectedFileEnabled = true;
    
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
    
    private readonly ApplicationState _appState;
    private readonly ISpeechService? _speechService;
    private readonly IChatService _chatService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IModelService _modelService;
   
    public MainViewViewModel(
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
        
        SelectedFile = new FileToUploadViewModel
        {
            IsActive = true
        };
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        
        _ = LoadApplicationStateAsync();
    }
    
    public void Receive(PropertyChangedMessage<Guid?> message)
    {
        if(message.PropertyName == nameof(ApplicationState.SelectedChatSessionId))
        {
            if(_appState.ChatSession?.Id != _appState.SelectedChatSessionId)
                Dispatcher.UIThread.InvokeAsync(LoadChatSessionAsync);
        }
    }

    private async Task LoadApplicationStateAsync()
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

    private async Task SendUpdatedUserMessage(UserMessageViewModel userMessage, AssistantMessageViewModel assistantMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage.Message))
        {
            await Task.CompletedTask;
            return;
        }

        var messageToSend = AesirChatMessage.NewUserMessage(userMessage.Message);
        
        SendingChatOrProcessingFile = true;

        // Need to add the ability to update a chat message
        // _appState.ChatSession!.(messageToSend);
        // ConversationMessages.Add(messageToSend);
        
        
    }
    
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            await Task.CompletedTask;
            
            return;
        }
        
        ConversationStarted = true;
        
        var message = AesirChatMessage.NewUserMessage(ChatMessage);
        
        SendingChatOrProcessingFile = true;
        
        _appState.ChatSession!.AddMessage(message);
        
        var userMessageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
        userMessageViewModel!.SetMessage(message);
        ConversationMessages.Add(userMessageViewModel);
        
        var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
        ConversationMessages.Add(assistantMessageViewModel);
        
        ChatMessage = null;
        
        var chatRequest = AesirChatRequest.NewWithDefaults();
        chatRequest.Model = SelectedModelName!;
        chatRequest.Conversation = _appState.ChatSession.Conversation;
        chatRequest.ChatSessionId = _appState.ChatSession.Id;
        chatRequest.Title = _appState.ChatSession.Title;
        chatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;
        
        var result = _chatService.ChatCompletionsStreamedAsync(chatRequest);
        var title = await assistantMessageViewModel!.SetStreamedMessageAsync(result).ConfigureAwait(false);
        
        _appState.ChatSession.Title = title;
        _appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());
        
        _appState.SelectedChatSessionId = _appState.ChatSession.Id;
        
        SendingChatOrProcessingFile = false;
    }

    [RelayCommand]
    private async Task ShowFileSelectionAsync()
    {
        var topLevel = TopLevel.GetTopLevel(GetMainView());

        // Start async operation to open the dialog.
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Upload PDF",
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType("PDF Documents")
                {
                    Patterns = ["*.pdf"],
                    MimeTypes = ["application/pdf"],
                    AppleUniformTypeIdentifiers = ["com.adobe.pdf"]
                }
            ]
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].Path.LocalPath;
            WeakReferenceMessenger.Default.Send(new FileUploadRequestMessage()
            {
                FilePath = filePath
            });
            // // Open reading stream from the first file.
            // await using var stream = await files[0].OpenReadAsync();
            // using var streamReader = new StreamReader(stream);
            // // Reads all the content of file as a text.
            // var fileContent = await streamReader.ReadToEndAsync();
        }   
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
    
    private ContentControl? GetMainView()
    {
        switch (Application.Current?.ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                return desktop.MainWindow;
            case ISingleViewApplicationLifetime singleView:
                return singleView.MainView as ContentControl;
            default:
                throw new System.NotImplementedException();
        }
    }

    public void Receive(FileUploadStatusMessage message)
    {
        SelectedFileEnabled = !message.IsProcessing;
        SendingChatOrProcessingFile = message.IsProcessing;
    }

    public void Receive(RegenerateMessageMessage message)
    {
        var messageViewModel = message.Value;

        switch (messageViewModel)
        {
            case UserMessageViewModel userMessage:
                RegenerateMessage(userMessage);
                break;
            case AssistantMessageViewModel assistantMessage:
                RegenerateFromAssistantMessage(assistantMessage);
                break;
        }
    }

    private async void RegenerateMessage(UserMessageViewModel userMessageViewModel)
    {
        // Find the index of the user message in the conversation
        var messageIndex = ConversationMessages.IndexOf(userMessageViewModel);
        if (messageIndex == -1) return;
        
        // Remove all messages after this user message (including the assistant response)
        for (int i = ConversationMessages.Count - 1; i > messageIndex; i--)
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

    private async void RegenerateFromAssistantMessage(AssistantMessageViewModel assistantMessageViewModel)
    {
        // Find the index of the assistant message in the conversation
        var assistantIndex = ConversationMessages.IndexOf(assistantMessageViewModel);
        if (assistantIndex == -1 || assistantIndex == 0) return;

        // Find the preceding user message
        UserMessageViewModel? userMessage = null;
        for (int i = assistantIndex - 1; i >= 0; i--)
        {
            if (ConversationMessages[i] is UserMessageViewModel user)
            {
                userMessage = user;
                break;
            }
        }

        if (userMessage == null) return;

        // Remove the assistant message and all messages after it
        for (int i = ConversationMessages.Count - 1; i >= assistantIndex; i--)
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

    private async Task ResendUserMessage(UserMessageViewModel userMessageViewModel)
    {
        ConversationStarted = true;
        SendingChatOrProcessingFile = true;

        // Add the edited chat message to the session
        _appState.ChatSession!.AddMessage(AesirChatMessage.NewUserMessage(userMessageViewModel.Message));
        
        // Create new assistant message view model
        var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
        ConversationMessages.Add(assistantMessageViewModel);

        // Prepare the chat request
        var chatRequest = AesirChatRequest.NewWithDefaults();
        chatRequest.Model = SelectedModelName!;
        chatRequest.Conversation = _appState.ChatSession!.Conversation;
        chatRequest.ChatSessionId = _appState.ChatSession.Id;
        chatRequest.Title = _appState.ChatSession.Title;
        chatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;

        // Send the request and stream the response
        var result = _chatService.ChatCompletionsStreamedAsync(chatRequest);
        var title = await assistantMessageViewModel!.SetStreamedMessageAsync(result).ConfigureAwait(false);

        // Update the chat session
        _appState.ChatSession.Title = title;
        _appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());

        _appState.SelectedChatSessionId = _appState.ChatSession.Id;

        SendingChatOrProcessingFile = false;
    }
}
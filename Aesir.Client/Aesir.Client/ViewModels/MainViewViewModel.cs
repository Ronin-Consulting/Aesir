using System;
using System.Collections.Generic;
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

public partial class MainViewViewModel : ObservableRecipient, IRecipient<PropertyChangedMessage<Guid?>>, IRecipient<FileUploadStatusMessage>, IDisposable
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
    
    // Services
    private readonly ApplicationState _appState;
    private readonly ISpeechService? _speechService;
    private readonly IChatService _chatService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IModelService _modelService;
   
    private readonly ChatSessionManager _chatSessionManager;

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
        _chatSessionManager = new ChatSessionManager(chatHistoryService, chatService, appState);

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

    public void Receive(FileUploadStatusMessage message)
    {
        SelectedFileEnabled = !message.IsProcessing;
        SendingChatOrProcessingFile = message.IsProcessing;
    }

    private async Task LoadApplicationStateAsync()
    {
        await LoadSelectedModelAsync();
        await LoadChatSessionAsync();
    }

    private async Task LoadSelectedModelAsync()
    {
        var models = await _modelService.GetModelsAsync();
        _appState.SelectedModel = models.FirstOrDefault(m => m.IsChatModel);
        SelectedModelName = _appState.SelectedModel?.Id!;
    }

    private async Task LoadChatSessionAsync()
    {
        await _chatSessionManager.LoadChatSessionAsync();
        ConversationStarted = _appState.SelectedChatSessionId != null;

        await RefreshConversationMessagesAsync();
    }

    private async Task RefreshConversationMessagesAsync()
    {
        ConversationMessages.Clear();

        foreach(var message in _appState.ChatSession!.GetMessages())
        {
            await AddMessageToConversationAsync(message);
        }
    }

    private Task AddMessageToConversationAsync(AesirChatMessage message)
    {
        MessageViewModel? messageViewModel = null;

        switch (message.Role)
        {
            case "user":
                messageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
                messageViewModel!.SetMessage(message);
                break;

            case "assistant":
                messageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
                messageViewModel!.SetMessage(message);
                break;

            case "system":
                messageViewModel = Ioc.Default.GetService<SystemMessageViewModel>();
                // always reset the system message
                messageViewModel!.SetMessage(AesirChatMessage.NewSystemMessage());
                break;
        }

        if (messageViewModel != null)
        {
            ConversationMessages.Add(messageViewModel);
        }

        return Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        try
        {
            ConversationStarted = true;
            SendingChatOrProcessingFile = true;

            // Add user message to UI
            var userMessage = AesirChatMessage.NewUserMessage(ChatMessage!);
            await AddMessageToConversationAsync(userMessage);

            // Add placeholder for assistant response
            var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
            ConversationMessages.Add(assistantMessageViewModel);

            // Clear input field
            ChatMessage = null;

            // Process the chat request
            await _chatSessionManager.ProcessChatRequestAsync(SelectedModelName!, ConversationMessages);
        }
        catch (Exception ex)
        {
            // Handle exceptions - in a real app, you'd want to log this and show a user-friendly message
            System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
        }
        finally
        {
            SendingChatOrProcessingFile = false;
        }
    }

    [RelayCommand]
    private async Task ShowFileSelectionAsync()
    {
        var files = await OpenPdfFilePickerAsync();

        if (files.Count >= 1)
        {
            RequestFileUpload(files[0].Path.LocalPath);
        }   
    }

    private async Task<IReadOnlyList<IStorageFile>> OpenPdfFilePickerAsync()
    {
        var topLevel = TopLevel.GetTopLevel(GetMainView());
        if (topLevel == null) return Array.Empty<IStorageFile>();

        return await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
    }

    private void RequestFileUpload(string filePath)
    {
        WeakReferenceMessenger.Default.Send(new FileUploadRequestMessage()
        {
            FilePath = filePath
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unregister messaging
            IsActive = false;

            // Dispose any managed resources if needed
            (_speechService as IDisposable)?.Dispose();
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
}

// ChatSessionManager class to handle chat session operations.
// For now, keeping it in same file to keep close to usage.
internal class ChatSessionManager(
    IChatHistoryService chatHistoryService,
    IChatService chatService,
    ApplicationState appState)
{
    public async Task LoadChatSessionAsync()
    {
        var currentId = appState.SelectedChatSessionId;
        if (currentId == null)
        {
            appState.ChatSession = new AesirChatSession();
            return;
        }

        appState.ChatSession = await chatHistoryService.GetChatSessionAsync(currentId.Value);
    }

    public async Task<string> ProcessChatRequestAsync(string modelName, ObservableCollection<MessageViewModel?> conversationMessages)
    {
        var message = AesirChatMessage.NewUserMessage(conversationMessages.Last(m => m is UserMessageViewModel)?.GetAesirChatMessage().Content!);
        appState.ChatSession!.AddMessage(message);

        var chatRequest = AesirChatRequest.NewWithDefaults();
        chatRequest.Model = modelName;
        chatRequest.Conversation = appState.ChatSession.Conversation;
        chatRequest.ChatSessionId = appState.ChatSession.Id;
        chatRequest.Title = appState.ChatSession.Title;
        chatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;

        var result = chatService.ChatCompletionsStreamedAsync(chatRequest);
        var assistantMessageViewModel = conversationMessages.Last(m => m is AssistantMessageViewModel) as AssistantMessageViewModel;
        var title = await assistantMessageViewModel!.SetStreamedMessageAsync(result).ConfigureAwait(false);

        appState.ChatSession.Title = title;
        appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());

        appState.SelectedChatSessionId = appState.ChatSession.Id;

        return title;
    }
}
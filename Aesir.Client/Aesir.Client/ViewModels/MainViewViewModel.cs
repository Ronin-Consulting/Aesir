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
using Microsoft.Extensions.Logging;

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
    [ObservableProperty]
    private string? _errorMessage;
    
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
    private readonly IChatSessionManager _chatSessionManager;
    private readonly IModelService _modelService;
    private readonly ILogger<MainViewViewModel> _logger;
    private readonly IDialogService _dialogService;

    public MainViewViewModel(
        ApplicationState appState,
        ISpeechService speechService,
        IChatSessionManager chatSessionManager,
        IModelService modelService,
        ILogger<MainViewViewModel> logger,
        IDialogService dialogService,
        FileToUploadViewModel fileToUploadViewModel)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _speechService = speechService;
        _chatSessionManager = chatSessionManager ?? throw new ArgumentNullException(nameof(chatSessionManager));
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        ToggleChatHistory = new RelayCommand(() => PanelOpen = !PanelOpen);
        ToggleNewChat = new RelayCommand(ExecuteNewChat);
        ToggleMicrophone = new RelayCommand(ExecuteToggleMicrophone);
        
        SelectedFile = fileToUploadViewModel;
        SelectedFile.IsActive = true;
    }

    private void ExecuteNewChat()
    {
        try
        {
            _appState.SelectedChatSessionId = null;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start new chat");
            ErrorMessage = "Failed to start new chat. Please try again.";
        }
    }

    private async void ExecuteToggleMicrophone()
    {
        try
        {
            MicOn = !MicOn;
            ErrorMessage = null;
            
            if (MicOn && _speechService != null)
            {
                await _speechService.SpeakAsync("Aesir is listening.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle microphone");
            ErrorMessage = "Failed to toggle microphone. Please try again.";
            MicOn = false;
        }
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

    public void Receive(RegenerateMessageMessage message)
    {
        var messageViewModel = message.Value;

        switch (messageViewModel)
        {
            case UserMessageViewModel userMessage:
                _ = RegenerateMessageAsync(userMessage);
                break;
            case AssistantMessageViewModel assistantMessage:
                _ = RegenerateFromAssistantMessageAsync(assistantMessage);
                break;
        }
    }

    private async Task LoadApplicationStateAsync()
    {
        await LoadSelectedModelAsync();
        await LoadChatSessionAsync();
    }

    private async Task LoadSelectedModelAsync()
    {
        try
        {
            var models = await _modelService.GetModelsAsync();
            _appState.SelectedModel = models.FirstOrDefault(m => m.IsChatModel);
            SelectedModelName = _appState.SelectedModel?.Id ?? "No model available";
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load models");
            ErrorMessage = "Failed to load available models. Please check your connection.";
            SelectedModelName = "Error loading models";
        }
    }

    private async Task LoadChatSessionAsync()
    {
        try
        {
            await _chatSessionManager.LoadChatSessionAsync();
            ConversationStarted = _appState.SelectedChatSessionId != null;
            await RefreshConversationMessagesAsync();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat session");
            ErrorMessage = "Failed to load chat session. Please try again.";
        }
    }

    private async Task RefreshConversationMessagesAsync()
    {
        try
        {
            ConversationMessages.Clear();

            if (_appState.ChatSession != null)
            {
                const int batchSize = 10;
                var messages = _appState.ChatSession.GetMessages().ToList();
                
                // Process messages in batches to avoid UI blocking
                for (var i = 0; i < messages.Count; i += batchSize)
                {
                    var batch = messages.Skip(i).Take(batchSize);
                    
                    foreach (var message in batch)
                    {
                        await AddMessageToConversationAsync(message);
                    }
                    
                    // Allow UI thread to breathe between batches
                    if (i + batchSize < messages.Count)
                    {
                        await Task.Delay(1); // Small delay to prevent UI freezing
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh conversation messages");
            ErrorMessage = "Failed to load conversation messages.";
        }
    }

    private async Task AddMessageToConversationAsync(AesirChatMessage message)
    {
        MessageViewModel? messageViewModel = null;

        switch (message.Role)
        {
            case "user":
                messageViewModel = Ioc.Default.GetService<UserMessageViewModel>();
                if (messageViewModel != null)
                    await messageViewModel.SetMessage(message);
                break;

            case "assistant":
                messageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
                if (messageViewModel != null)
                    await messageViewModel.SetMessage(message);
                break;

            case "system":
                messageViewModel = Ioc.Default.GetService<SystemMessageViewModel>();
                if (messageViewModel != null)
                    // always reset the system message
                    await messageViewModel.SetMessage(AesirChatMessage.NewSystemMessage());
                break;
        }

        if (messageViewModel != null)
        {
            ConversationMessages.Add(messageViewModel);
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatMessage))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedModelName) || SelectedModelName == "Select a model")
        {
            ErrorMessage = "Please select a model before sending a message.";
            return;
        }

        var currentMessage = ChatMessage;
        ChatMessage = null;
        ErrorMessage = null;

        try
        {
            ConversationStarted = true;
            SendingChatOrProcessingFile = true;

            // Add user message to UI
            var userMessage = AesirChatMessage.NewUserMessage(currentMessage);
            
            if (SelectedFile.IsVisible)
            {
                userMessage.AddFile(SelectedFile.FileName);
                SelectedFile.ClearFile();
            }
            
            await AddMessageToConversationAsync(userMessage);

            // Add placeholder for assistant response
            var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
            if (assistantMessageViewModel == null)
            {
                throw new InvalidOperationException("Could not resolve AssistantMessageViewModel");
            }
            ConversationMessages.Add(assistantMessageViewModel);

            // Process the chat request
            await _chatSessionManager.ProcessChatRequestAsync(SelectedModelName, ConversationMessages);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when sending message");
            ErrorMessage = "Invalid input. Please check your message and try again.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation when sending message");
            ErrorMessage = "Unable to send message. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message");
            ErrorMessage = "An unexpected error occurred. Please try again.";
        }
        finally
        {
            SendingChatOrProcessingFile = false;
        }
    }

    [RelayCommand]
    private async Task ShowFileSelectionAsync()
    {
        try
        {
            var files = await OpenPdfFilePickerAsync();

            if (files.Count >= 1)
            {
                SelectedFile.SetConversationId(_appState.ChatSession!.Conversation.Id);
                RequestFileUpload(files[0].Path.LocalPath);
                ErrorMessage = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file selection dialog");
            ErrorMessage = "Failed to open file selection dialog. Please try again.";
        }
    }

    private async Task<IReadOnlyList<IStorageFile>> OpenPdfFilePickerAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(GetTopLevelControl());
            if (topLevel?.StorageProvider == null) 
            {
                _logger.LogWarning("Storage provider not available");
                return Array.Empty<IStorageFile>();
            }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open PDF file picker");
            return Array.Empty<IStorageFile>();
        }
    }

    private void RequestFileUpload(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("Attempted to upload file with empty path");
                ErrorMessage = "Invalid file path.";
                return;
            }

            WeakReferenceMessenger.Default.Send(new FileUploadRequestMessage()
            {
                ConversationId = _appState.ChatSession?.Conversation.Id,
                FilePath = filePath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request file upload for path: {FilePath}", filePath);
            ErrorMessage = "Failed to upload file. Please try again.";
        }
    }

    private async Task RegenerateMessageAsync(UserMessageViewModel userMessageViewModel)
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

    private async Task RegenerateFromAssistantMessageAsync(AssistantMessageViewModel assistantMessageViewModel)
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
        if (string.IsNullOrWhiteSpace(SelectedModelName) || SelectedModelName == "Select a model")
        {
            ErrorMessage = "Please select a model before sending a message.";
            return;
        }
        
        ConversationStarted = true;
        SendingChatOrProcessingFile = true;

        // Create new assistant message view model
        var assistantMessageViewModel = Ioc.Default.GetService<AssistantMessageViewModel>();
        ConversationMessages.Add(assistantMessageViewModel);
        
        await _chatSessionManager.ProcessChatRequestAsync(SelectedModelName, ConversationMessages);
        
        SendingChatOrProcessingFile = false;
    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Unregister messaging
                IsActive = false;

                // Dispose any managed resources if needed
                if (_speechService is IDisposable disposableSpeechService)
                {
                    disposableSpeechService.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    private ContentControl? GetTopLevelControl()
    {
        try
        {
            return Application.Current?.ApplicationLifetime switch
            {
                IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
                ISingleViewApplicationLifetime singleView => singleView.MainView as ContentControl,
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top level control");
            return null;
        }
    }

}
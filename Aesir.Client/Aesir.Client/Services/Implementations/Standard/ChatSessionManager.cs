using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Client.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ChatSessionManager : IChatSessionManager
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IChatService _chatService;
    private readonly ApplicationState _appState;
    private readonly ILogger<ChatSessionManager> _logger;

    public ChatSessionManager(
        IChatHistoryService chatHistoryService,
        IChatService chatService,
        ApplicationState appState,
        ILogger<ChatSessionManager> logger)
    {
        _chatHistoryService = chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LoadChatSessionAsync()
    {
        try
        {
            var currentId = _appState.SelectedChatSessionId;
            if (currentId == null)
            {
                _appState.ChatSession = new AesirChatSession();
                return;
            }

            _appState.ChatSession = await _chatHistoryService.GetChatSessionAsync(currentId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat session with ID: {ChatSessionId}", _appState.SelectedChatSessionId);
            _appState.ChatSession = new AesirChatSession();
            throw;
        }
    }

    public async Task<string> ProcessChatRequestAsync(string modelName, ObservableCollection<MessageViewModel?> conversationMessages)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));

        if (conversationMessages == null)
            throw new ArgumentNullException(nameof(conversationMessages));

        if (_appState.ChatSession == null)
            throw new InvalidOperationException("Chat session is not loaded");

        try
        {
            var userMessage = conversationMessages.LastOrDefault(m => m is UserMessageViewModel)?.GetAesirChatMessage();
            if (userMessage?.Content == null)
                throw new InvalidOperationException("No user message found in conversation");

            var message = AesirChatMessage.NewUserMessage(userMessage.Content);
            _appState.ChatSession.AddMessage(message);

            var chatRequest = AesirChatRequest.NewWithDefaults();
            chatRequest.Model = modelName;
            chatRequest.Conversation = _appState.ChatSession.Conversation;
            chatRequest.ChatSessionId = _appState.ChatSession.Id;
            chatRequest.Title = _appState.ChatSession.Title;
            chatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;

            var result = _chatService.ChatCompletionsStreamedAsync(chatRequest);
            var assistantMessageViewModel = conversationMessages.LastOrDefault(m => m is AssistantMessageViewModel) as AssistantMessageViewModel;
            
            if (assistantMessageViewModel == null)
                throw new InvalidOperationException("No assistant message view model found");

            var title = await assistantMessageViewModel.SetStreamedMessageAsync(result).ConfigureAwait(false);

            _appState.ChatSession.Title = title;
            _appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());
            _appState.SelectedChatSessionId = _appState.ChatSession.Id;

            return title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat request for model: {ModelName}", modelName);
            throw;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Manages chat sessions including loading chat sessions and processing chat requests.
/// </summary>
public class ChatSessionManager(
    IChatHistoryService chatHistoryService,
    IChatService chatService,
    ApplicationState appState,
    ILogger<ChatSessionManager> logger)
    : IChatSessionManager
{
    /// <summary>
    /// Provides access to the chat history services for retrieving, managing,
    /// and updating chat session data. This service abstraction is responsible
    /// for handling chat session-related operations, such as fetching a
    /// specific chat session or managing session metadata.
    /// </summary>
    private readonly IChatHistoryService _chatHistoryService =
        chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));

    /// <summary>
    /// Represents the service responsible for handling chat-related operations such as processing chat requests,
    /// streaming chat completions, or interacting with backend chat functionality.
    /// </summary>
    /// <remarks>
    /// This service is used internally within the <see cref="ChatSessionManager"/> to manage and coordinate chat interactions
    /// between the application and the underlying chat infrastructure.
    /// </remarks>
    private readonly IChatService _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

    /// <summary>
    /// Represents the application's shared state used across the chat session manager.
    /// </summary>
    /// <remarks>
    /// This variable manages and provides access to the current state of the application,
    /// including active chat session details and selected chat session identifiers.
    /// It serves as the backbone for maintaining and retrieving relevant application data during runtime.
    /// </remarks>
    private readonly ApplicationState _appState = appState ?? throw new ArgumentNullException(nameof(appState));

    /// <summary>
    /// A logger instance used to log information, warnings, errors, and other messages
    /// for the <see cref="ChatSessionManager"/>. This is primarily utilized for debugging
    /// purposes, error tracking, and maintaining application logs.
    /// </summary>
    /// <remarks>
    /// Provides structured logging functionality, leveraging the Microsoft.Extensions.Logging
    /// framework.
    /// </remarks>
    private readonly ILogger<ChatSessionManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// Asynchronously loads the current chat session into the application state. If no chat session is
    /// selected, a new instance of the chat session is created. Handles any exceptions that occur
    /// during the loading process and logs the error.
    /// <returns>A task that represents the asynchronous operation.</returns>
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
            _logger.LogError(ex, "Failed to load chat session with ID: {ChatSessionId}",
                _appState.SelectedChatSessionId);
            _appState.ChatSession = new AesirChatSession();
            throw;
        }
    }

    /// <summary>
    /// Processes the chat request by utilizing the specified model and the provided conversation messages.
    /// </summary>
    /// <param name="modelName">The name of the model to be used for processing the chat request. It cannot be null or empty.</param>
    /// <param name="conversationMessages">The collection of messages representing the current conversation context. It cannot be null.</param>
    /// <returns>A string representing the result of the chat request processing.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="modelName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="conversationMessages"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the chat session is not loaded in the application state.</exception>
    /// <exception cref="Exception">Thrown when an unexpected error occurs during processing.</exception>
    public async Task<string> ProcessChatRequestAsync(string modelName,
        ObservableCollection<MessageViewModel?> conversationMessages)
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
            var assistantMessageViewModel =
                conversationMessages.LastOrDefault(m => m is AssistantMessageViewModel) as AssistantMessageViewModel;

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
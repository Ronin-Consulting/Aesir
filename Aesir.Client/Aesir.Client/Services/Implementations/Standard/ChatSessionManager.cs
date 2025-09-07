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
/// Provides functionality for managing chat sessions, including loading chat sessions and handling chat requests.
/// </summary>
public class ChatSessionManager(
    IChatHistoryService chatHistoryService,
    IChatService chatService,
    ApplicationState appState,
    ILogger<ChatSessionManager> logger)
    : IChatSessionManager
{
    /// <summary>
    /// Represents a service dedicated to accessing and managing historical chat data.
    /// Responsible for operations such as retrieving chat sessions, searching within chat
    /// histories, and performing updates or deletions of session data.
    /// </summary>
    private readonly IChatHistoryService _chatHistoryService =
        chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));

    /// <summary>
    /// Represents the service responsible for handling chat-related operations,
    /// including processing chat requests, streaming chat completions, and interfacing
    /// with the chat backend functionality.
    /// </summary>
    /// <remarks>
    /// This service is a core dependency for managing chat interactions, ensuring that
    /// request handling and response streaming are performed efficiently within the
    /// application.
    /// </remarks>
    private readonly IChatService _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

    /// <summary>
    /// Maintains and provides access to the current state of the application,
    /// including details related to the active chat session and selected identifiers.
    /// This variable acts as a shared resource for managing runtime application data
    /// across the chat session manager.
    /// </summary>
    private readonly ApplicationState _appState = appState ?? throw new ArgumentNullException(nameof(appState));

    /// <summary>
    /// Represents a logging mechanism for the <see cref="ChatSessionManager"/> class,
    /// facilitating the ability to record informational messages, warnings, errors,
    /// and other diagnostic logs. It is used to enhance traceability, debug issues,
    /// and monitor the application's behavior during chat session management processes.
    /// </summary>
    /// <remarks>
    /// This logger leverages the Microsoft.Extensions.Logging framework and provides
    /// structured, contextualized logging capabilities throughout the lifecycle of the
    /// chat session manager.
    /// </remarks>
    private readonly ILogger<ChatSessionManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// Asynchronously loads the current chat session into the application state. If no chat session is
    /// selected, creates a new instance of the chat session. Errors encountered during the loading
    /// process are logged and a new chat session is initialized in such cases.
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
    /// Asynchronously processes the chat request using the specified model and the provided conversation messages.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to be utilized for processing the chat request. Must not be null or empty.</param>
    /// <param name="conversationMessages">The collection of conversation messages that provide context for the chat request. Must not be null.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the chat request as a string.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="modelId"/> is null, empty, or consists only of whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="conversationMessages"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the chat session is not loaded in the current application state.</exception>
    /// <exception cref="Exception">Thrown if an error occurs while processing the chat request.</exception>
    public async Task<string> ProcessChatRequestAsync(Guid agentId,
        ObservableCollection<MessageViewModel?> conversationMessages)
    {
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

            var agentChatRequest = new AesirAgentChatRequestBase();
            agentChatRequest.AgentId = agentId;
            agentChatRequest.Conversation = _appState.ChatSession.Conversation;
            agentChatRequest.ChatSessionId = _appState.ChatSession.Id;
            agentChatRequest.Title = _appState.ChatSession.Title;
            agentChatRequest.ChatSessionUpdatedAt = DateTimeOffset.Now;
            agentChatRequest.User = "Unknown"; // TODO

            var result = _chatService.AgentChatCompletionsStreamedAsync(agentChatRequest);
            
            var assistantMessageViewModel =
                conversationMessages.LastOrDefault(m => m is AssistantMessageViewModel) as AssistantMessageViewModel;

            if (assistantMessageViewModel == null)
                throw new InvalidOperationException("No assistant message view model found");

            var title = await assistantMessageViewModel.SetStreamedMessageAsync(result);

            _appState.ChatSession.Title = title;
            _appState.ChatSession.AddMessage(assistantMessageViewModel.GetAesirChatMessage());
            _appState.SelectedChatSessionId = _appState.ChatSession.Id;

            return title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat request for agent: {AgentId}", agentId);
            throw;
        }
    }
}
using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Provides chat completion services using the OpenAI backend.
/// Handles both synchronous and streaming chat completions.
/// </summary>
/// <remarks>
/// This service requires OpenAI API credentials configured via the application settings.
/// It integrates with the chat history service to persist conversations.
/// Additionally, it supports document processing through the conversation document collection service,
/// allowing the AI to reference and utilize documents uploaded by users during chat sessions.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    private readonly IChatCompletionService _chatCompletionService;
    /// <summary>
    /// Service for managing document collections within conversations, providing functionality
    /// to search and retrieve information from documents uploaded during chat sessions.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="chatCompletionService">Service for chat completions.</param>
    /// <param name="chatHistoryService">Service for persisting and retrieving chat history.</param>
    /// <param name="conversationDocumentCollectionService">Service for accessing and searching documents uploaded within conversations.</param>
    public ChatService(
        ILogger<ChatService> logger,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService  conversationDocumentCollectionService)
        : base(logger, chatHistoryService, kernel)
    {
        _chatCompletionService = chatCompletionService;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
    }

    /// <summary>
    /// Generates a title for a chat session based on the user's first message.
    /// </summary>
    /// <param name="request">The chat request containing the user's message.</param>
    /// <returns>A concise title summarizing the user's message.</returns>
    protected override async Task<string> GetTitleForUserMessageAsync(AesirChatRequest request)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetTitleGenerationPrompt().Content);
        chatHistory.AddUserMessage(request.Conversation.Messages.Last().Content);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            Temperature = 0.2f
        };

        try
        {
            var completionResults = await _chatCompletionService.GetChatMessageContentsAsync(
                chatHistory,
                settings,
                _kernel);

            if (completionResults.Count > 0)
            {
                var content = completionResults[0].Content;
                return content?.Trim('"') ?? "Untitled conversation";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating title using Semantic Kernel IChatCompletionService");
        }

        return request.Conversation.Messages.Last().Content.Substring(0,
            Math.Min(50, request.Conversation.Messages.Last().Content.Length)) + "...";
    }

    /// <summary>
    /// Executes a chat completion request and returns the content and token usage.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <returns>A tuple containing the response content, prompt tokens, and completion tokens.</returns>
    protected override async Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(AesirChatRequest request)
    {
        var chatHistory = CreateChatHistory(request.Conversation.Messages);

        var settings = await CreatePromptExecutionSettingsAsync(request);

        var completionResults = await _chatCompletionService.GetChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel);

        if (completionResults.Count > 0)
        {
            var content = completionResults[0].Content ?? string.Empty;
            var promptTokens = 0;
            var completionTokens = 0;

            if (completionResults[0].Metadata != null &&
                completionResults[0].Metadata!.TryGetValue("Usage", out var usageObj) &&
                usageObj is ChatTokenUsage usage)
            {
                completionTokens = usage.OutputTokenCount;
                promptTokens = usage.InputTokenCount;
            }

            return (content, promptTokens, completionTokens);
        }

        return (string.Empty, 0, 0);
    }

    /// <summary>
    /// Executes a streaming chat completion request and returns content chunks with completion status.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <returns>An async enumerable of tuples containing content chunks and completion status.</returns>
    protected override async IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(AesirChatRequest request)
    {
        var chatHistory = CreateChatHistory(request.Conversation.Messages);

        var settings = await CreatePromptExecutionSettingsAsync(request);

        var streamingResults = _chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel);

        await foreach (var streamResult in streamingResults)
        {
            _logger.LogDebug("Received streaming content from Semantic Kernel: {Content}", streamResult.Content);

            var isComplete = streamResult is OpenAIStreamingChatMessageContent { FinishReason: ChatFinishReason.Stop };

            yield return (streamResult.Content ?? string.Empty, isComplete);
        }
    }

    /// <summary>
    /// Creates prompt execution settings for the OpenAI model based on the chat request parameters.
    /// If the conversation contains file attachments, configures the settings to utilize the document
    /// search functionality through function calling capabilities.
    /// </summary>
    /// <param name="request">The chat request containing model and parameter settings.</param>
    /// <returns>Configured OpenAI prompt execution settings.</returns>
    private async Task<OpenAIPromptExecutionSettings> CreatePromptExecutionSettingsAsync(AesirChatRequest request)
    {
        await Task.CompletedTask;
        
        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            Temperature = request.Temperature ?? 0.7f,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens
        };
        
        if (request.Conversation.Messages.Any(m => m.HasFile()))
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            var conversationId = request.Conversation.Id;
            
            var args = ConversationDocumentCollectionArgs.Default;
            args.AddConversationId(conversationId);
            
            _kernel.Plugins.Add(_conversationDocumentCollectionService.GetKernelPlugin(args));
        }
        
        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;

        return settings;
    }
    
    /// <summary>
    /// Creates a chat history from Aesir chat messages.
    /// </summary>
    /// <param name="messages">The messages to convert.</param>
    /// <returns>A chat history for use with the Semantic Kernel.</returns>
    private static ChatHistory CreateChatHistory(IEnumerable<AesirChatMessage> messages)
    {
        var chatHistory = new ChatHistory();

        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case "system":
                    chatHistory.AddSystemMessage(message.Content);
                    break;
                case "assistant":
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
                default:
                    chatHistory.AddUserMessage(message.Content);
                    break;
            }
        }

        return chatHistory;
    }
}

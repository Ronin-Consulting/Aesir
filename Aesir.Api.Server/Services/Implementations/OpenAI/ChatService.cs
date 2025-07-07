using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Implements chat completion functionality utilizing the OpenAI backend.
/// Provides support for generating both full and streaming responses to user input.
/// </summary>
/// <remarks>
/// This service relies on OpenAI API credentials configured in the application's settings.
/// It incorporates chat history management to persist user conversations, which can be leveraged
/// for contextually-aware responses. The service also integrates with a document collection service,
/// enabling the AI to process and use user-uploaded documents during conversations.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    /// <summary>
    /// Service responsible for handling chat completion operations, including generating
    /// responses to user messages and streaming chat completions, leveraging the OpenAI backend.
    /// </summary>
    private readonly IChatCompletionService _chatCompletionService;

    /// <summary>
    /// A service utilized within the chat framework to manage document collections related to conversations.
    /// Provides functionality to retrieve, process, or search through uploaded documents during a conversation.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    /// <summary>
    /// Provides access to prompt generation logic, enabling the creation and retrieval of prompts
    /// necessary for various AI-driven operations, including chat completions and title generation.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="chatCompletionService">Service for handling chat completions.</param>
    /// <param name="chatHistoryService">Service for persisting and managing chat history.</param>
    /// <param name="conversationDocumentCollectionService">Service for handling access and search functionalities for documents within conversations.</param>
    public ChatService(
        ILogger<ChatService> logger,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService conversationDocumentCollectionService)
        : base(logger, chatHistoryService, kernel)
    {
        _chatCompletionService = chatCompletionService;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
    }

    /// <summary>
    /// Generates a title for a chat session based on the user's first message.
    /// </summary>
    /// <param name="request">The chat request containing the user's message.</param>
    /// <returns>A task representing the asynchronous operation, returning a concise title summarizing the user's message.</returns>
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

        return request.Conversation.Messages.Last().Content[..Math.Min(50, request.Conversation.Messages.Last().Content.Length)] + "...";
    }

    /// <summary>
    /// Executes a chat completion request and returns the content and token usage.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <returns>A tuple containing the response content, prompt tokens, and completion tokens.</returns>
    protected override async Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
        AesirChatRequest request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request.Conversation.Messages);
        
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
    /// <param name="request">The chat request containing conversation details and prompt settings.</param>
    /// <returns>An asynchronous stream of tuples where each tuple contains a content chunk and a boolean indicating whether the completion is complete.</returns>
    protected override async IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(
        AesirChatRequest request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request.Conversation.Messages);

        var streamingResults = _chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel);

        await foreach (var streamResult in streamingResults)
        {
            //_logger.LogDebug("Received streaming content from Semantic Kernel: {Content}", streamResult.Content);

            var isComplete = streamResult is OpenAIStreamingChatMessageContent { FinishReason: ChatFinishReason.Stop };
            yield return (streamResult.Content ?? string.Empty, isComplete);
        }
    }

    /// <summary>
    /// Creates prompt execution settings for the OpenAI model based on the provided chat request.
    /// If the conversation includes file attachments, configures the settings to enable document search
    /// functionality leveraging function calling capabilities.
    /// </summary>
    /// <param name="request">The chat request containing parameters for model configuration, including model ID, temperature, top-p, and maximum tokens.</param>
    /// <returns>An instance of <see cref="OpenAIPromptExecutionSettings"/> configured with the details from the chat request.</returns>
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
            args.SetConversationId(conversationId);
            
            _kernel.Plugins.Add(_conversationDocumentCollectionService.GetKernelPlugin(args));
        }
        
        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;

        return settings;
    }

    /// <summary>
    /// Creates a chat history from a collection of Aesir chat messages.
    /// </summary>
    /// <param name="messages">The collection of Aesir chat messages to convert into a chat history.</param>
    /// <returns>A <see cref="ChatHistory"/> object representing the converted messages for use with the Semantic Kernel.</returns>
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

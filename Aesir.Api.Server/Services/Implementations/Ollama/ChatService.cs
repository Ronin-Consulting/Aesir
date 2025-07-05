using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Newtonsoft.Json;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Provides chat completion services using the Ollama backend.
/// Handles both synchronous and streaming chat completions.
/// </summary>
/// <remarks>
/// This service requires a running Ollama instance configured via the application settings.
/// It integrates with the chat history service to persist conversations.
/// Additionally, it supports document processing through the conversation document collection service,
/// allowing the AI to reference and utilize documents uploaded by users during chat sessions.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    private readonly OllamaApiClient _api;
    private readonly IChatCompletionService _chatCompletionService;
    /// <summary>
    /// Service for managing document collections within conversations, providing functionality
    /// to search and retrieve information from documents uploaded during chat sessions.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    private const int TitleGenerationMaxTokens = 250;
    private const float TitleGenerationTemperature = 0.2f;
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="api">Ollama API client for direct API access.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="chatCompletionService">Service for chat completions.</param>
    /// <param name="chatHistoryService">Service for persisting and retrieving chat history.</param>
    /// <param name="conversationDocumentCollectionService">Service for accessing and searching documents uploaded within conversations.</param>
    public ChatService(
        ILogger<ChatService> logger,
        OllamaApiClient api,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService  conversationDocumentCollectionService)
        : base(logger, chatHistoryService, kernel)
    {
        _api = api;
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
        if (request.Conversation.Messages.Count > 2)
            throw new InvalidOperationException("This operation should only be used when user first creates completion.");

        var requestOptions = new RequestOptions()
        {
            NumPredict = TitleGenerationMaxTokens,
            Temperature = TitleGenerationTemperature
        };

        var messages = new List<Message>
        {
            new Message(ChatRole.System, PromptProvider.GetTitleGenerationPrompt().Content),
            new Message(ChatRole.User, request.Conversation.Messages.Last().Content)
        };

        var ollamaRequest = new ChatRequest()
        {
            Model = request.Model,
            Stream = false,
            Options = requestOptions,
            Messages = messages
        };
        
        var message = AesirChatMessage.NewAssistantMessage("");
        await foreach (var completion in _api.ChatAsync(ollamaRequest))
        {
            message.Content += completion!.Message.Content;
        }

        return message.Content.Trim('"');
    }

    /// <summary>
    /// Executes a chat completion request and returns the content and token usage.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <returns>A tuple containing the response content, prompt tokens, and completion tokens.</returns>
    protected override async Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(AesirChatRequest request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request);
        
        var results = await _chatCompletionService.GetChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel
        );

        var content = string.Empty;
        int promptTokens = 0;
        int completionTokens = 0;
        
        foreach (var completion in results)
        {
            _logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));
            
            content += completion.Content ?? string.Empty;

            if (completion.InnerContent is not ChatDoneResponseStream { Done: true } doneCompletion) continue;
            
            completionTokens = doneCompletion.EvalCount;
            promptTokens = doneCompletion.PromptEvalCount;
        }

        return (content, promptTokens, completionTokens);
    }

    /// <summary>
    /// Executes a streaming chat completion request and returns content chunks with completion status.
    /// </summary>
    /// <param name="request">The chat request to process.</param>
    /// <returns>An async enumerable of tuples containing content chunks and completion status.</returns>
    protected override async IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(AesirChatRequest request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request);

        var results = _chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel
        );

        await foreach (var completion in results)
        {
            _logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));

            var isComplete = completion.InnerContent is ChatDoneResponseStream { Done: true };
            yield return (completion.Content ?? string.Empty, isComplete);
        }
    }

    /// <summary>
    /// Creates prompt execution settings for the Ollama model based on the chat request parameters.
    /// If the conversation contains file attachments, configures the settings to utilize the document
    /// search functionality through function calling capabilities.
    /// </summary>
    /// <param name="request">The chat request containing model and parameter settings.</param>
    /// <returns>Configured Ollama prompt execution settings.</returns>
    private async Task<OllamaPromptExecutionSettings> CreatePromptExecutionSettingsAsync(AesirChatRequest request)
    {
        await Task.CompletedTask;
        
        var settings = new OllamaPromptExecutionSettings
        {
            ModelId = request.Model
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
    /// Creates a chat history from an Aesir chat request for use with the Semantic Kernel.
    /// </summary>
    /// <param name="request">The chat request containing conversation messages.</param>
    /// <returns>A chat history for use with Semantic Kernel chat completions.</returns>
    private static ChatHistory CreateChatHistory(AesirChatRequest request)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddRange(request.Conversation.Messages.Select(ConvertToSemanticKernelMessage));
        return chatHistory;
    }

    /// <summary>
    /// Converts an Aesir chat message to a Semantic Kernel compatible message format.
    /// </summary>
    /// <param name="message">The Aesir chat message to convert.</param>
    /// <returns>A Semantic Kernel compatible chat message content.</returns>
    private static ChatMessageContent ConvertToSemanticKernelMessage(AesirChatMessage message)
    {
        var role = message.Role switch
        {
            "system" => AuthorRole.System,
            "assistant" => AuthorRole.Assistant,
            _ => AuthorRole.User
        };
        
        return new ChatMessageContent(role, message.GetContentWithoutFileName());
    }
}

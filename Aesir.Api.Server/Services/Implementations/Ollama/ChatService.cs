using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Data;
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
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    private readonly OllamaApiClient _api;
    private readonly IChatCompletionService _chatCompletionService;
    
    private const int TitleGenerationMaxTokens = 250;
    private const float TitleGenerationTemperature = 0.2f;
    private const string TitleGenerationSystemPrompt = "You are an AI designed to summarize user messages for display as concise list items. Your task is to take a user's chat message and shorten it into a brief, clear summary that retains the original meaning. Focus on capturing the key idea or intent, omitting unnecessary details, filler words, or repetition. The output should be succinct, natural, and suitable for a list format, ideally no longer than 5-10 words. If the message is already short, adjust it minimally to fit a list-item style.\nInput: A user's chat message\n\nOutput: A shortened version of the message as a list item\nExample:\nInput: \"I'm really excited about the new project launch happening next week, it's going to be amazing!\"\nOutput: \"Excited for next week's amazing project launch!\"";
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="api">Ollama API client for direct API access.</param>
    /// <param name="vectorStoreTextSearch">Vector store for semantic search.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="chatCompletionService">Service for chat completions.</param>
    /// <param name="chatHistoryService">Service for persisting and retrieving chat history.</param>
    public ChatService(
        ILogger<ChatService> logger,
        OllamaApiClient api,
        VectorStoreTextSearch<AesirTextData<Guid>> vectorStoreTextSearch,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService)
        : base(logger, chatHistoryService, kernel)
    {
        _api = api;
        _chatCompletionService = chatCompletionService;
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
            new Message(ChatRole.System, TitleGenerationSystemPrompt),
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
        var settings = CreatePromptExecutionSettings(request);
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
        var settings = CreatePromptExecutionSettings(request);
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

    private static OllamaPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequest request)
    {
        var settings = new OllamaPromptExecutionSettings
        {
            ModelId = request.Model,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        
        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;

        return settings;
    }

    private static ChatHistory CreateChatHistory(AesirChatRequest request)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddRange(request.Conversation.Messages.Select(ConvertToSemanticKernelMessage));
        return chatHistory;
    }

    private static ChatMessageContent ConvertToSemanticKernelMessage(dynamic message)
    {
        var role = message.Role switch
        {
            "system" => AuthorRole.System,
            "assistant" => AuthorRole.Assistant,
            _ => AuthorRole.User
        };
        
        return new ChatMessageContent(role, message.Content);
    }
}

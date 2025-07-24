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
/// Represents a chat service implementation utilizing OpenAI for processing user input and generating responses.
/// Extends functionality from the base chat service to support additional features and integrations.
/// </summary>
/// <remarks>
/// This class facilitates chat-based conversations, allowing for streamlined integration with the OpenAI backend.
/// It supports full and streaming response capabilities and leverages chat history for context awareness.
/// The service also has the ability to process user-uploaded documents to provide more informed responses.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    /// <summary>
    /// Instance of the chat completion service used to manage chat-related operations, such as
    /// generating responses to user inputs and facilitating real-time chat completion processes.
    /// </summary>
    private readonly IChatCompletionService _chatCompletionService;

    /// <summary>
    /// Service used for managing document collections associated with conversations,
    /// enabling functionality such as retrieving, processing, and searching through
    /// conversation-specific documents.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    /// <summary>
    /// Provides an implementation of prompt generation functionalities used for constructing
    /// and accessing prompts essential to AI-driven workflows, such as chat completion and
    /// other conversational AI operations.
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

        return request.Conversation.Messages.Last()
            .Content[..Math.Min(50, request.Conversation.Messages.Last().Content.Length)] + "...";
    }

    /// <summary>
    /// Executes a chat completion request and retrieves the response content, including token usage details.
    /// </summary>
    /// <param name="request">The chat request containing the required data for processing the chat completion action.</param>
    /// <returns>A task representing the asynchronous operation, containing a tuple with the response content, the number of prompt tokens, and the number of completion tokens.</returns>
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
    /// Executes a streaming chat completion request and yields content chunks with their completion statuses.
    /// </summary>
    /// <param name="request">The chat request containing conversations, messages, and prompt settings.</param>
    /// <returns>An asynchronous stream of tuples, where each tuple includes a content chunk, a boolean indicating whether the system is "thinking," and a boolean indicating if completion is final.</returns>
    protected override async IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
        ExecuteStreamingChatCompletionAsync(
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
            yield return (streamResult.Content ?? string.Empty, false, isComplete);
        }
    }

    /// <summary>
    /// Creates prompt execution settings for the OpenAI model based on the provided chat request.
    /// Configures the settings with parameters such as model ID, token limits, and optionally
    /// adjusts parameters like temperature or top-p based on the request. Enables document search
    /// capabilities if the conversation contains file attachments.
    /// </summary>
    /// <param name="request">The chat request containing details for configuring prompt execution, such as model ID, temperature, top-p, maximum tokens, and conversation metadata.</param>
    /// <returns>A task representing the async operation, with a result of <see cref="OpenAIPromptExecutionSettings"/> configured based on the provided request.</returns>
    private async Task<OpenAIPromptExecutionSettings> CreatePromptExecutionSettingsAsync(AesirChatRequest request)
    {
        await Task.CompletedTask;

        var systemPromptVariables = new Dictionary<string, object>
        {
            ["currentDateTime"] = request.ClientDateTime,
            ["webSearchtoolsEnabled"] = false,
            ["docSearchToolsEnabled"] = false
        };
        
        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            MaxTokens = request.MaxTokens
        };

        var globalPluginsExist = _kernel.Plugins.Count > 0;
        if (globalPluginsExist)
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
            
            // for now web search at some point we use tool name
            systemPromptVariables["webSearchtoolsEnabled"] = true;
        }

        if (request.Conversation.Messages.Any(m => m.HasFile()))
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            var conversationId = request.Conversation.Id;

            var args = ConversationDocumentCollectionArgs.Default;
            args.SetConversationId(conversationId);

            _kernel.Plugins.Add(_conversationDocumentCollectionService.GetKernelPlugin(args));
            
            systemPromptVariables["docSearchToolsEnabled"] = true;
        }

        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;

        RenderSystemPrompt(request.Conversation, systemPromptVariables);
        
        return settings;
    }

    /// <summary>
    /// Creates a chat history from a collection of Aesir chat messages.
    /// </summary>
    /// <param name="messages">The collection of Aesir chat messages to convert into a chat history.</param>
    /// <returns>A <see cref="ChatHistory"/> object representing the chat history derived from the provided messages.</returns>
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
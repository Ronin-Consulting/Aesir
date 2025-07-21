using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// An implementation of a chat service built on the Ollama backend.
/// Facilitates advanced chat completion features, including both synchronous and streaming modes of operation.
/// </summary>
/// <remarks>
/// This class extends the base chat service to provide enhanced functionalities tailored to the Ollama platform.
/// It integrates with multiple components including API clients, chat history tracking, and contextual document collections
/// to deliver intelligent and context-aware responses. The experimental designation indicates that its features and APIs
/// are subject to change.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    /// <summary>
    /// Handles communication with the Ollama API for executing chat-related operations,
    /// including sending requests and receiving synchronous or streaming responses.
    /// </summary>
    private readonly OllamaApiClient _api;

    /// <summary>
    /// Handles chat completion functionalities, supporting both synchronous processing
    /// and streaming of chat messages by interacting with the defined backend service.
    /// </summary>
    private readonly IChatCompletionService _chatCompletionService;

    /// <summary>
    /// Service responsible for managing collections of documents related to conversations.
    /// Supports operations such as retrieval, search, and integration of document collections
    /// into conversation workflows for enhanced dialogue-based interactions.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    /// <summary>
    /// Indicates whether the "thinking" mode is enabled for chat interactions.
    /// When set to true, the service utilizes an extended processing mode to deliver
    /// intermediate responses or signal ongoing operations in chat sessions.
    /// </summary>
    private readonly bool _enableThinking;

    /// <summary>
    /// Defines the maximum number of tokens that can be used to generate a title
    /// for a chat completion. This constant sets a limit on the token usage for
    /// creating succinct and relevant titles based on the conversational context.
    /// </summary>
    private const int TitleGenerationMaxTokens = 250;

    /// <summary>
    /// Defines the temperature setting for generating titles, influencing the randomness and
    /// creativity of the model's output. A lower value results in more focused and deterministic
    /// responses, while a higher value allows for increased variability and creativity.
    /// </summary>
    private const float TitleGenerationTemperature = 0.2f;

    /// <summary>
    /// Represents the prompt provider used for generating structured prompts in chat services.
    /// This variable supplies predefined templates for interactions, including system prompts
    /// and title generation prompts used in conversational AI contexts.
    /// </summary>
    private static readonly DefaultPromptProvider PromptProvider = new();

    /// <summary>
    /// Provides implementation for chat service operations, using Semantic Kernel and Ollama API.
    /// </summary>
    public ChatService(
        ILogger<ChatService> logger,
        OllamaApiClient api,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService conversationDocumentCollectionService,
        bool enableThinking = false)
        : base(logger, chatHistoryService, kernel)
    {
        _enableThinking = enableThinking;
        _api = api;
        _chatCompletionService = chatCompletionService;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
        _enableThinking = enableThinking;
    }

    /// <summary>
    /// Generates a title for a chat session based on the user's first message.
    /// </summary>
    /// <param name="request">The chat request containing the conversation details and user's message.</param>
    /// <returns>A concise title summarizing the user's message.</returns>
    protected override async Task<string> GetTitleForUserMessageAsync(AesirChatRequest request)
    {
        if (request.Conversation.Messages.Count > 2)
            throw new InvalidOperationException(
                "This operation should only be used when user first creates completion.");

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
            Messages = messages,
            Think = false
        };

        var message = AesirChatMessage.NewAssistantMessage("");
        await foreach (var completion in _api.ChatAsync(ollamaRequest))
        {
            message.Content += completion!.Message.Content;
        }

        var result = message.Content.Trim('"');
        return result.Length > 50 ? $"{result[..47]}..." : result;
    }

    /// <summary>
    /// Executes a chat completion request and returns the content and token usage.
    /// </summary>
    /// <param name="request">An instance of <see cref="AesirChatRequest"/> representing the details of the chat request.</param>
    /// <returns>A tuple containing the response content as a string, the number of prompt tokens used as an integer, and the number of completion tokens used as an integer.</returns>
    protected override async Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
        AesirChatRequest request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request);

        var results = await _chatCompletionService.GetChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel
        );

        var content = string.Empty;
        var promptTokens = 0;
        var completionTokens = 0;

        foreach (var completion in results)
        {
            //_logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));

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
    /// <param name="request">The chat request containing the necessary data for processing the chat completion.</param>
    /// <returns>An asynchronous enumerable of tuples where each tuple includes a content chunk, a boolean indicating if the system is still processing, and a boolean indicating if the completion is finalized.</returns>
    protected override async IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
        ExecuteStreamingChatCompletionAsync(AesirChatRequest request)
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
            //_logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));

            var isComplete = completion.InnerContent is ChatDoneResponseStream { Done: true };

            var isThinking = false;
            string? content = completion.Content;
            if (completion.InnerContent is ChatResponseStream chatResponseStream)
            {
                isThinking = chatResponseStream.Message.Thinking != null;

                if (isThinking) content = chatResponseStream.Message.Thinking;
            }

            yield return (content ?? string.Empty, isThinking, isComplete);
        }
    }

    /// <summary>
    /// Creates prompt execution settings for the Ollama model based on the provided chat request parameters.
    /// Configures additional settings, such as model behavior and document search capabilities,
    /// depending on the request's properties.
    /// </summary>
    /// <param name="request">The chat request containing model, token limits, temperature, and other parameter settings.</param>
    /// <returns>A task that represents the asynchronous operation, containing the configured Ollama prompt execution settings.</returns>
    private async Task<OllamaPromptExecutionSettings> CreatePromptExecutionSettingsAsync(AesirChatRequest request)
    {
        await Task.CompletedTask;
        
        var systemPromptVariables = new Dictionary<string, object>
        {
            ["currentDateTime"] = request.ClientDateTime,
            ["toolsEnabled"] = false
        };

        var settings = new OllamaPromptExecutionSettings
        {
            ModelId = request.Model,
            NumPredict = request.MaxTokens ?? 8192,
            ExtensionData = new Dictionary<string, object>()
        };

        if (_enableThinking)
            settings.ExtensionData.Add("think", true);

        if (request.Conversation.Messages.Any(m => m.HasFile()))
        {
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            var conversationId = request.Conversation.Id;

            var args = ConversationDocumentCollectionArgs.Default;
            args.SetConversationId(conversationId);
            _kernel.Plugins.Add(_conversationDocumentCollectionService.GetKernelPlugin(args));

            systemPromptVariables["toolsEnabled"] = true;
        }

        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;

        RenderSystemPrompt(request.Conversation, systemPromptVariables);
        
        return settings;
    }

    /// <summary>
    /// Creates a chat history from an Aesir chat request for use with the Semantic Kernel.
    /// </summary>
    /// <param name="request">The chat request containing conversation messages.</param>
    /// <returns>A chat history constructed from the conversation messages within the provided chat request.</returns>
    private static ChatHistory CreateChatHistory(AesirChatRequest request)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddRange(request.Conversation.Messages.Select(ConvertToSemanticKernelMessage));
        return chatHistory;
    }

    /// <summary>
    /// Converts an <see cref="AesirChatMessage"/> to a Semantic Kernel compatible message format.
    /// </summary>
    /// <param name="message">The Aesir chat message to be converted.</param>
    /// <returns>A Semantic Kernel compatible chat message content.</returns>
    private static ChatMessageContent ConvertToSemanticKernelMessage(AesirChatMessage message)
    {
        var role = message.Role switch
        {
            "system" => AuthorRole.System,
            "assistant" => AuthorRole.Assistant,
            _ => AuthorRole.User
        };

        return new ChatMessageContent(role, message.GetContentWithFileName());
    }
}
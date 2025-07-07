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
/// Represents a chat service implementation utilizing the Ollama backend.
/// Provides functionality for chat completions, including synchronous and streaming operations.
/// </summary>
/// <remarks>
/// This class builds upon the base chat service implementation to offer extended capabilities specific to the Ollama system.
/// It leverages an external API client for processing, integrates with chat history for conversation tracking,
/// and interacts with document collection services for enriched contextual responses in chat sessions.
/// The service is marked as experimental and may undergo changes in future iterations.
/// </remarks>
[Experimental("SKEXP0070")]
public class ChatService : BaseChatService
{
    /// <summary>
    /// Client for communicating with the Ollama backend, enabling chat interactions
    /// and facilitating API requests for chat completions, including synchronous
    /// and streaming responses.
    /// </summary>
    private readonly OllamaApiClient _api;

    /// <summary>
    /// Provides functionality for executing chat completion tasks using a defined backend service,
    /// including synchronous and streaming chat message processing.
    /// </summary>
    private readonly IChatCompletionService _chatCompletionService;

    /// <summary>
    /// Provides functionality for managing and interfacing with document collections associated with conversations,
    /// enabling operations such as document retrieval, search, and interaction within the context of a dialogue.
    /// </summary>
    private readonly IConversationDocumentCollectionService _conversationDocumentCollectionService;

    /// <summary>
    /// The maximum number of tokens allowed for generating a title during a chat completion.
    /// This limit controls the token budget used when creating a concise and meaningful title
    /// based on the user's input and the context of the conversation.
    /// </summary>
    private const int TitleGenerationMaxTokens = 250;

    /// <summary>
    /// Temperature setting used during title generation to adjust the randomness in the model's output.
    /// A lower value makes the output more deterministic, while a higher value introduces more variation.
    /// </summary>
    private const float TitleGenerationTemperature = 0.2f;

    /// <summary>
    /// Provides the default implementation for generating prompts used in chat completion services.
    /// Used to define and supply structured prompts for interactions and title generation in conversations.
    /// </summary>
    private static readonly DefaultPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="api">Ollama API client for direct API access.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="chatCompletionService">Service for generating chat completions.</param>
    /// <param name="chatHistoryService">Service for managing chat history persistence and retrieval.</param>
    /// <param name="conversationDocumentCollectionService">Service for handling and querying documents within conversations.</param>
    public ChatService(
        ILogger<ChatService> logger,
        OllamaApiClient api,
        Kernel kernel,
        IChatCompletionService chatCompletionService,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService conversationDocumentCollectionService)
        : base(logger, chatHistoryService, kernel)
    {
        _api = api;
        _chatCompletionService = chatCompletionService;
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
    }

    /// <summary>
    /// Generates a title for a chat session based on the user's first message.
    /// </summary>
    /// <param name="request">The chat request containing the conversation details and user's message.</param>
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

        var result = message.Content.Trim('"');
        return result.Length > 50 ? $"{result[..47]}..." : result;
    }

    /// <summary>
    /// Executes a chat completion request and returns the content and token usage.
    /// </summary>
    /// <param name="request">An instance of <see cref="AesirChatRequest"/> representing the chat request details.</param>
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
        int promptTokens = 0;
        int completionTokens = 0;
        
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
    /// <param name="request">The chat request to process.</param>
    /// <returns>An asynchronous enumerable of tuples containing content chunks and a boolean indicating the completion status.</returns>
    protected override async IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(
        AesirChatRequest request)
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
        
        return new ChatMessageContent(role, message.GetContentWithFileName());
    }
}

using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.OpenAI.Stopgap;
using Aesir.Modules.Inference.Services;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace Aesir.Modules.Inference.OpenAI.Services;

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
    /// Service used for managing document collections associated with conversations,
    /// enabling functionality such as retrieving, processing, and searching through
    /// conversation-specific documents.
    /// This service is optional and will be null if no embedding engine is configured.
    /// </summary>
    private readonly IConversationDocumentCollectionService? _conversationDocumentCollectionService;

    /// <summary>
    /// Service used for managing global document collections (including project documents),
    /// enabling RAG search functionality for project-scoped documents.
    /// This service is optional and will be null if no embedding engine is configured.
    /// </summary>
    private readonly IGlobalDocumentCollectionService? _globalDocumentCollectionService;

    /// <summary>
    /// Service responsible for managing and orchestrating plugins within the kernel, enabling the
    /// integration of additional functionalities or extensions into the core behavior of the system.
    /// </summary>
    private readonly IKernelPluginService _kernelPluginService;

    /// <summary>
    /// Factory for creating loggers, used to create typed loggers for child services.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Provides an implementation of prompt generation functionalities used for constructing
    /// and accessing prompts essential to AI-driven workflows, such as chat completion and
    /// other conversational AI operations.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = DefaultPromptProvider.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="loggerFactory">Factory for creating loggers for child services.</param>
    /// <param name="kernel">Semantic Kernel instance for AI operations.</param>
    /// <param name="kernelPluginService">Service for managing kernel plugins.</param>
    /// <param name="serviceProvider">Service provider for dependency resolution.</param>
    /// <param name="toolCallBroadcaster">Broadcaster for streaming tool call events to clients.</param>
    /// <param name="inferenceLogService">Service for persisting inference logs for observability.</param>
    /// <param name="inferenceEngineIdKey">The service key used to register this keyed service.</param>
    /// <param name="chatHistoryService">Service for persisting and managing chat history.</param>
    /// <param name="conversationDocumentCollectionService">Service for handling access and search functionalities for documents within conversations.</param>
    /// <param name="globalDocumentCollectionService">Service for handling global document collections including project documents.</param>
    public ChatService(
        ILogger<ChatService> logger,
        ILoggerFactory loggerFactory,
        Kernel kernel,
        IKernelPluginService kernelPluginService,
        IServiceProvider serviceProvider,
        IToolCallBroadcaster toolCallBroadcaster,
        IInferenceLogService? inferenceLogService,
        string inferenceEngineIdKey,
        IChatHistoryService chatHistoryService,
        IConversationDocumentCollectionService? conversationDocumentCollectionService,
        IGlobalDocumentCollectionService? globalDocumentCollectionService)
        : base(logger, chatHistoryService, kernel, serviceProvider, toolCallBroadcaster, inferenceLogService, inferenceEngineIdKey)
    {
        _conversationDocumentCollectionService = conversationDocumentCollectionService;
        _globalDocumentCollectionService = globalDocumentCollectionService;
        _kernelPluginService = kernelPluginService;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Generates a title for a chat session based on the user's first message.
    /// </summary>
    /// <param name="request">The chat request containing the user's message.</param>
    /// <returns>A task representing the asynchronous operation, returning a concise title summarizing the user's message.</returns>
    protected override async Task<string> GetTitleForUserMessageAsync(AesirChatRequestBase request)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetTitleGenerationPrompt().Content);
        chatHistory.AddUserMessage(request.Conversation.Messages.Last().Content);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model, // this is ignored and according to the repo authors not meant to change the model,
                                     // despite our attempt at submitted a PR
            Temperature = 0.2f
        };

        var chatCompletionService = GetChatCompletionService(request.Model);

        try
        {
            var completionResults = await chatCompletionService.GetChatMessageContentsAsync(
                chatHistory,
                settings,
                _kernel);

            if (completionResults.Count > 0)
            {
                var content = completionResults[0].Content;
                return content?.Trim('"') ?? "Untitled conversation";
            }
        }
        catch (HttpOperationException ex) when (ex.Message.Contains("temperature", StringComparison.OrdinalIgnoreCase))
        {
            // Some models (e.g., reasoning models) don't support temperature settings other than 1
            // Retry without temperature setting
            _logger.LogDebug("Model does not support temperature setting, retrying without it");

            var retrySettings = new OpenAIPromptExecutionSettings
            {
                ModelId = request.Model
            };

            try
            {
                var retryResults = await chatCompletionService.GetChatMessageContentsAsync(
                    chatHistory,
                    retrySettings,
                    _kernel);

                if (retryResults.Count > 0)
                {
                    var content = retryResults[0].Content;
                    return content?.Trim('"') ?? "Untitled conversation";
                }
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Error generating title on retry without temperature");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating title using Semantic Kernel IChatCompletionService");
        }

        var result = request.Conversation.Messages.Last()
            .Content.Trim('"');

        return result.Length > 50 ? $"{result[..47]}..." : result;
    }

    /// <summary>
    /// Executes a chat completion request and retrieves the response content, including token usage details.
    /// </summary>
    /// <param name="request">The chat request containing the required data for processing the chat completion action.</param>
    /// <returns>A task representing the asynchronous operation, containing a tuple with the response content, the number of prompt tokens, and the number of completion tokens.</returns>
    protected override async Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
        AesirChatRequestBase request)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request.Conversation.Messages);

        var chatCompletionService = GetChatCompletionService(request.Model);

        _kernel.Data.Add("ChatSessionId",request.ChatSessionId);
        _kernel.Data.Add("ConversationId",request.Conversation.Id);

        var completionResults = await chatCompletionService.GetChatMessageContentsAsync(
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
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed (e.g., timeout).</param>
    /// <returns>An asynchronous stream of tuples, where each tuple includes a content chunk, a boolean indicating whether the system is "thinking," and a boolean indicating if completion is final.</returns>
    protected override async IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
        ExecuteStreamingChatCompletionAsync(
            AesirChatRequestBase request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = await CreatePromptExecutionSettingsAsync(request);
        var chatHistory = CreateChatHistory(request.Conversation.Messages);

        var chatCompletionService = GetChatCompletionService(request.Model);

        _kernel.Data.Add("ChatSessionId", request.ChatSessionId);
        _kernel.Data.Add("ConversationId", request.Conversation.Id);

        // STOPGAP: Create reasoning content collector to intercept reasoning_content
        // from llama.cpp/TTRA responses. Remove when OpenAI SDK or SK adds native support.
        var reasoningCollectorLogger = _loggerFactory.CreateLogger<ReasoningContentCollector_Stopgap>();
        await using var reasoningCollector = new ReasoningContentCollector_Stopgap(reasoningCollectorLogger);
        ReasoningContentCollector_Stopgap.StoreInKernel(_kernel, reasoningCollector);

        try
        {
            // STOPGAP: Enable AsyncLocal scope so HTTP handler can access collector
            using (reasoningCollector.CreateScope())
            {
                var streamingResults = chatCompletionService.GetStreamingChatMessageContentsAsync(
                    chatHistory,
                    settings,
                    _kernel,
                    cancellationToken);

                // STOPGAP: Merge reasoning content with regular content
                await foreach (var result in MergeReasoningAndContent_Stopgap(streamingResults, reasoningCollector).WithCancellation(cancellationToken))
                {
                    yield return result;
                }
            }
        }
        finally
        {
            ReasoningContentCollector_Stopgap.RemoveFromKernel(_kernel);
        }
    }

    /// <summary>
    /// STOPGAP: Merges reasoning content from the HTTP interceptor with regular content from SK.
    /// Remove when native SDK support is available.
    /// </summary>
    private async IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
        MergeReasoningAndContent_Stopgap(
            IAsyncEnumerable<StreamingChatMessageContent> streamingResults,
            ReasoningContentCollector_Stopgap reasoningCollector)
    {
        // Drain any reasoning chunks that arrived before first content
        while (reasoningCollector.Reader.TryRead(out var chunk))
        {
            _logger.LogDebug("[Stopgap] Yielding pre-content reasoning chunk: {Content}",
                chunk.Content.Length > 50 ? chunk.Content[..50] + "..." : chunk.Content);
            yield return (chunk.Content, chunk.IsThinking, false);
        }

        await foreach (var streamResult in streamingResults)
        {
            // Check for reasoning chunks between content chunks
            while (reasoningCollector.Reader.TryRead(out var chunk))
            {
                _logger.LogDebug("[Stopgap] Yielding reasoning chunk: {Content}",
                    chunk.Content.Length > 50 ? chunk.Content[..50] + "..." : chunk.Content);
                yield return (chunk.Content, chunk.IsThinking, false);
            }

            var isComplete = streamResult is OpenAIStreamingChatMessageContent
                { FinishReason: ChatFinishReason.Stop };

            // Only yield non-empty content chunks
            if (!string.IsNullOrEmpty(streamResult.Content))
            {
                yield return (streamResult.Content, false, isComplete);
            }
            else if (isComplete)
            {
                // Still yield completion signal even if content is empty
                yield return (string.Empty, false, true);
            }
        }

        // Drain any remaining reasoning chunks
        while (reasoningCollector.Reader.TryRead(out var chunk))
        {
            _logger.LogDebug("[Stopgap] Yielding post-content reasoning chunk: {Content}",
                chunk.Content.Length > 50 ? chunk.Content[..50] + "..." : chunk.Content);
            yield return (chunk.Content, chunk.IsThinking, false);
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
    private async Task<OpenAIPromptExecutionSettings> CreatePromptExecutionSettingsAsync(AesirChatRequestBase request)
    {
        var settingsBuilderLogger = _loggerFactory.CreateLogger<OpenAiPromptExecutionSettingsBuilder>();
        var promptExecutionSettingsBuilder = new OpenAiPromptExecutionSettingsBuilder(
            _kernel, _conversationDocumentCollectionService, _globalDocumentCollectionService, _kernelPluginService, settingsBuilderLogger);

        var results =
            await promptExecutionSettingsBuilder.BuildAsync(request);

        RenderSystemPrompt(request.Conversation, results.SystemPromptVariables, request.ChatPromptPersona, request.ChatCustomPromptContent, request.ProjectInstructions);

        return results.Settings;
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

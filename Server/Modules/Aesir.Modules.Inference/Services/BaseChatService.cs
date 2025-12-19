using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Aesir.Common.Models;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Models;
using Aesir.Modules.Logging.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Serves as an abstract base class for managing chat services, providing essential functionalities
/// such as chat completions, chat history persistence, and integration with a semantic kernel.
/// </summary>
/// <param name="logger">The logger instance used for recording application events and debugging information.</param>
/// <param name="chatHistoryService">The service responsible for persisting and retrieving chat session histories.</param>
/// <param name="kernel">The semantic kernel utilized for processing chat completion requests and generating AI responses.</param>
/// <param name="toolCallBroadcaster">The broadcaster for streaming tool call events to clients.</param>
/// <param name="inferenceLogService">The service for persisting inference operation logs for observability.</param>
/// <param name="inferenceEngineIdKey">The service key used to register this keyed service.</param>
[Experimental("SKEXP0070")]
public abstract class BaseChatService : IChatService
{
    /// <summary>
    /// A protected instance of <see cref="ILogger"/> utilized for logging operations within the service.
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// A protected instance of <see cref="IChatHistoryService"/> used for managing operations related to chat history.
    /// </summary>
    protected readonly IChatHistoryService _chatHistoryService;

    /// <summary>
    /// A protected instance of the <see cref="Kernel"/> class used for managing AI-driven operations within the service.
    /// </summary>
    protected readonly Kernel _kernel;

    /// <summary>
    /// A protected string representing the key used to identify the inference engine.
    /// </summary>
    private readonly string _inferenceEngineIdKey;

    /// <summary>
    /// A protected instance of <see cref="IServiceProvider"/> utilized to resolve service dependencies dynamically.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The tool call broadcaster for streaming tool call events to clients.
    /// </summary>
    private readonly IToolCallBroadcaster _toolCallBroadcaster;

    /// <summary>
    /// The inference log service for persisting observability data.
    /// </summary>
    private readonly IInferenceLogService? _inferenceLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseChatService"/> class.
    /// </summary>
    protected BaseChatService(
        ILogger logger,
        IChatHistoryService chatHistoryService,
        Kernel kernel,
        IServiceProvider serviceProvider,
        IToolCallBroadcaster toolCallBroadcaster,
        IInferenceLogService? inferenceLogService,
        string inferenceEngineIdKey)
    {
        _logger = logger;
        _chatHistoryService = chatHistoryService;
        _kernel = kernel;
        _serviceProvider = serviceProvider;
        _toolCallBroadcaster = toolCallBroadcaster;
        _inferenceLogService = inferenceLogService;
        _inferenceEngineIdKey = inferenceEngineIdKey;
    }

    /// <summary>
    /// Retrieves an instance of the chat completion service based on the specified model identifier.
    /// </summary>
    /// <param name="modelId">
    /// The ID of the model to use for chat completion. This parameter determines which chat completion service will be provided.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IChatCompletionService"/> corresponding to the specified model ID.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the chat completion service cannot be retrieved for the specified model ID.
    /// </exception>
    protected IChatCompletionService GetChatCompletionService(string modelId)
    {
        var factory = _serviceProvider.GetKeyedService<IChatCompletionServiceFactory>(_inferenceEngineIdKey);

        return factory.GetChatCompletionService(modelId);
    }

    /// <summary>
    /// Processes a chat completion request and generates a response based on the provided input.
    /// </summary>
    /// <param name="request">
    /// The chat request containing the necessary information for generating a chat response, such as user input and conversation context.
    /// </param>
    /// <returns>
    /// An <see cref="AesirChatResult"/> instance containing the chat response, which includes details such as tokens used and the updated conversation context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
    /// </exception>
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequestBase request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        //request.SetClientDateTimeInSystemMessage();

        // Generate a new session ID if one isn't provided (new chat)
        request.ChatSessionId ??= Guid.NewGuid();

        var response = new AesirChatResult()
        {
            ChatSessionId = request.ChatSessionId,
            AesirConversation = request.Conversation,
            CompletionTokens = 0,
            PromptTokens = 0,
            TotalTokens = 0
        };

        var messageToSave = AesirChatMessage.NewAssistantMessage("");

        try
        {
            var (content, promptTokens, completionTokens) = await ExecuteChatCompletionAsync(request);

            messageToSave.Content = content;
            response.AesirConversation.Messages.Add(messageToSave);
            response.CompletionTokens = completionTokens;
            response.PromptTokens = promptTokens;
            response.TotalTokens = promptTokens + completionTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion");
            messageToSave.Content = "I apologize, but I encountered an error processing your request.";
            response.AesirConversation.Messages.Add(messageToSave);
        }

        var title = request.Title;
        // Generate title on first user message exchange (1 user + 1 assistant, ignoring system messages)
        var nonSystemMessageCount = request.Conversation.Messages.Count(m => m.Role != "system");
        if (nonSystemMessageCount == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }

        await PersistChatSessionAsync(request, response.AesirConversation, title);

        // Include the title in the response so clients can display it
        response.Title = title;

        return response;
    }

    /// <summary>
    /// Streams chat completions asynchronously based on the provided chat request.
    /// </summary>
    /// <param name="request">
    /// An instance of <see cref="AesirChatRequestBase"/> containing the chat request details, including conversation messages and metadata.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <see cref="AesirChatStreamedResultBase"/> representing incremental results of the chat completion process.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
    /// </exception>
    public async IAsyncEnumerable<AesirChatStreamedResultBase> ChatCompletionsStreamedAsync(AesirChatRequestBase request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        // Generate a new session ID if one isn't provided (new chat)
        request.ChatSessionId ??= Guid.NewGuid();

        var completionId = Guid.NewGuid().ToString();
        var messageToSave = AesirChatMessage.NewAssistantMessage("");

        // Use the existing title from request, but treat "title-not-set" as empty
        var title = request.Title;
        if (string.IsNullOrEmpty(title) || title == "title-not-set")
        {
            // Try to fetch existing title from database for existing sessions
            if (request.ChatSessionId.HasValue)
            {
                var existingSession = await _chatHistoryService.GetChatSessionAsync(request.ChatSessionId.Value);
                title = existingSession?.Title ?? string.Empty;
            }
        }

        var titleTask = Task.FromResult(title);
        // Generate title on first user message (1 non-system message before streaming adds assistant response)
        var nonSystemMessageCount = request.Conversation.Messages.Count(m => m.Role != "system");
        if (nonSystemMessageCount == 1 && string.IsNullOrEmpty(title))
        {
            titleTask = GetTitleForUserMessageAsync(request);
        }

        // Handle initialization errors without try/catch around yield
        IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>? streamingResults = null;

        bool initializationError = false;
        AesirChatMessage errorMessage =
            AesirChatMessage.NewAssistantMessage("I apologize, but I encountered an error processing your request.");

        // Create tool call broadcaster scope for this request and store it in Kernel.Data
        // This allows the ToolCallStreamingFilter to access it via the Kernel context
        await using var toolCallScope = _toolCallBroadcaster.CreateScope();
        _kernel.Data[ToolCallBroadcaster.KernelDataKey] = toolCallScope;

        // Create inference log collector for observability and store in Kernel.Data
        // This collects all tool calls and timing data for the inference log
        IInferenceLogCollector? logCollector = null;
        if (_inferenceLogService != null)
        {
            logCollector = new InferenceLogCollector();
            var userQuery = request.Conversation.Messages
                .LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
            Guid? conversationId = Guid.TryParse(request.Conversation.Id, out var parsedId) ? parsedId : null;
            logCollector.Start(request.ChatSessionId, conversationId, userQuery);
            InferenceLogCollector.StoreInKernel(_kernel, logCollector);
        }

        _logger.LogDebug("About to call ExecuteStreamingChatCompletionAsync");

        try
        {
            streamingResults = ExecuteStreamingChatCompletionAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing streaming chat completion");

            initializationError = true;
            request.Conversation.Messages.Add(errorMessage);
            title = await titleTask;
            await PersistChatSessionAsync(request, request.Conversation, title);

            // Log failed inference
            await PersistInferenceLogAsync(logCollector, null, ex.Message);
        }

        // Handle initialization errors outside the catch block
        if (initializationError)
        {
            title = await titleTask;

            yield return CreateErrorResult(completionId, request, errorMessage, title);
            yield break;
        }

        // Process streaming results with tool call merging
        if (streamingResults != null)
        {
            await foreach (var streamEvent in MergeToolCallsAndContentAsync(
                streamingResults, toolCallScope.Reader, completionId, request, title, titleTask, messageToSave))
            {
                // Update title if available
                if (title == request.Title && titleTask.IsCompleted)
                    title = await titleTask;

                yield return streamEvent;
            }

            request.Conversation.Messages.Add(messageToSave);

            // Ensure title generation completes before persisting
            // This prevents race condition where streaming finishes before title is ready
            title = await titleTask;

            await PersistChatSessionAsync(request, request.Conversation, title);

            // Log successful inference with assistant response
            await PersistInferenceLogAsync(logCollector, messageToSave.Content, null);
        }

        // Signal that no more tool calls will come and clean up Kernel.Data
        toolCallScope.Complete();
        _kernel.Data.Remove(ToolCallBroadcaster.KernelDataKey);
        InferenceLogCollector.RemoveFromKernel(_kernel);
    }

    /// <summary>
    /// Merges tool call events with text/thinking content into a single stream.
    /// Tool calls are checked non-blocking before each content chunk.
    /// </summary>
    private async IAsyncEnumerable<AesirChatStreamedResultBase> MergeToolCallsAndContentAsync(
        IAsyncEnumerable<(string content, bool isThinking, bool isComplete)> contentStream,
        ChannelReader<AesirToolCallInfo> toolCallReader,
        string completionId,
        AesirChatRequestBase request,
        string title,
        Task<string> titleTask,
        AesirChatMessage messageToSave)
    {
        // Collect tool calls to persist with the message, using Dictionary for deduplication by ToolCallId.
        // This prevents duplicate tool calls if the same event is received multiple times.
        var collectedToolCalls = new Dictionary<string, AesirToolCallInfo>();

        await foreach (var (content, isThinking, isComplete) in contentStream)
        {
            // Check for tool calls first (non-blocking) and yield any pending ones
            while (toolCallReader.TryRead(out var toolCall))
            {
                collectedToolCalls[toolCall.ToolCallId] = toolCall;
                yield return CreateToolCallResult(completionId, request, title, toolCall);
            }

            // Update title if ready
            if (title == request.Title && titleTask.IsCompleted)
                title = await titleTask;

            // Yield content chunk
            if (!string.IsNullOrEmpty(content))
            {
                var messageToSend = AesirChatMessage.NewAssistantMessage(string.Empty);

                if (isThinking)
                {
                    messageToSave.ThoughtsContent += content;
                    messageToSend.ThoughtsContent = content;
                }
                else
                {
                    messageToSave.Content += content;
                    messageToSend.Content = content;
                }

                yield return new AesirChatStreamedResult
                {
                    Id = completionId,
                    ChatSessionId = request.ChatSessionId,
                    ConversationId = request.Conversation.Id,
                    Delta = messageToSend,
                    Title = title,
                    IsThinking = isThinking,
                    EventType = isThinking ? StreamEventType.Thinking : StreamEventType.Text
                };
            }
        }

        // Drain any remaining tool calls after content stream completes
        while (toolCallReader.TryRead(out var toolCall))
        {
            collectedToolCalls[toolCall.ToolCallId] = toolCall;
            yield return CreateToolCallResult(completionId, request, title, toolCall);
        }

        // Persist collected tool calls with the message (deduplicated by ToolCallId)
        if (collectedToolCalls.Count > 0)
        {
            messageToSave.ToolCalls = collectedToolCalls.Values.ToList();
        }
    }

    /// <summary>
    /// Creates a streamed result for a tool call event.
    /// </summary>
    private static AesirChatStreamedResult CreateToolCallResult(
        string completionId,
        AesirChatRequestBase request,
        string title,
        AesirToolCallInfo toolCall)
    {
        return new AesirChatStreamedResult
        {
            Id = completionId,
            ChatSessionId = request.ChatSessionId,
            ConversationId = request.Conversation.Id,
            Delta = AesirChatMessage.NewAssistantMessage(string.Empty),
            Title = title,
            EventType = toolCall.Status == ToolCallStatus.Started
                ? StreamEventType.ToolCallStart
                : StreamEventType.ToolCallResult,
            ToolCall = toolCall
        };
    }

    /// <summary>
    /// Creates an error result for a streamed chat response with the specified details.
    /// </summary>
    /// <param name="completionId">
    /// The unique identifier for the request responsible for triggering the error.
    /// </param>
    /// <param name="request">
    /// The chat request object that contains metadata associated with the session or conversation.
    /// </param>
    /// <param name="errorMessage">
    /// The error message that specifies the details of the encountered issue.
    /// </param>
    /// <param name="title">
    /// The title providing a summary or context for the error.
    /// </param>
    /// <returns>
    /// An instance of <see cref="AesirChatStreamedResultBase"/> representing the error details,
    /// including identifiers and corresponding error message information.
    /// </returns>
    private static AesirChatStreamedResultBase CreateErrorResult(
        string completionId,
        AesirChatRequestBase request,
        AesirChatMessage errorMessage,
        string title)
    {
        return new AesirChatStreamedResult()
        {
            Id = completionId,
            ChatSessionId = request.ChatSessionId,
            ConversationId = request.Conversation.Id,
            Delta = errorMessage,
            Title = title
        };
    }

    /// <summary>
    /// Retrieves a title based on the user's message included in the specified chat request.
    /// </summary>
    /// <param name="request">
    /// The chat request containing the user's message and additional contextual details.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation, resulting in a string that serves as the title derived from the user's message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="request"/> is null.
    /// </exception>
    protected abstract Task<string> GetTitleForUserMessageAsync(AesirChatRequestBase request);

    /// <summary>
    /// Executes the process of generating a chat-based completion based on the provided request.
    /// </summary>
    /// <param name="request">
    /// The instance of <see cref="AesirChatRequestBase"/> containing the parameters and input for generating the chat response.
    /// </param>
    /// <returns>
    /// A tuple containing the generated response content as a string, the number of tokens used for the prompt, and the number of tokens generated in the completion.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
    /// </exception>
    protected abstract Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
        AesirChatRequestBase request);

    /// <summary>
    /// Executes a streaming chat completion based on the provided request, yielding incremental responses
    /// as a stream of content along with status indicators.
    /// </summary>
    /// <param name="request">
    /// The chat request containing user input details and conversation context necessary for generating
    /// the streamed chat responses.
    /// </param>
    /// <returns>
    /// An asynchronous enumerable of tuples, where each tuple consists of the generated content as a string,
    /// a boolean indicating if the system is processing ("isThinking"), and a boolean indicating if the stream
    /// is fully completed ("isComplete").
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the provided <paramref name="request"/> is null.
    /// </exception>
    protected abstract IAsyncEnumerable<(string content, bool isThinking, bool isComplete)>
        ExecuteStreamingChatCompletionAsync(AesirChatRequestBase request);

    /// <summary>
    /// Persists the chat session by saving or updating the session's details in the chat history service.
    /// </summary>
    /// <param name="request">
    /// The chat request containing information about the current session, including user and session details.
    /// </param>
    /// <param name="conversation">
    /// The conversation object that includes the collection of messages exchanged during the chat session.
    /// </param>
    /// <param name="title">
    /// The title of the chat session, used for identification and display purposes.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation of persisting the chat session.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <paramref name="request.ChatSessionId"/> is null.
    /// </exception>
    protected async Task PersistChatSessionAsync(AesirChatRequestBase request, AesirConversation conversation, string title)
    {
        await _chatHistoryService.UpsertChatSessionAsync(new AesirChatSession()
        {
            Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
            Title = title,
            Conversation = conversation,
            UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
            UserId = request.User
        });
    }

    /// <summary>
    /// Renders the system prompt within the conversation by applying the provided arguments to the system message template.
    /// If no system message exists in the conversation, a default one will be added.
    /// </summary>
    /// <param name="conversation">
    /// The conversation containing the messages, including the system message that needs to be rendered with the given arguments.
    /// </param>
    /// <param name="arguments">
    /// A dictionary of key-value pairs representing the variables to replace in the system message template.
    /// </param>
    protected void RenderSystemPrompt(AesirConversation conversation, Dictionary<string, object> arguments)
    {
        var systemPromptMessage = conversation.Messages.FirstOrDefault(m => m.Role == "system");

        // If no system message exists, add a default one
        if (systemPromptMessage == null)
        {
            systemPromptMessage = new AesirChatMessage
            {
                Role = "system",
                Content = "You are a helpful AI assistant."
            };
            conversation.Messages.Insert(0, systemPromptMessage);
        }

        var systemPromptTemplate = new PromptTemplate(
            systemPromptMessage.Content
        );

        systemPromptMessage.Content = systemPromptTemplate.Render(arguments);
    }

    /// <summary>
    /// Persists the inference log to the database for observability.
    /// </summary>
    /// <param name="collector">The inference log collector (may be null if logging is disabled).</param>
    /// <param name="assistantResponse">The assistant's response text (for successful completions).</param>
    /// <param name="errorMessage">The error message (for failed completions).</param>
    private async Task PersistInferenceLogAsync(
        IInferenceLogCollector? collector,
        string? assistantResponse,
        string? errorMessage)
    {
        if (collector == null || _inferenceLogService == null)
            return;

        try
        {
            AesirInferenceLog inferenceLog;

            if (!string.IsNullOrEmpty(errorMessage))
            {
                inferenceLog = collector.Fail(errorMessage);
            }
            else
            {
                inferenceLog = collector.Complete(assistantResponse);
            }

            await _inferenceLogService.LogInferenceAsync(inferenceLog);

            _logger.LogDebug("Persisted inference log {InferenceLogId} with {ToolCallCount} tool calls",
                inferenceLog.Id, inferenceLog.ToolCallCount);
        }
        catch (Exception ex)
        {
            // Don't fail the request if logging fails - just log the error
            _logger.LogError(ex, "Failed to persist inference log");
        }
    }
}

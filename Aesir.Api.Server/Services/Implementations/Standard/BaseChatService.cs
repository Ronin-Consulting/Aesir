using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Serves as an abstract base for implementing chat services with predefined functionality and utility methods.
/// </summary>
/// <param name="logger">The logging interface used for capturing and managing logs.</param>
/// <param name="chatHistoryService">The service for handling storage and retrieval of chat history.</param>
/// <param name="kernel">The semantic kernel powering AI-driven chat operations.</param>
[Experimental("SKEXP0070")]
public abstract class BaseChatService(
    ILogger logger,
    IChatHistoryService chatHistoryService,
    Kernel kernel)
    : IChatService
{
    /// <summary>
    /// Represents the logger instance for recording and managing log information.
    /// </summary>
    /// <remarks>
    /// Used to log errors, warnings, and informational messages for debugging and monitoring purposes
    /// within the chat service operations. Supports integration with various logging frameworks.
    /// </remarks>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// Represents the chat history service, which provides functionality for storing
    /// and managing chat sessions.
    /// </summary>
    /// <remarks>
    /// This instance is used to persist chat session data, support retrieval of previous
    /// sessions, and perform operations such as updating or deleting chat history records.
    /// </remarks>
    protected readonly IChatHistoryService _chatHistoryService = chatHistoryService;

    /// <summary>
    /// Represents an instance of the Microsoft.SemanticKernel Kernel, which is utilized to execute
    /// various functionalities, such as managing plugins, handling prompt execution settings,
    /// and processing chat completion tasks in the context of the chat service implementation.
    /// </summary>
    /// <remarks>
    /// This member is primarily used by methods for chat-related operations, such as message
    /// contents retrieval, streaming chat completion, and prompt execution settings creation.
    /// It serves as a central component for integrating AI functionalities and orchestrating the
    /// execution of tasks within the chat service ecosystem.
    /// </remarks>
    protected readonly Kernel _kernel = kernel;

    /// <summary>
    /// Processes a chat completion request and generates a response based on the provided input.
    /// </summary>
    /// <param name="request">
    /// The chat request containing the details required for generating a chat response, including the user input and conversation context.
    /// </param>
    /// <returns>
    /// An <see cref="AesirChatResult"/> object containing the response to the chat request, including tokens consumed and the conversation context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="request"/> is null.
    /// </exception>
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        var response = new AesirChatResult()
        {
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
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }

        await PersistChatSessionAsync(request, response.AesirConversation, title);

        return response;
    }

    /// <summary>
    /// Streams chat completion results based on the provided chat request asynchronously.
    /// </summary>
    /// <param name="request">An instance of <see cref="Aesir.Api.Server.Models.AesirChatRequest"/> containing the details of the chat request, including conversation messages and other metadata.</param>
    /// <returns>An asynchronous stream of <see cref="Aesir.Api.Server.Models.AesirChatStreamedResult"/> representing the streamed chat completion results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="request"/> parameter is null.</exception>
    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        var completionId = Guid.NewGuid().ToString();
        var messageToSave = AesirChatMessage.NewAssistantMessage("");

        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }

        // Handle initialization errors without try/catch around yield
        IAsyncEnumerable<(string content, bool isComplete)> streamingResults = null;

        bool initializationError = false;
        AesirChatMessage errorMessage = AesirChatMessage.NewAssistantMessage("I apologize, but I encountered an error processing your request.");

        try
        {
            streamingResults = ExecuteStreamingChatCompletionAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing streaming chat completion");

            initializationError = true;
            request.Conversation.Messages.Add(errorMessage);
            await PersistChatSessionAsync(request, request.Conversation, title);
        }

        // Handle initialization errors outside the catch block
        if (initializationError)
        {
            yield return CreateErrorResult(completionId, request, errorMessage, title);
            yield break;
        }

        // Process streaming results - only execute if no initialization error occurred
        if (streamingResults != null)
        {
            await foreach (var (content, _) in streamingResults)
            {
                //_logger.LogDebug("Received streaming content: {Content}", content);

                if (!string.IsNullOrEmpty(content))
                {
                    messageToSave.Content += content;

                    var messageToSend = AesirChatMessage.NewAssistantMessage(content);

                    yield return new AesirChatStreamedResult()
                    {
                        Id = completionId,
                        ChatSessionId = request.ChatSessionId,
                        ConversationId = request.Conversation.Id,
                        Delta = messageToSend,
                        Title = title
                    };
                }
            }
            
            request.Conversation.Messages.Add(messageToSave);
            await PersistChatSessionAsync(request, request.Conversation, title);
        }
    }

    /// <summary>
    /// Creates an error result for a streamed chat response.
    /// </summary>
    /// <param name="completionId">The unique identifier for the completion operation.</param>
    /// <param name="request">The chat request containing session and conversation details.</param>
    /// <param name="errorMessage">The error message to include in the result.</param>
    /// <param name="title">The descriptive title of the error message.</param>
    /// <returns>An instance of <see cref="AesirChatStreamedResult"/> containing the error details.</returns>
    private static AesirChatStreamedResult CreateErrorResult(
        string completionId,
        AesirChatRequest request,
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
    /// Retrieves a title based on the content of the user's message contained within the specified chat request.
    /// </summary>
    /// <param name="request">The chat request containing the user's message and associated context.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the title derived from the user's message.</returns>
    protected abstract Task<string> GetTitleForUserMessageAsync(AesirChatRequest request);

    /// <summary>
    /// Executes the process of generating a chat-based completion based on the provided request.
    /// </summary>
    /// <param name="request">The chat request containing parameters and input for generating the chat response.</param>
    /// <returns>A tuple containing the content of the response, the number of prompt tokens used, and the number of completion tokens generated.</returns>
    protected abstract Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(AesirChatRequest request);

    /// <summary>
    /// Executes a streaming chat completion based on the provided request. This abstract method is implemented
    /// by derived classes to process and return incremental chat responses as a stream.
    /// </summary>
    /// <param name="request">The chat request containing input data required for generating the streamed completion.</param>
    /// <returns>An asynchronous enumerable of tuples where each tuple contains a piece of content and a boolean indicating if the stream is complete.</returns>
    protected abstract IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(AesirChatRequest request);

    /// <summary>
    /// Persists the chat session by saving or updating the session details in the chat history service.
    /// </summary>
    /// <param name="request">The chat request containing information about the current chat session.</param>
    /// <param name="conversation">The conversation object representing the messages in the chat session.</param>
    /// <param name="title">The title of the chat session.</param>
    /// <returns>A task that represents the asynchronous operation of persisting the chat session.</returns>
    protected async Task PersistChatSessionAsync(AesirChatRequest request, AesirConversation conversation, string title)
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
}

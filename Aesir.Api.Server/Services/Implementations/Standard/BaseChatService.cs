using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Provides an abstract foundation for building chat service implementations with core utilities
/// for handling conversational AI, logging, and chat history management.
/// </summary>
/// <param name="logger">The interface for logging operations and diagnostics.</param>
/// <param name="chatHistoryService">The service for managing persistent storage and retrieval of chat histories.</param>
/// <param name="kernel">The semantic kernel utilized for AI-based chat completion and response generation.</param>
[Experimental("SKEXP0070")]
public abstract class BaseChatService(
    ILogger logger,
    IChatHistoryService chatHistoryService,
    Kernel kernel)
    : IChatService
{
    /// <summary>
    /// A protected instance of <see cref="ILogger"/> used for logging activities within the service.
    /// </summary>
    /// <remarks>
    /// Facilitates the recording of critical information, such as errors, warnings, and application events,
    /// to aid in troubleshooting, monitoring, and maintaining service operations.
    /// </remarks>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// Represents the chat history service used for managing and storing chat session data.
    /// </summary>
    /// <remarks>
    /// Facilitates operations such as persisting chat sessions, retrieving previous conversations,
    /// updating records, and managing the overall chat history lifecycle. Provides an abstraction
    /// for handling chat-related data across the application.
    /// </remarks>
    protected readonly IChatHistoryService _chatHistoryService = chatHistoryService;

    /// <summary>
    /// Represents an instance of the Microsoft.SemanticKernel Kernel utilized for managing and executing
    /// AI-driven functionalities within the chat service.
    /// </summary>
    /// <remarks>
    /// Provides core capabilities for tasks such as plugin management, prompt execution, and handling
    /// chat-related operations, serving as the foundational component for integrating AI features
    /// into the service workflow.
    /// </remarks>
    protected readonly Kernel _kernel = kernel;

    /// <summary>
    /// Processes a chat completion request and generates a response based on the provided input.
    /// </summary>
    /// <param name="request">
    /// The chat request containing the necessary information for generating a chat response, such as user input and conversation context.
    /// </param>
    /// <returns>
    /// An <see cref="AesirChatResult"/> instance containing the chat response, including details such as tokens used and the updated conversation context.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
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
    /// Streams chat completions asynchronously based on the provided chat request.
    /// </summary>
    /// <param name="request">
    /// An instance of <see cref="Aesir.Api.Server.Models.AesirChatRequest"/> containing the chat request details, such as the conversation messages and associated metadata.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <see cref="Aesir.Api.Server.Models.AesirChatStreamedResult"/> representing the incremental results of the chat completion process.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the <paramref name="request"/> parameter is null.
    /// </exception>
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
        IAsyncEnumerable<(string content, bool isThinking, bool isComplete)> streamingResults = null;

        bool initializationError = false;
        AesirChatMessage errorMessage =
            AesirChatMessage.NewAssistantMessage("I apologize, but I encountered an error processing your request.");

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
            await foreach (var (content, isThinking, isComplete) in streamingResults)
            {
                //_logger.LogDebug("Received streaming content: {Content}", content);

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

                    yield return new AesirChatStreamedResult()
                    {
                        Id = completionId,
                        ChatSessionId = request.ChatSessionId,
                        ConversationId = request.Conversation.Id,
                        Delta = messageToSend,
                        Title = title,
                        IsThinking = isThinking
                    };
                }
            }

            request.Conversation.Messages.Add(messageToSave);
            await PersistChatSessionAsync(request, request.Conversation, title);
        }
    }

    /// <summary>
    /// Creates an error result for a streamed chat response with the specified details.
    /// </summary>
    /// <param name="completionId">
    /// The unique identifier for the chat completion request that triggered the error.
    /// </param>
    /// <param name="request">
    /// The chat request object containing session and conversation-specific metadata.
    /// </param>
    /// <param name="errorMessage">
    /// The error message that provides details about the issue encountered.
    /// </param>
    /// <param name="title">
    /// The title summarizing the context or nature of the error.
    /// </param>
    /// <returns>
    /// An instance of <see cref="AesirChatStreamedResult"/> containing information about the error,
    /// including the identifiers and message details.
    /// </returns>
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
    /// Retrieves a title derived from the user's message contained in the specified chat request.
    /// </summary>
    /// <param name="request">
    /// The chat request containing the user's message and any associated contextual information.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation, with the task result containing the title generated from the user's message.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="request"/> is null.
    /// </exception>
    protected abstract Task<string> GetTitleForUserMessageAsync(AesirChatRequest request);

    /// <summary>
    /// Executes the process of generating a chat-based completion based on the provided request.
    /// </summary>
    /// <param name="request">
    /// The chat request containing parameters and input for generating the chat response.
    /// </param>
    /// <returns>
    /// A tuple containing the response content, the number of prompt tokens used, and the number of completion tokens generated.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the provided <paramref name="request"/> is null.
    /// </exception>
    protected abstract Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(
        AesirChatRequest request);

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
        ExecuteStreamingChatCompletionAsync(AesirChatRequest request);

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
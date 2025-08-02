using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Common.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

/// <summary>
/// Serves as an abstract base class for managing chat services, providing essential functionalities
/// such as chat completions, chat history persistence, and integration with a semantic kernel.
/// </summary>
/// <param name="logger">The logger instance used for recording application events and debugging information.</param>
/// <param name="chatHistoryService">The service responsible for persisting and retrieving chat session histories.</param>
/// <param name="kernel">The semantic kernel utilized for processing chat completion requests and generating AI responses.</param>
[Experimental("SKEXP0070")]
public abstract class BaseChatService(
    ILogger logger,
    IChatHistoryService chatHistoryService,
    Kernel kernel)
    : IChatService
{
    /// <summary>
    /// A protected instance of <see cref="ILogger"/> utilized for logging operations within the service.
    /// </summary>
    /// <remarks>
    /// Supports capturing and recording of events such as errors, exceptions, and other operational details,
    /// which aids in diagnostics, system monitoring, and application maintenance.
    /// </remarks>
    protected readonly ILogger _logger = logger;

    /// <summary>
    /// A protected instance of <see cref="IChatHistoryService"/> used for managing operations related to chat history.
    /// </summary>
    /// <remarks>
    /// Provides functionalities for persisting, retrieving, and managing chat session data.
    /// Ensures that chat history is accessible and up-to-date for various application components.
    /// </remarks>
    protected readonly IChatHistoryService _chatHistoryService = chatHistoryService;

    /// <summary>
    /// A protected instance of the <see cref="Kernel"/> class used for managing AI-driven operations within the service.
    /// </summary>
    /// <remarks>
    /// Serves as the core engine for enabling functionalities such as plugin execution, prompt handling,
    /// and supporting various AI-related workflows essential to the chat service's operations.
    /// </remarks>
    protected readonly Kernel _kernel = kernel;

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
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        //request.SetClientDateTimeInSystemMessage();

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
    /// An instance of <see cref="Aesir.Api.Server.Models.AesirChatRequest"/> containing the chat request details, including conversation messages and metadata.
    /// </param>
    /// <returns>
    /// An asynchronous stream of <see cref="Aesir.Api.Server.Models.AesirChatStreamedResult"/> representing incremental results of the chat completion process.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
    /// </exception>
    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        //request.SetClientDateTimeInSystemMessage();

        var completionId = Guid.NewGuid().ToString();
        var messageToSave = AesirChatMessage.NewAssistantMessage("");

        var title = request.Title;
        var titleTask = Task.FromResult(title);
        if (request.Conversation.Messages.Count == 2)
        {
            titleTask = GetTitleForUserMessageAsync(request);
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
            title = await titleTask;
            await PersistChatSessionAsync(request, request.Conversation, title);
        }

        // Handle initialization errors outside the catch block
        if (initializationError)
        {
            title = await titleTask;
            
            yield return CreateErrorResult(completionId, request, errorMessage, title);
            yield break;
        }

        // Process streaming results - only execute if no initialization error occurred
        if (streamingResults != null)
        {
            title = await titleTask;
            
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
    /// An instance of <see cref="AesirChatStreamedResult"/> representing the error details,
    /// including identifiers and corresponding error message information.
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
    protected abstract Task<string> GetTitleForUserMessageAsync(AesirChatRequest request);

    /// <summary>
    /// Executes the process of generating a chat-based completion based on the provided request.
    /// </summary>
    /// <param name="request">
    /// The instance of <see cref="AesirChatRequest"/> containing the parameters and input for generating the chat response.
    /// </param>
    /// <returns>
    /// A tuple containing the generated response content as a string, the number of tokens used for the prompt, and the number of tokens generated in the completion.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified <paramref name="request"/> is null.
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

    /// <summary>
    /// Renders the system prompt within the conversation by applying the provided arguments to the system message template.
    /// </summary>
    /// <param name="conversation">
    /// The conversation containing the messages, including the system message that needs to be rendered with the given arguments.
    /// </param>
    /// <param name="arguments">
    /// A dictionary of key-value pairs representing the variables to replace in the system message template.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there is no system message present in the provided conversation.
    /// </exception>
    protected void RenderSystemPrompt(AesirConversation conversation, Dictionary<string, object> arguments)
    {
        var systemPromptMessage = conversation.Messages.First(m => m.Role == "system");
        var systemPromptTemplate = new PromptTemplate(
            systemPromptMessage.Content
        );

        systemPromptMessage.Content = systemPromptTemplate.Render(arguments);
    }
}
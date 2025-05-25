using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;

namespace Aesir.Api.Server.Services.Implementations.Standard;

[Experimental("SKEXP0070")]
public abstract class BaseChatService : IChatService
{
    protected readonly ILogger _logger;

    protected readonly IChatHistoryService _chatHistoryService;

    protected readonly Kernel _kernel;

    protected BaseChatService(
        ILogger logger,
        IChatHistoryService chatHistoryService,
        Kernel kernel)
    {
        _logger = logger;
        _chatHistoryService = chatHistoryService;
        _kernel = kernel;
    }

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

    public IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        return ProcessStreamingChatCompletionAsync(request);
    }

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

    private async IAsyncEnumerable<AesirChatStreamedResult> ProcessStreamingChatCompletionAsync(AesirChatRequest request)
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
            await foreach (var (content, isComplete) in streamingResults)
            {
                _logger.LogDebug("Received streaming content: {Content}", content);

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

                if (isComplete)
                {
                    request.Conversation.Messages.Add(messageToSave);
                    await PersistChatSessionAsync(request, request.Conversation, title);
                }
            }
        }
    }

    protected abstract Task<string> GetTitleForUserMessageAsync(AesirChatRequest request);

    protected abstract Task<(string content, int promptTokens, int completionTokens)> ExecuteChatCompletionAsync(AesirChatRequest request);

    protected abstract IAsyncEnumerable<(string content, bool isComplete)> ExecuteStreamingChatCompletionAsync(AesirChatRequest request);

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

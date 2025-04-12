using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly OpenAIAPI _api;
    private readonly IChatHistoryService _chatHistoryService;

    public ChatService(
        ILogger<ChatService> logger,
        IConfiguration configuration,
        IChatHistoryService chatHistoryService)
    {
        _logger = logger;
        _chatHistoryService = chatHistoryService;
        var apiKey = configuration.GetValue<string>("Inference:OpenAI:ApiKey") ?? 
                    throw new InvalidOperationException("OpenAI API key is not configured");
        _api = new OpenAIAPI(apiKey);
    }

    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        try
        {
            var chatRequest = new ChatRequest
            {
                Model = request.Model,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Messages = request.Conversation.Messages.Select(m => new ChatMessage
                {
                    Role = m.Role switch
                    {
                        "system" => ChatMessageRole.System,
                        "assistant" => ChatMessageRole.Assistant,
                        _ => ChatMessageRole.User
                    },
                    Content = m.Content
                }).ToList()
            };

            var response = await _api.Chat.CreateChatCompletionAsync(chatRequest);
            
            var messageToSave = AesirChatMessage.NewAssistantMessage(response.Choices[0].Message.Content);
            
            var result = new AesirChatResult
            {
                AesirConversation = request.Conversation,
                CompletionTokens = response.Usage.CompletionTokens,
                PromptTokens = response.Usage.PromptTokens,
                TotalTokens = response.Usage.TotalTokens
            };
            
            result.AesirConversation.Messages.Add(messageToSave);
            
            var title = request.Title;
            if (request.Conversation.Messages.Count == 2)
            {
                title = await GetTitleForUserMessageAsync(request);
            }
            
            await _chatHistoryService.UpsertChatSessionAsync(new AesirChatSession
            {
                Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
                Title = title,
                Conversation = result.AesirConversation,
                UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
                UserId = request.User
            });
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI chat completion");
            throw;
        }
    }

    private async Task<string> GetTitleForUserMessageAsync(AesirChatRequest request)
    {
        try
        {
            var chatRequest = new ChatRequest
            {
                Model = request.Model,
                Temperature = 0.2,
                MaxTokens = 250,
                Messages = new List<ChatMessage>
                {
                    new()
                    {
                        Role = ChatMessageRole.System,
                        Content = "You are an AI designed to summarize user messages for display as concise list items. Your task is to take a user's chat message and shorten it into a brief, clear summary that retains the original meaning. Focus on capturing the key idea or intent, omitting unnecessary details, filler words, or repetition. The output should be succinct, natural, and suitable for a list format, ideally no longer than 5-10 words. If the message is already short, adjust it minimally to fit a list-item style.\nInput: A user's chat message\n\nOutput: A shortened version of the message as a list item\nExample:\nInput: \"I'm really excited about the new project launch happening next week, it's going to be amazing!\"\nOutput: \"Excited for next week's amazing project launch!\""
                    },
                    new()
                    {
                        Role = ChatMessageRole.User,
                        Content = request.Conversation.Messages.Last().Content
                    }
                }
            };

            var response = await _api.Chat.CreateChatCompletionAsync(chatRequest);
            return response.Choices[0].Message.Content.Trim('"');
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during title generation");
            return "New Chat";
        }
    }

    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        try
        {
            var chatRequest = new ChatRequest
            {
                Model = request.Model,
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                Messages = request.Conversation.Messages.Select(m => new ChatMessage
                {
                    Role = m.Role switch
                    {
                        "system" => ChatMessageRole.System,
                        "assistant" => ChatMessageRole.Assistant,
                        _ => ChatMessageRole.User
                    },
                    Content = m.Content
                }).ToList(),
                Stream = true
            };

            var completionId = Guid.NewGuid().ToString();
            var messageToSave = AesirChatMessage.NewAssistantMessage("");

            var title = request.Title;
            if (request.Conversation.Messages.Count == 2)
            {
                title = await GetTitleForUserMessageAsync(request);
            }

            var streamingResponse = _api.Chat.CreateChatCompletionAsync(chatRequest);
            
            await foreach (var response in streamingResponse)
            {
                var content = response.Choices[0].Message.Content;
                var messageToSend = AesirChatMessage.NewAssistantMessage(content);
                
                messageToSave.Content += content;
                
                if (response.Choices[0].FinishReason == "stop")
                {
                    request.Conversation.Messages.Add(messageToSave);
                    await _chatHistoryService.UpsertChatSessionAsync(new AesirChatSession
                    {
                        Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
                        Title = title,
                        Conversation = request.Conversation,
                        UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
                        UserId = request.User
                    });
                }
                
                yield return new AesirChatStreamedResult
                {
                    Id = completionId,
                    ChatSessionId = request.ChatSessionId,
                    ConversationId = request.Conversation.Id,
                    Delta = messageToSend,
                    Title = title
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI chat streaming");
            throw;
        }
    }
}

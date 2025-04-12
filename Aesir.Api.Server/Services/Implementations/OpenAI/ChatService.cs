using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using OpenAI;
using OpenAI.Chat;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ChatService(
    ILogger<ChatService> logger,
    OpenAIClient api,
    IChatHistoryService chatHistoryService)
    : IChatService
{
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        request.SetClientDateTimeInSystemMessage();
        
        var chatMessages = request.Conversation.Messages.Select(m =>
        {
            return m.Role switch
            {
                "system" => new ChatMessage(ChatMessageRole.System, m.Content),
                "assistant" => new ChatMessage(ChatMessageRole.Assistant, m.Content),
                _ => new ChatMessage(ChatMessageRole.User, m.Content)
            };
        }).ToList();

        var chatRequest = new ChatRequest
        {
            Model = request.Model,
            Messages = chatMessages
        };
        
        if (request.Temperature.HasValue)
            chatRequest.Temperature = (float)request.Temperature.Value;
        else if (request.TopP.HasValue)
            chatRequest.TopP = (float)request.TopP.Value;
        
        if (request.MaxTokens.HasValue)
            chatRequest.MaxTokens = request.MaxTokens.Value;
        
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        
        var messageToSave = AesirChatMessage.NewAssistantMessage(response.Choices[0].Message.Content);
        
        var result = new AesirChatResult
        {
            AesirConversation = request.Conversation,
            CompletionTokens = response.Usage.CompletionTokens,
            PromptTokens = response.Usage.PromptTokens,
            TotalTokens = response.Usage.TotalTokens,
            ChatSessionId = request.ChatSessionId
        };
        
        result.AesirConversation.Messages.Add(messageToSave);
        
        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        await chatHistoryService.UpsertChatSessionAsync(new AesirChatSession
        {
            Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
            Title = title,
            Conversation = result.AesirConversation,
            UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
            UserId = request.User
        });
        
        return result;
    }
    
    private async Task<string> GetTitleForUserMessageAsync(AesirChatRequest request)
    {
        if (request.Conversation.Messages.Count > 2)
            throw new InvalidOperationException("This operation should only be used when user first creates completion.");
        
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatMessageRole.System, "You are an AI designed to summarize user messages for display as concise list items. Your task is to take a user's chat message and shorten it into a brief, clear summary that retains the original meaning. Focus on capturing the key idea or intent, omitting unnecessary details, filler words, or repetition. The output should be succinct, natural, and suitable for a list format, ideally no longer than 5-10 words. If the message is already short, adjust it minimally to fit a list-item style.\nInput: A user's chat message\n\nOutput: A shortened version of the message as a list item\nExample:\nInput: \"I'm really excited about the new project launch happening next week, it's going to be amazing!\"\nOutput: \"Excited for next week's amazing project launch!\""),
            new ChatMessage(ChatMessageRole.User, request.Conversation.Messages.Last().Content)
        };
        
        var chatRequest = new ChatRequest
        {
            Model = request.Model,
            Messages = messages,
            Temperature = 0.2f,
            MaxTokens = 250
        };
        
        var response = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        
        return response.Choices[0].Message.Content.Trim('"');
    }
    
    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        request.SetClientDateTimeInSystemMessage();
        
        var chatMessages = request.Conversation.Messages.Select(m =>
        {
            return m.Role switch
            {
                "system" => new ChatMessage(ChatMessageRole.System, m.Content),
                "assistant" => new ChatMessage(ChatMessageRole.Assistant, m.Content),
                _ => new ChatMessage(ChatMessageRole.User, m.Content)
            };
        }).ToList();

        var chatRequest = new ChatRequest
        {
            Model = request.Model,
            Messages = chatMessages
        };
        
        if (request.Temperature.HasValue)
            chatRequest.Temperature = (float)request.Temperature.Value;
        else if (request.TopP.HasValue)
            chatRequest.TopP = (float)request.TopP.Value;
        
        if (request.MaxTokens.HasValue)
            chatRequest.MaxTokens = request.MaxTokens.Value;
        
        var completionId = Guid.NewGuid().ToString();
        var messageToSave = AesirChatMessage.NewAssistantMessage("");
        
        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        var streamingChatCompletions = api.ChatEndpoint.StreamCompletionAsync(chatRequest);
        
        await foreach (var completion in streamingChatCompletions)
        {
            logger.LogDebug("Received Chat Completion Response from OpenAI backend");
            
            var content = completion.Choices[0].Delta.Content;
            if (content != null)
            {
                var delta = AesirChatMessage.NewAssistantMessage(content);
                messageToSave.Content += content;
                
                yield return new AesirChatStreamedResult
                {
                    Id = completionId,
                    ChatSessionId = request.ChatSessionId,
                    ConversationId = request.Conversation.Id,
                    Delta = delta,
                    Title = title
                };
            }
            
            if (completion.Choices[0].FinishReason != null)
            {
                request.Conversation.Messages.Add(messageToSave);
                await chatHistoryService.UpsertChatSessionAsync(new AesirChatSession
                {
                    Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
                    Title = title,
                    Conversation = request.Conversation,
                    UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
                    UserId = request.User
                });
            }
        }
    }
}

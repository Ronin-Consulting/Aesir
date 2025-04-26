using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using OpenAI.Chat;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

[Experimental("SKEXP0070")]
public class ChatService(
    ILogger<ChatService> logger,
    VectorStoreTextSearch<AesirTextData<Guid>> vectorStoreTextSearch,
    Kernel kernel,
    IChatCompletionService chatCompletionService,
    IChatHistoryService chatHistoryService)
    : IChatService
{
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        var chatHistory = CreateChatHistory(request.Conversation.Messages);
        
        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            Temperature = request.Temperature ?? 0.7f,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens
        };

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
            var completionResults = await chatCompletionService.GetChatMessageContentsAsync(
                chatHistory,
                settings,
                kernel);
            
            if (completionResults.Count > 0)
            {
                messageToSave.Content = completionResults[0].Content ?? string.Empty;
                
                response.AesirConversation.Messages.Add(messageToSave);
                
                if (completionResults[0].Metadata != null && 
                    completionResults[0].Metadata!.TryGetValue("Usage", out var usageObj) && 
                    usageObj is ChatTokenUsage usage)
                {
                    response.CompletionTokens = usage.OutputTokenCount;
                    response.PromptTokens = usage.InputTokenCount;
                    response.TotalTokens = usage.TotalTokenCount;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting chat completion from Semantic Kernel IChatCompletionService");
            messageToSave.Content = "I apologize, but I encountered an error processing your request.";
            response.AesirConversation.Messages.Add(messageToSave);
        }

        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        await chatHistoryService.UpsertChatSessionAsync(new AesirChatSession()
        {
            Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
            Title = title,
            Conversation = response.AesirConversation,
            UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
            UserId = request.User 
        });
        
        return response;
    }
    
    private async Task<string> GetTitleForUserMessageAsync(AesirChatRequest request)
    {
        var titleSystemPrompt = "You are an AI designed to summarize user messages for display as concise list items. Your task is to take a user's chat message and shorten it into a brief, clear summary that retains the original meaning. Focus on capturing the key idea or intent, omitting unnecessary details, filler words, or repetition. The output should be succinct, natural, and suitable for a list format, ideally no longer than 5-10 words. If the message is already short, adjust it minimally to fit a list-item style.\nInput: A user's chat message\n\nOutput: A shortened version of the message as a list item\nExample:\nInput: \"I'm really excited about the new project launch happening next week, it's going to be amazing!\"\nOutput: \"Excited for next week's amazing project launch!\"";
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(titleSystemPrompt);
        chatHistory.AddUserMessage(request.Conversation.Messages.Last().Content);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            Temperature = 0.2f
        };

        try
        {
            var completionResults = await chatCompletionService.GetChatMessageContentsAsync(
                chatHistory,
                settings,
                kernel);
            
            if (completionResults.Count > 0)
            {
                var content = completionResults[0].Content;
                return content?.Trim('"') ?? "Untitled conversation";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating title using Semantic Kernel IChatCompletionService");
        }
        
        // Fallback to the basic title
        return request.Conversation.Messages.Last().Content.Substring(0, 
            Math.Min(50, request.Conversation.Messages.Last().Content.Length)) + "...";
    }

    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));
        request.SetClientDateTimeInSystemMessage();

        var chatHistory = CreateChatHistory(request.Conversation.Messages);
        
        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            Temperature = request.Temperature ?? 0.7f,
            TopP = request.TopP,
            MaxTokens = request.MaxTokens
        };

        var completionId = Guid.NewGuid().ToString();
        var messageToSave = AesirChatMessage.NewAssistantMessage("");
        
        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        IAsyncEnumerable<StreamingChatMessageContent> streamingResults;
        try
        {
            streamingResults = chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory,
                settings,
                kernel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error streaming chat completion from Semantic Kernel");
            
            var errorMessage = AesirChatMessage.NewAssistantMessage("I apologize, but I encountered an error processing your request.");
            
            // Save the conversation with the error message
            request.Conversation.Messages.Add(errorMessage);
            await chatHistoryService.UpsertChatSessionAsync(new AesirChatSession()
            {
                Id = request.ChatSessionId ?? throw new InvalidOperationException("ChatSessionId is null"),
                Title = title,
                Conversation = request.Conversation,
                UpdatedAt = request.ChatSessionUpdatedAt.ToUniversalTime(),
                UserId = request.User 
            });

            throw;
        }
        
        await foreach (var streamResult in streamingResults)
        {
            logger.LogDebug("Received streaming content from Semantic Kernel: {Content}", streamResult.Content);
            
            // The content might be null for the initial response or other metadata messages
            if (!string.IsNullOrEmpty(streamResult.Content))
            {
                messageToSave.Content += streamResult.Content;
                
                var messageToSend = AesirChatMessage.NewAssistantMessage(streamResult.Content);
                
                yield return new AesirChatStreamedResult()
                {
                    Id = completionId,
                    ChatSessionId = request.ChatSessionId,
                    ConversationId = request.Conversation.Id,
                    Delta = messageToSend,
                    Title = title
                };
            }
            
            // Check if this is the last chunk (using IsEnd property if available)
            if (streamResult is OpenAIStreamingChatMessageContent { FinishReason: ChatFinishReason.Stop }) 
            {
                request.Conversation.Messages.Add(messageToSave);
                await chatHistoryService.UpsertChatSessionAsync(new AesirChatSession()
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
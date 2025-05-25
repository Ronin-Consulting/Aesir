using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Data;
using Newtonsoft.Json;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

[Experimental("SKEXP0070")]
public class ChatService(
    ILogger<ChatService> logger,
    OllamaApiClient api,
    VectorStoreTextSearch<AesirTextData<Guid>> vectorStoreTextSearch,
    Kernel  kernel,
    IChatCompletionService chatCompletionService,
    IChatHistoryService chatHistoryService)
    : IChatService
{
    public async Task<AesirChatResult> ChatCompletionsAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        request.SetClientDateTimeInSystemMessage();
        
        var requestOptions = new RequestOptions()
        {
            NumPredict = request.MaxTokens
        };
        if(request.Temperature.HasValue)
            requestOptions.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            requestOptions.TopP = (float?)request.TopP;
        
        var ollamaRequest = new ChatRequest()
        {
             Model = request.Model,
             Stream = false,
             Options = requestOptions,
             Messages = request.Conversation.Messages.Select(
                 m =>
                 {
                     return m.Role switch
                     {
                         "system" => new Message(ChatRole.System, m.Content),
                         "assistant" => new Message(ChatRole.Assistant, m.Content),
                         _ => new Message(ChatRole.User, m.Content)
                     };
                 }).ToList()
        };
        
        var response = new AesirChatResult()
        {
            AesirConversation = request.Conversation,
            CompletionTokens = 0,
            PromptTokens = 0,
            TotalTokens = 0
        };
        var messageToSave = AesirChatMessage.NewAssistantMessage("");
        
        await foreach (var completion in api.ChatAsync(ollamaRequest))
        {
            messageToSave.Content += completion!.Message.Content;

            if (!completion.Done) continue;

            var doneCompletion = (ChatDoneResponseStream)completion;
            
            response.AesirConversation.Messages.Add(messageToSave);
            response.CompletionTokens = doneCompletion.EvalCount!;
            response.PromptTokens = doneCompletion.PromptEvalCount!;
            response.TotalTokens = doneCompletion.EvalCount + doneCompletion.PromptEvalCount;
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
        var requestOptions = new RequestOptions()
        {
            NumPredict = 250,
            Temperature = 0.2f
        };
        
        if(request.Conversation.Messages.Count > 2)
            throw new InvalidOperationException("This operation should only be used when user first creates completion.");
        
        var messages = new List<Message>
        {
            new Message(ChatRole.System, "You are an AI designed to summarize user messages for display as concise list items. Your task is to take a user’s chat message and shorten it into a brief, clear summary that retains the original meaning. Focus on capturing the key idea or intent, omitting unnecessary details, filler words, or repetition. The output should be succinct, natural, and suitable for a list format, ideally no longer than 5-10 words. If the message is already short, adjust it minimally to fit a list-item style.\nInput: A user’s chat message\n\nOutput: A shortened version of the message as a list item\nExample:\nInput: \"I’m really excited about the new project launch happening next week, it’s going to be amazing!\"\nOutput: \"Excited for next week’s amazing project launch!\""),
            new Message(ChatRole.User, request.Conversation.Messages.Last().Content)
        };
        
        var ollamaRequest = new ChatRequest()
        {
            Model = request.Model,
            Stream = false,
            Options = requestOptions,
            Messages = messages
        };
        
        ollamaRequest.CustomHeaders.Add("enable_thinking", "false");
        
        var message = AesirChatMessage.NewAssistantMessage("");
        await foreach (var completion in api.ChatAsync(ollamaRequest))
        {
            message.Content += completion!.Message.Content;
        }
        
        return message.Content.Trim('"');
    }

    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsync(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        request.SetClientDateTimeInSystemMessage();
        
        var settings = new OllamaPromptExecutionSettings
        {
            ModelId = request.Model,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = request.Temperature.HasValue ? (float?)request.Temperature.Value : null
        };
        
        if(request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;
        
        var chatHistory = new ChatHistory();
        
        chatHistory.AddRange(request.Conversation.Messages.Select(
        m =>
        {
            return m.Role switch
            {
                "system" => new ChatMessageContent(AuthorRole.System, m.Content),
                "assistant" => new ChatMessageContent(AuthorRole.Assistant, m.Content),
                _ => new ChatMessageContent(AuthorRole.User, m.Content)
            };
        }));

        var completionId = Guid.NewGuid().ToString();
        
        var messageToSave = AesirChatMessage.NewAssistantMessage("");
        
        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        var results = chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            kernel
        );
        
        await foreach (var completion in results)
        {
            logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));
            
            var messageToSend = AesirChatMessage.NewAssistantMessage(completion!.Content!);
            
            messageToSave.Content += completion!.Content;
            
            if(completion.InnerContent is ChatDoneResponseStream { Done: true })
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

    public async IAsyncEnumerable<AesirChatStreamedResult> ChatCompletionsStreamedAsyncZZ(AesirChatRequest request)
    {
        request = request ?? throw new ArgumentNullException(nameof(request));

        request.SetClientDateTimeInSystemMessage();
        
        var requestOptions = new RequestOptions()
        {
            NumPredict = request.MaxTokens
        };
        if(request.Temperature.HasValue)
            requestOptions.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            requestOptions.TopP = (float?)request.TopP;
        
        var ollamaRequest = new ChatRequest()
        {
             Model = request.Model,
             Stream = true,
             Options = requestOptions,
             Messages = request.Conversation.Messages.Select(
             m =>
             {
                 return m.Role switch
                 {
                     "system" => new Message(ChatRole.System, m.Content),
                     "assistant" => new Message(ChatRole.Assistant, m.Content),
                     _ => new Message(ChatRole.User, m.Content)
                 };
             }).ToList()
        };
        
        var completionId = Guid.NewGuid().ToString();
        
        var messageToSave = AesirChatMessage.NewAssistantMessage("");
        
        var title = request.Title;
        if (request.Conversation.Messages.Count == 2)
        {
            title = await GetTitleForUserMessageAsync(request);
        }
        
        await foreach (var completion in api.ChatAsync(ollamaRequest))
        {
            logger.LogDebug("Received Chat Completion Response from Ollama backend: {Json}", JsonConvert.SerializeObject(completion));
            
            var messageToSend = AesirChatMessage.NewAssistantMessage(completion!.Message.Content!);
            
            messageToSave.Content += completion!.Message.Content;
            
            if(completion.Done)
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
}
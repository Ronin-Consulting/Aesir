using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

public class OpenAiPromptExecutionSettingsBuilder(Kernel kernel, IConversationDocumentCollectionService conversationDocumentCollectionService) : 
    BasePromptExecutionSettingsBuilder<OpenAIPromptExecutionSettings>(kernel, conversationDocumentCollectionService)
{
    protected override OpenAIPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequest request)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = request.Model,
            MaxTokens = request.MaxTokens
        };
        
        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;
        
        return settings;
    }
    
    protected override void ConfigureForThinking(OpenAIPromptExecutionSettings settings, AesirChatRequest request)
    {
        if (request.EnableThinking ?? false)
        {
            settings.ReasoningEffort = (string?)request.ThinkValue;
        }
    }
}
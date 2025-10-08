using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services.Implementations.Standard;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

public class OllamaPromptExecutionSettingsBuilder(Kernel kernel, IConversationDocumentCollectionService conversationDocumentCollectionService, IKernelPluginService kernelPluginService) : 
    BasePromptExecutionSettingsBuilder<OllamaPromptExecutionSettings>(kernel, conversationDocumentCollectionService, kernelPluginService)
{
    protected override OllamaPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequest request)
    {
        var settings = new OllamaPromptExecutionSettings
        {
            ModelId = request.Model,
            NumPredict = request.MaxTokens ?? 32768,
            ExtensionData = new Dictionary<string, object>()
        };
        
        if (request.Temperature.HasValue)
            settings.Temperature = (float?)request.Temperature;
        else if (request.TopP.HasValue)
            settings.TopP = (float?)request.TopP;
        
        return settings;
    }

    protected override void ConfigureForThinking(OllamaPromptExecutionSettings settings, AesirChatRequest request)
    {
        if (request.EnableThinking ?? false)
        {
            settings.ExtensionData!.Add("think", request.ThinkValue ?? true);
        }
    }
}
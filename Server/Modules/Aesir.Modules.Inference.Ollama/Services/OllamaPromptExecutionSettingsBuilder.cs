using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;
using Aesir.Infrastructure.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Modules.Inference.Ollama.Services;

[Experimental("SKEXP0070")]
public class OllamaPromptExecutionSettingsBuilder(Kernel kernel, IConversationDocumentCollectionService conversationDocumentCollectionService, IKernelPluginService kernelPluginService) :
    BasePromptExecutionSettingsBuilder<OllamaPromptExecutionSettings>(kernel, conversationDocumentCollectionService, kernelPluginService)
{
    protected override OllamaPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequestBase request)
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

    protected override void ConfigureForThinking(OllamaPromptExecutionSettings settings, AesirChatRequestBase request)
    {
        if (request.EnableThinking ?? false)
        {
            settings.ExtensionData!.Add("think", request.ThinkValue ?? true);
        }
    }
}

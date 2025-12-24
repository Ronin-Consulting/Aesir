using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Modules.Inference.Ollama.Services;

[Experimental("SKEXP0070")]
public class OllamaPromptExecutionSettingsBuilder(
    Kernel kernel,
    IConversationDocumentCollectionService? conversationDocumentCollectionService,
    IKernelPluginService kernelPluginService,
    ILogger<OllamaPromptExecutionSettingsBuilder> logger) :
    BasePromptExecutionSettingsBuilder<OllamaPromptExecutionSettings>(kernel, conversationDocumentCollectionService, kernelPluginService, logger)
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
        logger.LogInformation("[Ollama-Thinking] === CONFIGURING THINKING FOR OLLAMA ===");
        logger.LogInformation("[Ollama-Thinking] Input: EnableThinking={EnableThinking}, ThinkValue={ThinkValue}",
            request.EnableThinking, request.ThinkValue);

        if (request.EnableThinking ?? false)
        {
            var thinkValue = request.ThinkValue ?? true;
            settings.ExtensionData!.Add("think", thinkValue);
            logger.LogInformation("[Ollama-Thinking] === SENDING TO OLLAMA API: think={ThinkValue} ===", thinkValue);
        }
        else
        {
            logger.LogInformation("[Ollama-Thinking] Thinking NOT enabled - 'think' parameter will NOT be sent to Ollama");
        }
    }
}

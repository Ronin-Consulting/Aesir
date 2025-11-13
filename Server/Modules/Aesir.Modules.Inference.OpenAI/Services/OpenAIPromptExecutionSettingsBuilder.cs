using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;
using Aesir.Infrastructure.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aesir.Modules.Inference.OpenAI.Services;

[Experimental("SKEXP0070")]
public class OpenAiPromptExecutionSettingsBuilder(Kernel kernel, IConversationDocumentCollectionService conversationDocumentCollectionService, IKernelPluginService kernelPluginService) :
    BasePromptExecutionSettingsBuilder<OpenAIPromptExecutionSettings>(kernel, conversationDocumentCollectionService, kernelPluginService)
{
    protected override OpenAIPromptExecutionSettings CreatePromptExecutionSettings(AesirChatRequestBase request)
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

    protected override void ConfigureForThinking(OpenAIPromptExecutionSettings settings, AesirChatRequestBase request)
    {
        // Note: ReasoningEffort property is not available in OpenAI connector 2.1.0
        // This feature may be added in future versions
        if (request.EnableThinking ?? false)
        {
            // TODO: Implement thinking/reasoning mode when API support is available
            // For now, this is a no-op
        }
    }
}

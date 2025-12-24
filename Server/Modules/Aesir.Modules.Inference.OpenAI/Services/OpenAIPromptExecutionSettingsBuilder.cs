using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Models;
using Aesir.Modules.Inference.Services;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

#pragma warning disable SKEXP0010 // ReasoningEffort is experimental

namespace Aesir.Modules.Inference.OpenAI.Services;

[Experimental("SKEXP0070")]
public class OpenAiPromptExecutionSettingsBuilder(
    Kernel kernel,
    IConversationDocumentCollectionService? conversationDocumentCollectionService,
    IKernelPluginService kernelPluginService,
    ILogger<OpenAiPromptExecutionSettingsBuilder> logger) :
    BasePromptExecutionSettingsBuilder<OpenAIPromptExecutionSettings>(kernel, conversationDocumentCollectionService, kernelPluginService, logger)
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
        logger.LogInformation("[OpenAI-Thinking] === CONFIGURING THINKING FOR OPENAI ===");
        logger.LogInformation("[OpenAI-Thinking] Input: EnableThinking={EnableThinking}, ThinkValue={ThinkValue}",
            request.EnableThinking, request.ThinkValue);

        if (request.EnableThinking ?? false)
        {
            // Map ThinkValue to OpenAI ReasoningEffort
            // ThinkValue can be: true/false (boolean) or "high"/"medium"/"low" (string)
            var reasoningEffort = MapThinkValueToReasoningEffort(request.ThinkValue);

            settings.ReasoningEffort = reasoningEffort;
            logger.LogInformation("[OpenAI-Thinking] === SENDING TO OPENAI API: reasoning_effort={ReasoningEffort} ===", reasoningEffort);
        }
        else
        {
            logger.LogInformation("[OpenAI-Thinking] Thinking NOT enabled - reasoning_effort will NOT be sent to OpenAI");
        }
    }

    /// <summary>
    /// Maps the ThinkValue to OpenAI's ReasoningEffort values.
    /// OpenAI supports: "low", "medium", "high", "minimal"
    /// </summary>
    private static string MapThinkValueToReasoningEffort(ThinkValue? thinkValue)
    {
        if (thinkValue == null)
            return "medium"; // Default to medium if not specified

        var value = thinkValue.Value.ToString()?.ToLowerInvariant();

        return value switch
        {
            "true" => "medium",      // Boolean true defaults to medium
            "false" => "low",        // Boolean false defaults to low (shouldn't reach here normally)
            "high" => "high",
            "medium" => "medium",
            "low" => "low",
            "minimal" => "minimal",  // GPT-5 support
            _ => "medium"            // Unknown values default to medium
        };
    }
}

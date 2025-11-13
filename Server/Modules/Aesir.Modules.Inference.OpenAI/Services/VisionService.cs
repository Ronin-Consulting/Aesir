using Aesir.Common.Prompts;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aesir.Modules.Inference.OpenAI.Services;

/// <summary>
/// Implements vision-based AI services using OpenAI to extract textual content from images.
/// </summary>
public class VisionService(
    ILogger<VisionService> logger,
    IServiceProvider serviceProvider) : IVisionService
{
    /// <summary>
    /// Represents a provider for generating and retrieving prompt templates used in AI operations,
    /// such as OCR and text extraction, within the VisionService.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = DefaultPromptProvider.Instance;

    /// <summary>
    /// Extracts text content from a provided image asynchronously.
    /// </summary>
    /// <param name="modelLocationDescriptor">The loation of the model to use.</param>
    /// <param name="imageBytes">The image data provided as a read-only memory byte array.</param>
    /// <param name="contentType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Returns the text extracted from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(Aesir.Infrastructure.Services.ModelLocationDescriptor modelLocationDescriptor,
        ReadOnlyMemory<byte> imageBytes, string contentType, CancellationToken cancellationToken = default)
    {
        var chatCompletionService =
            serviceProvider.GetKeyedService<IChatCompletionService>(modelLocationDescriptor.InterfaceEngineId)
            ?? throw new InvalidOperationException($"Failed to get Chat Completion service for engine {modelLocationDescriptor.InterfaceEngineId}");

        if (string.IsNullOrWhiteSpace(modelLocationDescriptor.ModelId))
            throw new InvalidOperationException("No vision model provided");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptPersona.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Analyze this image."),
            new ImageContent(imageBytes, contentType),
        ]);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = modelLocationDescriptor.ModelId,
            Temperature = 0.2f
        };

        var result = await chatCompletionService
            .GetChatMessageContentsAsync(chatHistory, settings, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return string.Join("\n", result.Select(x => x.Content));
    }
}

using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Implements vision-based AI services using OpenAI to extract textual content from images.
/// </summary>
public class VisionService(
    ILogger<VisionService> logger,
    VisionModelConfig visionModelConfig,
    IChatCompletionService chatCompletionService) : IVisionService
{
    /// <summary>
    /// Represents a provider for generating and retrieving prompt templates used in AI operations,
    /// such as OCR and text extraction, within the VisionService.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = DefaultPromptProvider.Instance;

    /// <summary>
    /// Represents the configured identifier for the vision model, utilized to process image data and extract textual content.
    /// </summary>
    private readonly string _visionModel = visionModelConfig.ModelId;

    /// <summary>
    /// Extracts text content from a provided image asynchronously.
    /// </summary>
    /// <param name="imageBytes">The image data provided as a read-only memory byte array.</param>
    /// <param name="contentType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Returns the text extracted from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> imageBytes, string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptPersona.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Analyze this image."),
            new ImageContent(imageBytes, contentType),
        ]);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = _visionModel,
            Temperature = 0.2f
        };

        var result = await chatCompletionService
            .GetChatMessageContentsAsync(chatHistory, settings, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return string.Join("\n", result.Select(x => x.Content));
    }
}
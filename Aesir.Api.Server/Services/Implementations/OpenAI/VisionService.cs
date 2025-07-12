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
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Represents the configured identifier for the vision model, utilized to process image data and extract textual content.
    /// </summary>
    private readonly string _visionModel = visionModelConfig.ModelId;

    /// <summary>
    /// Extracts text content from a provided image asynchronously.
    /// </summary>
    /// <param name="image">The image data provided as a read-only memory byte array.</param>
    /// <param name="mimeType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Returns the text extracted from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptContext.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Extract and return only the text visible in the provided image as plain text."),
            new ImageContent(image, mimeType),
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

/// <summary>
/// Configuration class for specifying settings and parameters for the vision AI model,
/// such as the model identifier used by the VisionService.
/// </summary>
public class VisionModelConfig
{
    /// <summary>
    /// Represents the identifier of the vision model configured for processing images and extracting textual information.
    /// </summary>
    public required string ModelId { get; set; }
}
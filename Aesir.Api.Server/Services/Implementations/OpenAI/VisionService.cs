using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Provides vision AI services using the OpenAI backend with functionalities to process images and extract text.
/// </summary>
public class VisionService(
    ILogger<VisionService> logger,
    VisionModelConfig visionModelConfig,
    IChatCompletionService chatCompletionService) : IVisionService
{
    /// <summary>
    /// Specifies the identifier of the vision model used for processing images and extracting text.
    /// </summary>
    private readonly string _visionModel = visionModelConfig.ModelId;

    /// <summary>
    /// Extracts text content from a provided image asynchronously.
    /// </summary>
    /// <param name="image">The image data provided as a read-only memory byte array.</param>
    /// <param name="mimeType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>Returns the text extracted from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");

        var chatHistory = new ChatHistory();
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
/// Configuration class for managing settings related to the vision AI model.
/// </summary>
public class VisionModelConfig
{
    /// <summary>
    /// Gets or sets the identifier for the vision model used in the AI service.
    /// </summary>
    /// <remarks>
    /// This property is used to specify the unique identifier for the vision model configuration
    /// and is typically supplied from application settings or configuration files.
    /// </remarks>
    public required string ModelId { get; set; }
}
using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Provides vision AI services for extracting text from images using the Ollama backend.
/// </summary>
/// <param name="logger">The logging service for capturing operational details and diagnostics.</param>
/// <param name="visionModelConfig">The configuration details including the model ID for the vision AI service.</param>
/// <param name="chatCompletionService">The service for handling chat-based interactions with the vision processing backend.</param>
[Experimental("SKEXP0070")]
public class VisionService(
    ILogger<VisionService> logger,
    VisionModelConfig visionModelConfig,
    IChatCompletionService chatCompletionService)
    : IVisionService
{
    /// <summary>
    /// A static instance of <see cref="IPromptProvider"/> used for managing and retrieving predefined prompt templates
    /// for various contexts, such as OCR, business, and military.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Represents the identifier for the configured vision model to be used for image processing tasks.
    /// </summary>
    /// <remarks>
    /// This variable retrieves its value from the <see cref="VisionModelConfig"/> and is used in various vision-related operations,
    /// including text extraction from images. If not properly configured, operations may throw an exception.
    /// </remarks>
    private readonly string _visionModel = visionModelConfig.ModelId;

    /// <summary>
    /// Extracts and returns the text content visible in the provided image as plain text.
    /// </summary>
    /// <param name="image">The image data from which to extract text, provided as a read-only memory byte buffer.</param>
    /// <param name="mimeType">The MIME type of the provided image, such as "image/png" or "image/jpeg".</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the extracted text from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");
        
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptContext.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Extract and return only the text visible in the provided image as plain text."),
            new ImageContent(image, mimeType),
        ]);
        
        var settings = new OllamaPromptExecutionSettings
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
/// Represents the configuration settings required for the vision AI model.
/// </summary>
public class VisionModelConfig
{
    /// <summary>
    /// Gets or sets the identifier of the vision model used for processing.
    /// This property specifies the model identifier that the vision service utilizes
    /// for handling image processing tasks. It is required to correctly configure
    /// which vision model will be used during service operations.
    /// </summary>
    public required string ModelId { get; set; }
}
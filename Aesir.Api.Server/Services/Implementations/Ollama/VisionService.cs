using System.Diagnostics.CodeAnalysis;
using Aesir.Common.Prompts;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// A service for processing images to extract textual content using a vision AI model backend.
/// </summary>
/// <param name="logger">The logging provider used for recording activity and errors occurring within the service.</param>
/// <param name="visionModelConfig">Configuration parameters for the vision AI model, including any necessary model-specific settings.</param>
/// <param name="chatCompletionService">Service utilized to manage interactions with the backend vision processing system.</param>
[Experimental("SKEXP0070")]
public class VisionService(
    ILogger<VisionService> logger,
    VisionModelConfig visionModelConfig,
    IChatCompletionService chatCompletionService)
    : IVisionService
{
    /// <summary>
    /// A static instance of <see cref="IPromptProvider"/> for managing and retrieving prompt templates
    /// tailored to specific contexts within the application.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = DefaultPromptProvider.Instance;

    /// <summary>
    /// Represents the configured vision model identifier used to control which model is employed
    /// for image analysis and processing tasks.
    /// </summary>
    /// <remarks>
    /// This field is initialized from the <see cref="VisionModelConfig"/> property <c>ModelId</c>
    /// and is essential for performing vision-related operations. If not configured correctly,
    /// related methods may fail due to the absence of a valid model identifier.
    /// </remarks>
    private readonly string _visionModel = visionModelConfig.ModelId;

    /// <summary>
    /// Extracts and returns the text content visible in the provided image as plain text.
    /// </summary>
    /// <param name="image">The image data from which to extract text, provided as a read-only memory byte buffer.</param>
    /// <param name="contentType">The MIME type of the provided image, such as "image/png" or "image/jpeg".</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the extracted text from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptPersona.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Analyze this image."),
            new ImageContent(image, contentType),
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
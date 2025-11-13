using System.Diagnostics.CodeAnalysis;
using Aesir.Common.FileTypes;
using Aesir.Common.Prompts;
using Aesir.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Aesir.Modules.Inference.Ollama.Services;

/// <summary>
/// A service for analyzing images and extracting textual content using the specified vision AI model and configurations.
/// </summary>
/// <param name="logger">Instance of a logger to capture and record service-related activities and errors.</param>
/// <param name="serviceProvider">Locates other services.</param>
[Experimental("SKEXP0070")]
public class VisionService(
    ILogger<VisionService> logger,
    IServiceProvider serviceProvider)
    : IVisionService
{
    /// <summary>
    /// Represents an instance of <see cref="IPromptProvider"/> used to handle prompt template management
    /// and delivery for defined application-specific contexts.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = DefaultPromptProvider.Instance;

    /// <summary>
    /// Extracts and returns the textual content from the provided image using the configured vision model.
    /// </summary>
    /// <param name="modelLocationDescriptor">The loation of the model to use.</param>
    /// <param name="imageBytes">The raw bytes of the image to be analyzed.</param>
    /// <param name="contentType">The MIME type of the image, such as "image/png", "image/jpeg", or "image/tiff".</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the extracted text from the image as a plain string.</returns>
    public async Task<string> GetImageTextAsync(Aesir.Infrastructure.Services.ModelLocationDescriptor modelLocationDescriptor, ReadOnlyMemory<byte> imageBytes, string contentType,
        CancellationToken cancellationToken = default)
    {
        var chatCompletionService = serviceProvider.GetKeyedService<IChatCompletionService>(modelLocationDescriptor.InterfaceEngineId)
            ?? throw new InvalidOperationException($"Failed to get Chat Completion service for engine {modelLocationDescriptor.InterfaceEngineId}");

        if (string.IsNullOrWhiteSpace(modelLocationDescriptor.ModelId))
            throw new InvalidOperationException("No vision model provided");

        // Resize the image to a resolution that works well with the vision model
        using var image = Image.Load(imageBytes.Span);
        image.Mutate(x =>
            x.Resize(new ResizeOptions
            {
                // gemma 3 vision model preferred resolution
                // its our default tested vision model
                Size = new Size(896, 896),
                Mode = ResizeMode.Max
            }));

        using var ms = new MemoryStream();
        switch (contentType)
        {
            case FileTypeManager.MimeTypes.Jpeg:
                await image.SaveAsJpegAsync(ms, cancellationToken);
                break;
            case FileTypeManager.MimeTypes.Png:
                await image.SaveAsPngAsync(ms, cancellationToken);
                break;
            case FileTypeManager.MimeTypes.Tiff:
                await image.SaveAsTiffAsync(ms, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Unsupported image content type: {contentType}");
        }

        var resizedImageBytes = new ReadOnlyMemory<byte>(ms.ToArray());

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptPersona.Ocr).Content);
        chatHistory.AddUserMessage([
            new TextContent("Analyze this image."),
            new ImageContent(resizedImageBytes, contentType),
        ]);

        var settings = new OllamaPromptExecutionSettings
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

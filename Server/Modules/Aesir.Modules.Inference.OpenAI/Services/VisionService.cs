using System.Text.RegularExpressions;
using Aesir.Common.Prompts;
using Aesir.Infrastructure.Services;
using Aesir.Modules.Inference.Services;
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

    /// <inheritdoc />
    public int MaxBatchSize => 10;

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
        var chatCompletionServiceFactory =
            serviceProvider.GetKeyedService<IChatCompletionServiceFactory>(modelLocationDescriptor.InterfaceEngineId)
            ?? throw new InvalidOperationException($"Failed to get Chat Completion service factory for engine {modelLocationDescriptor.InterfaceEngineId}");

        if (string.IsNullOrWhiteSpace(modelLocationDescriptor.ModelId))
            throw new InvalidOperationException("No vision model provided");

        var chatCompletionService = chatCompletionServiceFactory.GetChatCompletionService(modelLocationDescriptor.ModelId);

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

    /// <inheritdoc />
    public async Task<string[]> GetImageTextBatchAsync(
        ModelLocationDescriptor modelLocationDescriptor,
        IReadOnlyList<(ReadOnlyMemory<byte> ImageBytes, string ContentType)> images,
        CancellationToken cancellationToken = default)
    {
        if (images.Count == 0)
            return [];

        if (images.Count == 1)
        {
            var singleResult = await GetImageTextAsync(
                modelLocationDescriptor,
                images[0].ImageBytes,
                images[0].ContentType,
                cancellationToken).ConfigureAwait(false);
            return [singleResult];
        }

        var chatCompletionServiceFactory =
            serviceProvider.GetKeyedService<IChatCompletionServiceFactory>(modelLocationDescriptor.InterfaceEngineId)
            ?? throw new InvalidOperationException($"Failed to get Chat Completion service factory for engine {modelLocationDescriptor.InterfaceEngineId}");

        if (string.IsNullOrWhiteSpace(modelLocationDescriptor.ModelId))
            throw new InvalidOperationException("No vision model provided");

        var chatCompletionService = chatCompletionServiceFactory.GetChatCompletionService(modelLocationDescriptor.ModelId);

        // Build message with multiple images
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(PromptProvider.GetSystemPrompt(PromptPersona.Ocr).Content);

        var messageContents = new List<KernelContent>
        {
            new TextContent($"Analyze these {images.Count} images. Extract text from each, clearly separated with markers [IMAGE 1], [IMAGE 2], etc.")
        };

        foreach (var img in images)
        {
            messageContents.Add(new ImageContent(img.ImageBytes, img.ContentType));
        }

        chatHistory.AddUserMessage([.. messageContents]);

        var settings = new OpenAIPromptExecutionSettings
        {
            ModelId = modelLocationDescriptor.ModelId,
            Temperature = 0.2f
        };

        var result = await chatCompletionService
            .GetChatMessageContentsAsync(chatHistory, settings, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var fullText = string.Join("\n", result.Select(x => x.Content));

        // Parse the response to extract per-image text
        return ParseBatchResponse(fullText, images.Count);
    }

    /// <summary>
    /// Parses a batch response into individual image texts based on [IMAGE N] markers.
    /// </summary>
    private static string[] ParseBatchResponse(string response, int expectedCount)
    {
        var results = new string[expectedCount];

        // Try to parse [IMAGE N] markers
        var pattern = @"\[IMAGE\s*(\d+)\]";
        var matches = Regex.Matches(response, pattern, RegexOptions.IgnoreCase);

        if (matches.Count >= expectedCount - 1)
        {
            // Split by markers
            var sections = Regex.Split(response, pattern, RegexOptions.IgnoreCase);

            for (var i = 1; i < sections.Length && i + 1 < sections.Length; i += 2)
            {
                if (int.TryParse(sections[i], out var imgNum) && imgNum > 0 && imgNum <= expectedCount)
                {
                    results[imgNum - 1] = sections[i + 1].Trim();
                }
            }
        }
        else
        {
            // Fallback: Split evenly if markers not found
            var avgLength = response.Length / expectedCount;
            for (var i = 0; i < expectedCount; i++)
            {
                var start = i * avgLength;
                var length = i == expectedCount - 1
                    ? response.Length - start
                    : avgLength;
                results[i] = response.Substring(start, Math.Min(length, response.Length - start)).Trim();
            }
        }

        // Ensure no null entries
        for (var i = 0; i < results.Length; i++)
        {
            results[i] ??= string.Empty;
        }

        return results;
    }
}

using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Provides vision AI services using the Ollama backend.
/// </summary>
/// <param name="logger">The logger instance for recording operations.</param>
/// <param name="visionModelConfig">The configuration for the vision model.</param>
/// <param name="chatCompletionService">The chat completion service for processing vision requests.</param>
[Experimental("SKEXP0070")]
public class VisionService(
    ILogger<VisionService> logger,
    VisionModelConfig visionModelConfig,
    IChatCompletionService chatCompletionService)
    : IVisionService
{
    private readonly string _visionModel = visionModelConfig.ModelId;

    public async Task<string> GetImageTextAsync(ReadOnlyMemory<byte> image, string mimeType, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(_visionModel))
            throw new InvalidOperationException("No vision model provided");
        
        var chatHistory = new ChatHistory();
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

public class VisionModelConfig
{
    public required string ModelId { get; set; }
}
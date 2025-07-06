using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using OllamaSharp;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

[Experimental("SKEXP0070")]
public class VisionService : IVisionService, IAsyncDisposable
{
    private readonly ILogger<VisionService> _logger;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly OllamaApiClient _ollamaApiClient;
    private readonly string _visionModel;

    public VisionService(
        ILogger<VisionService> logger, 
        VisionModelConfig  visionModelConfig,
        IChatCompletionService chatCompletionService,
        OllamaApiClient  ollamaApiClient
        )
    {
        _logger = logger;
        _chatCompletionService = chatCompletionService;
        _ollamaApiClient = ollamaApiClient;
        _visionModel = visionModelConfig.ModelId;
    }
    
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
        
        var result = await _chatCompletionService
            .GetChatMessageContentsAsync(chatHistory, settings, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        
        return string.Join("\n", result.Select(x => x.Content));
    }

    public async Task UnloadModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _ollamaApiClient.RequestModelUnloadAsync(_visionModel, cancellationToken: cancellationToken);
        }
        catch
        {
            _logger.LogWarning("Failed to unload vision model");
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        await UnloadModelAsync();
        
        if (_ollamaApiClient is IAsyncDisposable ollamaApiClientAsyncDisposable)
            await ollamaApiClientAsyncDisposable.DisposeAsync();
        else
            _ollamaApiClient.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}

public class VisionModelConfig
{
    public required string ModelId { get; set; }
}
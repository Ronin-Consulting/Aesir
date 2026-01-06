using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aesir.Infrastructure.Services;

/// <summary>
/// Resolves IChatService instances for agents based on their inference engine ID.
/// Uses the service provider to resolve keyed services.
/// </summary>
public class ChatServiceResolver : IChatServiceResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ChatServiceResolver> _logger;

    public ChatServiceResolver(
        IServiceProvider serviceProvider,
        ILogger<ChatServiceResolver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatService? GetChatService(Guid? inferenceEngineId)
    {
        if (!inferenceEngineId.HasValue)
        {
            _logger.LogWarning("Agent has no inference engine ID configured");
            return null;
        }

        var engineIdKey = inferenceEngineId.Value.ToString();
        _logger.LogDebug(
            "Resolving IChatService for inference engine ID: {EngineId}",
            inferenceEngineId.Value);

        var chatService = _serviceProvider.GetKeyedService<IChatService>(engineIdKey);

        if (chatService == null)
        {
            _logger.LogWarning(
                "No IChatService found for inference engine ID: {EngineId}. " +
                "Ensure the inference engine module is registered correctly.",
                inferenceEngineId.Value);
            return null;
        }

        _logger.LogDebug(
            "Successfully resolved IChatService: {ServiceType}",
            chatService.GetType().Name);

        return chatService;
    }

    /// <inheritdoc />
    public IChatService GetRequiredChatService(Guid? inferenceEngineId, string agentDescription = "Agent")
    {
        if (!inferenceEngineId.HasValue)
        {
            throw new InvalidOperationException(
                $"{agentDescription} must have an inference engine configured");
        }

        var chatService = GetChatService(inferenceEngineId);

        if (chatService == null)
        {
            throw new InvalidOperationException(
                $"No IChatService found for {agentDescription}'s inference engine {inferenceEngineId}");
        }

        return chatService;
    }
}

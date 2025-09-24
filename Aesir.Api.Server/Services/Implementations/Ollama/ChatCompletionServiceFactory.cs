using Microsoft.SemanticKernel.ChatCompletion;

namespace Aesir.Api.Server.Services.Implementations.Ollama;

/// <summary>
/// Factory class responsible for creating instances of <see cref="IChatCompletionService"/> for a specific model.
/// </summary>
/// <remarks>
/// This class is particularly designed for environments where a single instance of
/// <see cref="IChatCompletionService"/> should be reused for a given inference engine or service configuration.
/// </remarks>
public class ChatCompletionServiceFactory(IServiceProvider serviceProvider, string serviceKey)
    : IChatCompletionServiceFactory
{
    /// <summary>
    /// Retrieves an instance of <see cref="IChatCompletionService"/> for the specified model ID.
    /// </summary>
    /// <param name="modelId">The identifier of the model for which the chat completion service is requested.</param>
    /// <returns>An instance of <see cref="IChatCompletionService"/> configured for the specified model.</returns>
    public IChatCompletionService GetChatCompletionService(string modelId)
    {
        // we only create one IChatCompletionService for Ollama per inference engine, just return the one
        return serviceProvider.GetKeyedService<IChatCompletionService>(serviceKey);
    }
}
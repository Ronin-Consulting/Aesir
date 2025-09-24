using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI;

namespace Aesir.Api.Server.Services.Implementations.OpenAI;

/// <summary>
/// Factory class responsible for creating instances of <see cref="IChatCompletionService"/>
/// for specific OpenAI models. This implementation ensures that individual instances of
/// <see cref="OpenAIChatCompletionService"/> are created for each model.
/// </summary>
/// <remarks>
/// This design adheres to the current behavior of the Semantic Kernel library, which doesn't
/// honor model-specific settings passed during chat completion execution. As a result, the
/// factory instantiates a separate <see cref="IChatCompletionService"/> for each model type.
/// Future implementations may consider caching these instances to improve efficiency if
/// Semantic Kernel updates the underlying parameter handling.
/// </remarks>
public class ChatCompletionServiceFactory(OpenAIClient openAiClient, ILoggerFactory loggerFactory)
    : IChatCompletionServiceFactory
{
    /// <summary>
    /// Retrieves an instance of <see cref="IChatCompletionService"/> for the specified model ID.
    /// </summary>
    /// <param name="modelId">The identifier of the model for which the chat completion service is requested.</param>
    /// <returns>An instance of <see cref="IChatCompletionService"/> configured for the specified model.</returns>
    public IChatCompletionService GetChatCompletionService(string modelId)
    {
        // We create one IChatCompletionService for OpenAI per model. Currently, the calls to this object
        // take PromptExecutionSettings that have a model id, but that is ignored. We tried to create a PR
        // that would support using the paramter (https://github.com/microsoft/semantic-kernel/pull/13143), but
        // they rejected it. Until the time that they honor the parameter, we will create a new 
        // OpenAIChatCompletionService per model, per their suggestion.
        
        // TODO consider caching this as normally they would be registered as singletons anyway
        
        return new OpenAIChatCompletionService(modelId, openAiClient, loggerFactory);
    }
}
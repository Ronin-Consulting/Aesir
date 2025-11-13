using Microsoft.SemanticKernel.ChatCompletion;

namespace Aesir.Modules.Inference.Services;

/// <summary>
/// Represents a factory interface for obtaining implementations of <see cref="IChatCompletionService"/>.
/// </summary>
/// <remarks>
/// This interface provides a mechanism to retrieve instances of <see cref="IChatCompletionService"/>
/// based on a specified model identifier. Implementations of this factory are responsible
/// for ensuring that the correct service instance is returned to handle chat completions
/// for the corresponding AI model.
/// </remarks>
public interface IChatCompletionServiceFactory
{
    /// <summary>
    /// Retrieves an instance of <see cref="IChatCompletionService"/> for the specified model ID.
    /// </summary>
    /// <param name="modelId">The identifier of the model for which the chat completion service is requested.</param>
    /// <returns>An instance of <see cref="IChatCompletionService"/> configured for the specified model.</returns>
    IChatCompletionService GetChatCompletionService(string modelId);
}

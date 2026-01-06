namespace Aesir.Infrastructure.Services;

/// <summary>
/// Resolves IChatService instances for agents based on their inference engine ID.
/// Provides a centralized way to obtain chat services without duplicating
/// keyed service resolution logic across multiple services.
/// </summary>
public interface IChatServiceResolver
{
    /// <summary>
    /// Gets the IChatService for the specified inference engine ID.
    /// </summary>
    /// <param name="inferenceEngineId">The inference engine ID to resolve.</param>
    /// <returns>The chat service, or null if service cannot be resolved.</returns>
    IChatService? GetChatService(Guid? inferenceEngineId);

    /// <summary>
    /// Gets the IChatService for the specified inference engine ID.
    /// Throws an exception if the service cannot be resolved.
    /// </summary>
    /// <param name="inferenceEngineId">The inference engine ID to resolve.</param>
    /// <param name="agentDescription">Description of the agent for error messages.</param>
    /// <returns>The chat service.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service cannot be resolved.</exception>
    IChatService GetRequiredChatService(Guid? inferenceEngineId, string agentDescription = "Agent");
}

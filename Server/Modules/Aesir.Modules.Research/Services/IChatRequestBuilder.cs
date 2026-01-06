using Aesir.Common.Models;
using Aesir.Common.Prompts;
using Aesir.Modules.Research.Agents;

namespace Aesir.Modules.Research.Services;

/// <summary>
/// Options for building a chat request.
/// </summary>
public record ChatRequestOptions
{
    /// <summary>
    /// Whether to include tools configured for the agent.
    /// </summary>
    public bool IncludeTools { get; init; }

    /// <summary>
    /// Override temperature setting. If null, uses agent's temperature.
    /// </summary>
    public double? TemperatureOverride { get; init; }

    /// <summary>
    /// Override max tokens setting. If null, uses agent's max tokens.
    /// </summary>
    public int? MaxTokensOverride { get; init; }

    /// <summary>
    /// User identifier for the request.
    /// </summary>
    public required string User { get; init; }

    /// <summary>
    /// Title for the chat request.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Whether to use custom persona with the agent's persona string.
    /// </summary>
    public bool UseCustomPersona { get; init; }

    /// <summary>
    /// Custom persona content (uses agent.Persona if not specified).
    /// Only used when UseCustomPersona is true.
    /// </summary>
    public string? CustomPersonaContent { get; init; }
}

/// <summary>
/// Builds chat requests for research agents, consolidating common
/// configuration logic like thinking settings, tools, and model defaults.
/// </summary>
public interface IChatRequestBuilder
{
    /// <summary>
    /// Builds a chat request for a research agent.
    /// </summary>
    /// <param name="agent">The research agent making the request.</param>
    /// <param name="systemPrompt">The system prompt content.</param>
    /// <param name="userPrompt">The user prompt content.</param>
    /// <param name="options">Configuration options for the request.</param>
    /// <returns>A configured chat request.</returns>
    Task<AesirChatRequestBase> BuildAsync(
        ResearchAgent agent,
        string systemPrompt,
        string userPrompt,
        ChatRequestOptions options);
}

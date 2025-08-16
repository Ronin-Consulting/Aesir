using Aesir.Common.Models;
using Aesir.Common.Prompts.PromptCategories;

namespace Aesir.Common.Prompts;

/// <summary>
/// Represents the default implementation of the <see cref="IPromptProvider"/> interface.
/// Responsible for providing system prompts, title generation prompts, and condense prompts
/// based on the specified context.
/// </summary>
public class DefaultPromptProvider : IPromptProvider
{
    /// <summary>
    /// Provides access to the singleton instance of the DefaultPromptProvider.
    /// </summary>
    public static readonly DefaultPromptProvider Instance = new();

    /// <summary>
    /// Gets or sets the default prompt persona used when no specific context is provided.
    /// </summary>
    public PromptPersona? DefaultPromptPersona { get; set; }

    /// <summary>
    /// Provides default implementations for generating system prompts, title generation prompts,
    /// and condensed prompts based on the specified context.
    /// Implements the <see cref="IPromptProvider"/> interface.
    /// </summary>
    private DefaultPromptProvider()
    {
    }

    /// <summary>
    /// Retrieves a system prompt template based on the specified prompt context.
    /// </summary>
    /// <param name="context">The prompt context used to select the appropriate system prompt template. If null, a default context is used.</param>
    /// <returns>An instance of <see cref="PromptTemplate"/> that represents the system prompt template for the given context.</returns>
    public PromptTemplate GetSystemPrompt(PromptPersona? context = null)
    {
        context ??= DefaultPromptPersona;
        return context switch
        {
            PromptPersona.Business => BusinessPrompts.SystemPrompt,
            PromptPersona.Military => MilitaryPrompts.SystemPrompt,
            PromptPersona.Ocr => OcrPrompt.SystemPrompt,
            _ => BusinessPrompts.SystemPrompt
        };
    }

    /// <summary>
    /// Retrieves the title generation prompt from the default title generation prompts.
    /// </summary>
    /// <returns>
    /// A <see cref="PromptTemplate"/> containing the system-defined title generation prompt.
    /// </returns>
    public PromptTemplate GetTitleGenerationPrompt()
    {
        return TitleGenerationPrompts.SystemPrompt;
    }

    /// <summary>
    /// Retrieves a predefined prompt template used for condensing text
    /// or performing similar operations within the context.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="PromptTemplate"/> representing the condense prompt.
    /// </returns>
    public PromptTemplate GetCondensePrompt()
    {
        return SystemPrompts.CondensePrompt;
    }
}
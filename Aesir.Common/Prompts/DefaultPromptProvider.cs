using Aesir.Common.Models;
using Aesir.Common.Prompts.PromptCategories;

namespace Aesir.Common.Prompts;

/// <summary>
/// Provides default implementations for generating system prompts, title generation prompts,
/// and condensed prompts based on the specified context.
/// Implements the <see cref="IPromptProvider"/> interface.
/// </summary>
public class DefaultPromptProvider : IPromptProvider
{
    /// <summary>
    /// Retrieves a system prompt template based on the specified prompt context.
    /// </summary>
    /// <param name="context">The prompt context to determine the appropriate system prompt template.</param>
    /// <returns>A <see cref="PromptTemplate"/> object representing the system prompt for the given context.</returns>
    public PromptTemplate GetSystemPrompt(PromptContext context)
    {
        return context switch
        {
            PromptContext.Business => BusinessPrompts.SystemPrompt,
            PromptContext.Military => MilitaryPrompts.SystemPrompt,
            PromptContext.Ocr => OcrPrompt.SystemPrompt,
            _ => BusinessPrompts.SystemPrompt
        };
    }

    /// Retrieves the title generation prompt from the default title generation prompts.
    /// <returns>
    /// A PromptTemplate containing the system-defined title generation prompt.
    /// </returns>
    public PromptTemplate GetTitleGenerationPrompt()
    {
        return TitleGenerationPrompts.SystemPrompt;
    }

    /// <summary>
    /// Retrieves a pre-defined prompt template for condensing text or other operations.
    /// </summary>
    /// <returns>
    /// A <see cref="PromptTemplate"/> instance representing the condense prompt.
    /// </returns>
    public PromptTemplate GetCondensePrompt()
    {
        return SystemPrompts.CondensePrompt;
    }
}
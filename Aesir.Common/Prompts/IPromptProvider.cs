using System.ComponentModel;
using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

/// <summary>
/// Represents a provider for generating various types of prompt templates.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Retrieves a system-level prompt template based on the specified prompt context.
    /// </summary>
    /// <param name="context">The context used to determine the type of system prompt to retrieve.
    /// This could be Business, Military, Ocr, or other predefined contexts.</param>
    /// <returns>A prompt template corresponding to the provided context.</returns>
    PromptTemplate GetSystemPrompt(PromptContext context);

    /// <summary>
    /// Retrieves the system-defined prompt for title generation.
    /// </summary>
    /// <returns>
    /// A <see cref="PromptTemplate"/> instance containing the predefined content
    /// and variable placeholders for generating a title.
    /// </returns>
    PromptTemplate GetTitleGenerationPrompt();

    /// <summary>
    /// Retrieves a pre-defined prompt template for condensing text or other related operations.
    /// </summary>
    /// <returns>
    /// A PromptTemplate instance containing the content and variables for a condense operation.
    /// </returns>
    PromptTemplate GetCondensePrompt();
}

/// <summary>
/// Specifies the context in which a prompt is used.
/// </summary>
public enum PromptContext
{
    /// <summary>
    /// Represents a context for prompts related to business operations or scenarios.
    /// </summary>
    [Description("Business")]
    Business,

    /// <summary>
    /// Represents a military-focused prompt context. This context is used to provide prompts
    /// or templates tailored to scenarios, operations, or discussions within a military domain.
    /// </summary>
    [Description("Military")]
    Military,

    /// <summary>
    /// Represents the Optical Character Recognition (OCR) context in a prompt.
    /// This context is used when generating system prompts specific to OCR-related operations,
    /// such as extracting or processing text from images or scanned documents.
    /// </summary>
    [Description("Ocr")]
    Ocr
}
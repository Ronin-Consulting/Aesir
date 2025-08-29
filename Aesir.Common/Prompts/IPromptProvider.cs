using System.ComponentModel;
using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

/// <summary>
/// Represents an interface for providing various types of prompt templates,
/// including but not limited to system-level prompts, title generation prompts,
/// and condensed prompts. These prompts are leveraged for specific contextual needs.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Retrieves a system-level prompt template based on the specified prompt context.
    /// </summary>
    /// <param name="context">The context used to determine the type of system prompt.
    /// Examples include predefined contexts such as Business, Military, Ocr, or Custom.
    /// If no context is provided, a default context is used.</param>
    /// <returns>An instance of <see cref="PromptTemplate"/> corresponding to the specified context.</returns>
    PromptTemplate GetSystemPrompt(PromptPersona? context = null);

    /// <summary>
    /// Retrieves the system-defined prompt template used for generating titles.
    /// </summary>
    /// <returns>A <see cref="PromptTemplate"/> instance containing predefined content and placeholders for title generation.</returns>
    PromptTemplate GetTitleGenerationPrompt();

    /// <summary>
    /// Retrieves a predefined prompt template designed for condensing text
    /// or performing similar operations.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="PromptTemplate"/> representing the condense operation prompt.
    /// </returns>
    PromptTemplate GetCondensePrompt();
}

/// <summary>
/// Represents various personas or roles that can be used to generate context-specific prompts.
/// </summary>
public enum PromptPersona
{
    /// <summary>
    /// Represents a prompt persona focused on business contexts, suitable for corporate, professional, or enterprise use cases.
    /// </summary>
    [Description("Business")] Business,

    /// <summary>
    /// Represents prompts designed specifically for military-related contexts and operations.
    /// </summary>
    [Description("Military")] Military,

    /// <summary>
    /// Represents prompts designed for Optical Character Recognition (OCR) tasks and contexts,
    /// enabling interactions related to text extraction or analysis from visual formats.
    /// </summary>
    [Description("Ocr")] Ocr,

    /// <summary>
    /// Represents a user-defined or customized prompt context, allowing flexibility to tailor the prompt according to specific needs beyond predefined categories.
    /// </summary>
    [Description("Custom")] Custom,
}

/// <summary>
/// Provides extension methods for enhancing the functionality of the <see cref="PromptPersona"/> enum,
/// particularly for operations involving descriptions and mappings.
/// </summary>
public static class PromptPersonaExtensions
{
    /// <summary>
    /// Maps a description string to its corresponding <see cref="PromptPersona"/> enumeration value.
    /// </summary>
    /// <param name="description">The description string representing a valid prompt persona, such as "Business", "Military", "Ocr", or "Custom".</param>
    /// <returns>The <see cref="PromptPersona"/> value that matches the given description.</returns>
    /// <exception cref="ArgumentException">Thrown when no matching <see cref="PromptPersona"/> is found for the provided description.</exception>
    public static PromptPersona PromptPersonaFromDescription(this string description)
    {
        foreach (var persona in Enum.GetValues<PromptPersona>())
        {
            var attr = (DescriptionAttribute?)Attribute.GetCustomAttribute(
                persona.GetType().GetField(persona.ToString())!,
                typeof(DescriptionAttribute));

            if (attr?.Description.Equals(description, StringComparison.OrdinalIgnoreCase) == true)
            {
                return persona;
            }
        }

        throw new ArgumentException($"No PromptPersona found with description '{description}'.");
    }    
}

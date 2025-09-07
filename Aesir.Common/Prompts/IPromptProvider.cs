using System.ComponentModel;
using Aesir.Common.Models;

namespace Aesir.Common.Prompts;

/// <summary>
/// Defines an abstraction for providing prompt templates utilized in various contexts,
/// including system prompts, title generation prompts, and condensed prompts.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// Retrieves a system-level prompt template based on the specified prompt context.
    /// </summary>
    /// <param name="context">The context used to determine the type of system prompt.
    /// Examples include predefined contexts such as Business, Military, or Ocr.</param>
    /// <returns>An instance of <see cref="PromptTemplate"/> corresponding to the given context.</returns>
    PromptTemplate GetSystemPrompt(PromptPersona context);

    /// <summary>
    /// Retrieves the system-defined prompt template used for generating titles.
    /// </summary>
    /// <returns>A <see cref="PromptTemplate"/> that contains predefined content and placeholders necessary for title generation.</returns>
    PromptTemplate GetTitleGenerationPrompt();

    /// <summary>
    /// Retrieves a predefined prompt template designed for condensing text
    /// or executing related operations.
    /// </summary>
    /// <returns>
    /// A PromptTemplate instance representing the condense operation prompt.
    /// </returns>
    PromptTemplate GetCondensePrompt();
}

/// <summary>
/// Defines various personas or contexts in which prompts can be generated.
/// </summary>
public enum PromptPersona
{
    /// <summary>
    /// Designates prompts tailored towards business-related contexts and applications.
    /// </summary>
    [Description("Business")] Business,

    /// <summary>
    /// Represents a context for prompts related to military operations, strategies, or scenarios.
    /// </summary>
    [Description("Military")] Military,

    /// <summary>
    /// Represents a context for prompts related to Optical Character Recognition (OCR) operations or scenarios.
    /// </summary>
    [Description("Ocr")] Ocr
}

/// <summary>
/// Provides extension methods for the <see cref="PromptPersona"/> enum to facilitate additional functionality,
/// such as mapping description strings to their respective enum values.
/// </summary>
public static class PromptPersonaExtensions
{
    /// <summary>
    /// Converts a description string to its corresponding <see cref="PromptPersona"/> enum value.
    /// </summary>
    /// <param name="description">The description string that represents a valid prompt persona, such as "Business", "Military", or "Ocr".</param>
    /// <returns>The <see cref="PromptPersona"/> value that matches the provided description string.</returns>
    /// <exception cref="ArgumentException">Thrown if no matching <see cref="PromptPersona"/> is found for the given description.</exception>
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

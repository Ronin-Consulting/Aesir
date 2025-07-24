using HandlebarsDotNet;

namespace Aesir.Common.Models;

/// <summary>
/// Represents a template for generating dynamic text output by combining static content
/// and variable placeholders with their corresponding values.
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// Gets or sets the content of the prompt or message.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Represents a collection of key-value pairs that can be used to store and reference variable data
    /// within a <see cref="PromptTemplate"/>. The keys are the variable names, and the values represent
    /// their associated string data.
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = new();

    /// <summary>
    /// Represents a template for constructing prompts.
    /// </summary>
    /// <remarks>
    /// This class is used to define the content of a prompt which can be utilized in various contexts.
    /// It encapsulates the text content to be used as the basis for prompting mechanisms.
    /// </remarks>
    public PromptTemplate(string content)
    {
        Content = content;
    }

    /// Renders the content of a template by replacing placeholders with their corresponding
    /// values from the provided variables. If no additional variables are supplied,
    /// it uses the default variables defined in the template.
    /// <param name="variables">
    /// An optional dictionary of additional key-value pairs to replace placeholders.
    /// If null, only the pre-defined variables in the template are used.
    /// </param>
    /// <returns>
    /// The rendered string with all placeholders replaced by their corresponding values.
    /// </returns>
    public string Render(Dictionary<string, object>? variables = null)
    {
        // our helpers... as this grows we should pull this out
        Handlebars.RegisterHelper("or", (writer, context, parameters) =>
        {
            foreach (var param in parameters)
            {
                // Check if the parameter is truthy (non-null, non-false, non-empty, etc.)
                if (HandlebarsUtils.IsTruthy(param))
                {
                    writer.Write(true); // Output true to trigger the if block
                    return;
                }
            }
            writer.Write(false); // All falsey, so output false
        });
        
        var template = Handlebars.Compile(Content);
        
        var allVariables = Variables.ToDictionary(kvp => kvp.Key, object (kvp) => kvp.Value);

        if (variables == null) return template(allVariables);
        
        foreach (var variable in variables)
        {
            allVariables[variable.Key] = variable.Value;
        }

        return template(allVariables);
    }

    /// Provides implicit conversion from a string to a PromptTemplate object.
    /// Allows a string to be converted directly to a PromptTemplate instance
    /// where the string represents the content of the template.
    public static implicit operator PromptTemplate(string content) => new(content);
}
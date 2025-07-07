using Aesir.Common.Prompts;

namespace Aesir.Api.Server.Services;

/// <summary>
/// Provides a centralized library for accessing system and condense prompt templates.
/// </summary>
public class PromptLibrary
{
    /// <summary>
    /// The prompt provider used to retrieve prompts.
    /// </summary>
    private static readonly IPromptProvider PromptProvider = new DefaultPromptProvider();

    /// <summary>
    /// Gets the default system prompt template for business context.
    /// </summary>
    public static string DefaultSystemPromptTemplate => 
        PromptProvider.GetSystemPrompt(PromptContext.Business).Content;

    /// <summary>
    /// Gets the default condense prompt template.
    /// </summary>
    public static string DefaultCondensePromptTemplate => 
        PromptProvider.GetCondensePrompt().Content;
}
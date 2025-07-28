using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// Defines a service for processing Markdown content, including rendering it
/// into various formats such as HTML or plain text.
public interface IMarkdownService
{
    /// <summary>
    /// Asynchronously converts the provided Markdown string to its HTML equivalent.
    /// </summary>
    /// <param name="markdown">The Markdown-formatted string that needs to be rendered as HTML.</param>
    /// <returns>A task that represents the asynchronous operation, producing a string containing the rendered HTML output.</returns>
    Task<string> RenderMarkdownAsHtmlAsync(string markdown);

    /// <summary>
    /// Converts the provided Markdown string into its plain text representation asynchronously.
    /// </summary>
    /// <param name="markdown">The Markdown content to be converted into plain text.</param>
    /// <returns>A task representing the asynchronous operation, with a string result containing the converted plain text.</returns>
    Task<string> RenderMarkdownAsPlainTextAsync(string markdown);
}
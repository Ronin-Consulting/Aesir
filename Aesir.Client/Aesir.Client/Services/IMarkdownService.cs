using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// Represents a service for processing and rendering Markdown content into HTML.
public interface IMarkdownService
{
    /// <summary>
    /// Converts the provided Markdown string into its corresponding HTML representation asynchronously.
    /// </summary>
    /// <param name="markdown">The Markdown text to be converted into HTML.</param>
    /// <returns>A task representing the asynchronous operation, with a string result containing the converted HTML.</returns>
    Task<string> RenderMarkdownAsHtmlAsync(string markdown);
}
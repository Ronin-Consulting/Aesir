using System.Threading.Tasks;

namespace Aesir.Client.Services;

/// Represents a service interface for processing and rendering Markdown content.
/// This interface provides methods for converting Markdown into formats such
/// as HTML or plain text, supporting various rendering options.
public interface IMarkdownService
{
    /// <summary>
    /// Asynchronously converts the provided Markdown string to its equivalent HTML representation.
    /// </summary>
    /// <param name="markdown">The Markdown-formatted string to be converted.</param>
    /// <param name="shouldRenderFencedCodeBlocks">
    /// Indicates whether fenced code blocks in the Markdown content should be rendered into corresponding HTML.
    /// </param>
    /// <returns>A task that represents the asynchronous operation, producing a string containing the HTML representation of the Markdown content.</returns>
    Task<string> RenderMarkdownAsHtmlAsync(string markdown, bool shouldRenderFencedCodeBlocks = false);

    /// <summary>
    /// Asynchronously converts the provided Markdown string to its plain text equivalent.
    /// </summary>
    /// <param name="markdown">The Markdown-formatted string to be rendered as plain text.</param>
    /// <returns>A task that represents the asynchronous operation, producing a string containing the plain text output.</returns>
    Task<string> RenderMarkdownAsPlainTextAsync(string markdown);
}
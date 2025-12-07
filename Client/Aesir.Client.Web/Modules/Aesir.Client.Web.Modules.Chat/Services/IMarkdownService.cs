namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for converting markdown text to HTML.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts markdown text to sanitized HTML.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>HTML string suitable for rendering.</returns>
    string ToHtml(string? markdown);
}

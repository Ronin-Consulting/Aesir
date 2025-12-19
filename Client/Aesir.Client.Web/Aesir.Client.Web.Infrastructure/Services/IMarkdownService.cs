namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for converting markdown text to HTML.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Converts markdown text to sanitized HTML.
    /// Standard markdown behavior where single newlines become spaces.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>HTML string suitable for rendering.</returns>
    string ToHtml(string? markdown);

    /// <summary>
    /// Converts markdown text to sanitized HTML with whitespace preservation.
    /// Single newlines become &lt;br&gt; tags and multiple spaces are preserved.
    /// Ideal for user-entered content where formatting should be maintained.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>HTML string suitable for rendering with preserved whitespace.</returns>
    string ToHtmlPreservingWhitespace(string? markdown);
}

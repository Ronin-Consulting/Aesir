using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

/// <summary>
/// Custom link renderer for Markdig markdown library.
/// </summary>
/// <remarks>
/// This class extends the default HTML object renderer for handling <see cref="LinkInline"/> elements
/// within the Markdig rendering pipeline. It provides custom processing for links based on their type:
/// - Standard links (e.g., HTTP/HTTPS) are rendered with appropriate escaping.
/// - Links with "file://" URLs are rendered with custom attributes and href.
/// - Image links are rendered unchanged, maintaining the standard image tag syntax.
/// </remarks>
/// <see cref="HtmlObjectRenderer{LinkInline}"/>
/// <see cref="LinkInline"/>
public class AesirLinkRenderer : HtmlObjectRenderer<LinkInline>
{
    /// Renders a Markdown link inline element into its corresponding HTML representation.
    /// The method handles different types of links such as images, file protocol links,
    /// and standard web links.
    /// <param name="renderer">
    /// An instance of <see cref="Markdig.Renderers.HtmlRenderer"/> used for writing
    /// the resulting HTML output.
    /// </param>
    /// <param name="link">
    /// An instance of <see cref="Markdig.Syntax.Inlines.LinkInline"/> representing
    /// the Markdown link to be rendered.
    /// </param>
    protected override void Write(Markdig.Renderers.HtmlRenderer renderer, LinkInline link)
    {
        if (link.IsImage)
        {
            // Leave image handling unchanged
            renderer.Write("<img src=\"").WriteEscapeUrl(link.Url).Write("\" alt=\"");
            renderer.WriteEscape(link.FirstChild?.ToString() ?? "").Write("\" />");
            return;
        }

        if (link.Url?.StartsWith("file://") == true)
        {
            // write href with link that goes nowhere as we are going to handle it
            renderer.Write("<a href=\"#\"")
                    .Write($" data-href=\"{link.Url}\"")
                    .Write(">");
            renderer.WriteEscape(link.FirstChild?.ToString() ?? link.Url);
            renderer.Write("</a>");
        }
        else
        {
            // Default link rendering for http, https, relative, etc.
            renderer.Write("<a href=\"");
            renderer.WriteEscapeUrl(link.GetDynamicUrl != null ? link.GetDynamicUrl() ?? string.Empty : link.Url ?? string.Empty);
            renderer.Write('"');
            if (link.IsAutoLink)
            {
                renderer.Write(" class=\"autolink\"");
            }
            renderer.Write(">");
            renderer.WriteEscape(link.FirstChild?.ToString() ?? link.Url ?? string.Empty);
            renderer.Write("</a>");
        }
    }
}
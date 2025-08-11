using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

public class AesirLinkRenderer : HtmlObjectRenderer<LinkInline>
{
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
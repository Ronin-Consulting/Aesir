using System.Text.RegularExpressions;
using Markdig;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for converting markdown text to HTML using Markdig.
/// </summary>
public partial class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    // Regex to match <pre><code> blocks and wrap them with copy functionality
    [GeneratedRegex(@"<pre><code(?:\s+class=""language-(\w+)"")?>([\s\S]*?)</code></pre>", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseTaskLists()
            .UseEmojiAndSmiley()
            .Build();
    }

    /// <inheritdoc />
    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(markdown, _pipeline);

        // Wrap code blocks with copy button container
        html = CodeBlockRegex().Replace(html, match =>
        {
            var language = match.Groups[1].Success ? match.Groups[1].Value : "";
            var code = match.Groups[2].Value;
            var languageLabel = string.IsNullOrEmpty(language) ? "" : $"<span class=\"code-language\">{language}</span>";
            var uniqueId = Guid.NewGuid().ToString("N")[..8];

            return $@"<div class=""code-block-wrapper"">
                <div class=""code-block-header"">
                    {languageLabel}
                    <button class=""code-copy-btn"" onclick=""copyCodeBlock('{uniqueId}')"" title=""Copy code"">
                        <svg xmlns=""http://www.w3.org/2000/svg"" width=""16"" height=""16"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round"">
                            <rect x=""9"" y=""9"" width=""13"" height=""13"" rx=""2"" ry=""2""></rect>
                            <path d=""M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1""></path>
                        </svg>
                        <span class=""copy-text"">Copy</span>
                    </button>
                </div>
                <pre><code id=""code-{uniqueId}"" class=""{(string.IsNullOrEmpty(language) ? "" : $"language-{language}")}"">{code}</code></pre>
            </div>";
        });

        return html;
    }
}

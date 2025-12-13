using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Markdig;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for converting markdown text to HTML using Markdig.
/// </summary>
public partial class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownPipeline _whitespacePreservingPipeline;

    // Regex to match <pre><code> blocks and wrap them with copy functionality
    [GeneratedRegex(@"<pre><code(?:\s+class=""language-(\w+)"")?>([\s\S]*?)</code></pre>", RegexOptions.Compiled)]
    private static partial Regex CodeBlockRegex();

    // Regex to match two or more consecutive spaces (for preserving in user messages)
    [GeneratedRegex(@"  +", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    // Regex to match anchor tags with file:// URLs (citation links)
    // Matches: <a href="file:///guid/filename#page=N">text</a>
    // Groups: 1=full URL, 2=GUID, 3=filename (may include #page=N), 4=link text
    [GeneratedRegex(
        @"<a\s+href=""(file:///([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/([^""]+))"">([^<]*)</a>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CitationLinkRegex();

    // Regex to extract page number from filename or fragment
    [GeneratedRegex(@"#page=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PageFragmentRegex();

    // Regex to match anchor tags with http/https URLs (external links)
    // Excludes file:// URLs which are handled separately by CitationLinkRegex
    [GeneratedRegex(
        @"<a\s+href=""(https?://[^""]+)"">([^<]*)</a>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ExternalLinkRegex();

    public MarkdownService()
    {
        // Standard pipeline for assistant messages
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseTaskLists()
            .UseEmojiAndSmiley()
            .Build();

        // Whitespace-preserving pipeline for user messages
        // UseSoftlineBreakAsHardlineBreak converts single newlines to <br> tags
        _whitespacePreservingPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseTaskLists()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
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

        // Transform citation links (file:// URLs) to interactive elements
        html = TransformCitationLinks(html);

        // Transform external links (http/https) to open in new tab
        html = TransformExternalLinks(html);

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

    /// <inheritdoc />
    public string ToHtmlPreservingWhitespace(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        // Pre-process: convert tabs and multiple consecutive spaces to non-breaking spaces
        var preprocessed = PreserveWhitespace(markdown);

        var html = Markdown.ToHtml(preprocessed, _whitespacePreservingPipeline);

        // Apply same transformations as standard pipeline
        html = TransformCitationLinks(html);
        html = TransformExternalLinks(html);

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

    /// <summary>
    /// Preserves whitespace by converting tabs to spaces and multiple consecutive
    /// spaces to alternating non-breaking spaces and regular spaces.
    /// </summary>
    private static string PreserveWhitespace(string text)
    {
        // First, convert tabs to 4 non-breaking spaces (standard tab width)
        text = text.Replace("\t", "\u00A0\u00A0\u00A0\u00A0");

        // Then, convert multiple consecutive spaces to alternating nbsp and regular space
        return MultipleSpacesRegex().Replace(text, match =>
        {
            var spaces = match.Length;
            var result = new StringBuilder(spaces);
            for (var i = 0; i < spaces; i++)
            {
                // Alternate between non-breaking space and regular space
                result.Append(i % 2 == 0 ? '\u00A0' : ' ');
            }
            return result.ToString();
        });
    }

    /// <summary>
    /// Transforms file:// citation links to interactive elements with data attributes.
    /// </summary>
    private static string TransformCitationLinks(string html)
    {
        return CitationLinkRegex().Replace(html, match =>
        {
            var fullUrl = match.Groups[1].Value;
            var conversationId = match.Groups[2].Value;
            var fileNameWithFragment = match.Groups[3].Value;
            var linkText = match.Groups[4].Value;

            // URL decode the filename
            var decodedFileName = HttpUtility.UrlDecode(fileNameWithFragment);

            // Extract page number if present
            var pageMatch = PageFragmentRegex().Match(decodedFileName);
            var pageNumber = pageMatch.Success ? pageMatch.Groups[1].Value : "";

            // Get clean filename (without fragment)
            var fileName = decodedFileName;
            var hashIndex = fileName.IndexOf('#');
            if (hashIndex >= 0)
            {
                fileName = fileName[..hashIndex];
            }

            // Get file type styling from helper
            var fileStyle = FileTypeHelper.GetStyle(fileName);
            var fileIcon = GetFileTypeIcon(fileStyle.Category);
            var fileCategory = fileStyle.Category.ToString().ToLowerInvariant();
            var fileExtension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

            // Build data attributes including file category for CSS styling and extension for type detection
            var dataAttrs = $@"data-conversation-id=""{HttpUtility.HtmlEncode(conversationId)}"" data-filename=""{HttpUtility.HtmlEncode(fileName)}"" data-category=""{fileCategory}"" data-extension=""{HttpUtility.HtmlEncode(fileExtension)}""";
            if (!string.IsNullOrEmpty(pageNumber))
            {
                dataAttrs += $@" data-page=""{pageNumber}""";
            }

            // Page badge for PDFs
            var pageBadge = !string.IsNullOrEmpty(pageNumber)
                ? $@"<span class=""citation-page"">p.{pageNumber}</span>"
                : "";

            // Return transformed link with citation styling and file-type class
            return $@"<a href=""javascript:void(0)"" class=""citation-link citation-{fileCategory}"" {dataAttrs} onclick=""window.aesirCitationHandler?.openCitation(this)"" title=""View: {HttpUtility.HtmlEncode(fileName)}{(string.IsNullOrEmpty(pageNumber) ? "" : $" (page {pageNumber})")}"">{fileIcon}<span class=""citation-text"">{HttpUtility.HtmlEncode(linkText)}</span>{pageBadge}</a>";
        });
    }

    /// <summary>
    /// Transforms external http/https links to open in new tab with proper security attributes.
    /// For Tauri, adds onclick handler to use system browser instead of webview navigation.
    /// </summary>
    private static string TransformExternalLinks(string html)
    {
        return ExternalLinkRegex().Replace(html, match =>
        {
            var url = match.Groups[1].Value;
            var linkText = match.Groups[2].Value;

            // Add target="_blank" for browser, onclick for Tauri to use system browser
            // rel="noopener noreferrer" prevents tab-nabbing security issues
            return $@"<a href=""{HttpUtility.HtmlEncode(url)}"" target=""_blank"" rel=""noopener noreferrer"" onclick=""window.aesirExternalLink?.open(event, this.href)"" class=""external-link"">{linkText}</a>";
        });
    }

    /// <summary>
    /// Gets an SVG icon for the file type based on category.
    /// </summary>
    private static string GetFileTypeIcon(FileTypeHelper.FileCategory category)
    {
        return category switch
        {
            FileTypeHelper.FileCategory.Pdf =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><path d=""M9 15h6""/><path d=""M9 11h6""/></svg>",

            FileTypeHelper.FileCategory.Image =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2"" ry=""2""/><circle cx=""8.5"" cy=""8.5"" r=""1.5""/><polyline points=""21 15 16 10 5 21""/></svg>",

            FileTypeHelper.FileCategory.Data =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><line x1=""16"" y1=""13"" x2=""8"" y2=""13""/><line x1=""16"" y1=""17"" x2=""8"" y2=""17""/><polyline points=""10 9 9 9 8 9""/></svg>",

            FileTypeHelper.FileCategory.Document =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/><line x1=""16"" y1=""13"" x2=""8"" y2=""13""/><line x1=""16"" y1=""17"" x2=""8"" y2=""17""/></svg>",

            FileTypeHelper.FileCategory.Code =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><polyline points=""16 18 22 12 16 6""/><polyline points=""8 6 2 12 8 18""/></svg>",

            FileTypeHelper.FileCategory.Archive =>
                @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z""/><line x1=""12"" y1=""11"" x2=""12"" y2=""17""/><line x1=""9"" y1=""14"" x2=""15"" y2=""14""/></svg>",

            _ => @"<svg class=""citation-icon"" xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 24 24"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z""/><polyline points=""14 2 14 8 20 8""/></svg>"
        };
    }
}

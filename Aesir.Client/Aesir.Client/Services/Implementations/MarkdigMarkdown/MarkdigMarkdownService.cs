using System.Threading.Tasks;
using ColorCode.Styling;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

/// <summary>
/// Service class for handling Markdown rendering operations, such as converting
/// Markdown content into HTML and plain text, utilizing the Markdig library.
/// Implements the <see cref="IMarkdownService"/> interface.
/// </summary>
public class MarkdigMarkdownService(ILogger<MarkdigMarkdownService> logger) : IMarkdownService
{
    /// <summary>
    /// Used for logging diagnostic messages and runtime information within the
    /// <see cref="MarkdigMarkdownService"/> class. Facilitates tracking and debugging
    /// Markdown rendering processes and other operations of the service.
    /// </summary>
    private readonly ILogger<MarkdigMarkdownService> _logger = logger;

    /// <summary>
    /// Represents the Markdown processing pipeline used for rendering Markdown content to HTML or
    /// plain text within the <see cref="MarkdigMarkdownService"/> class.
    /// </summary>
    /// <remarks>
    /// This pipeline is configured with a set of extensions to enhance Markdown rendering, including:
    /// support for pipe tables, advanced Markdown features, syntax highlighting, automatic links,
    /// emoji parsing, media links, and citations.
    /// </remarks>
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAdvancedExtensions()
        .UseColorCode(styleDictionary: StyleDictionary.DefaultDark)
        .UseAutoLinks()
        .UseEmojiAndSmiley()
        .UseMediaLinks()
        .UseCitations()
        .Build();

    /// <summary>
    /// Converts the provided Markdown string to its equivalent HTML representation asynchronously.
    /// </summary>
    /// <param name="markdown">The input Markdown string to be converted to HTML.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTML string generated from the Markdown input.</returns>
    public Task<string> RenderMarkdownAsHtmlAsync(string markdown)
    {
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        //_logger.LogDebug("Rendered markdown to HTML: {html}", html);

        return Task.FromResult(html);
    }

    /// <summary>
    /// Converts the provided Markdown string into its plain text representation asynchronously.
    /// </summary>
    /// <param name="markdown">The Markdown text to be converted into plain text.</param>
    /// <returns>A task representing the asynchronous operation, with a string result containing the converted plain text.</returns>
    public Task<string> RenderMarkdownAsPlainTextAsync(string markdown)
    {
        var plainText = Markdig.Markdown.ToPlainText(markdown, _pipeline);
        
        return Task.FromResult(plainText);
    }
}
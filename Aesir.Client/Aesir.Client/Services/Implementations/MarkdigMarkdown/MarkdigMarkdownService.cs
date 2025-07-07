using System.Threading.Tasks;
using ColorCode.Styling;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

/// Service class for rendering Markdown to HTML using the Markdig library.
public class MarkdigMarkdownService(ILogger<MarkdigMarkdownService> logger) : IMarkdownService
{
    /// <summary>
    /// Represents the logging mechanism used in the <see cref="MarkdigMarkdownService"/> class.
    /// This logger is used to log diagnostic messages and runtime information related to the
    /// Markdown rendering operations or other internal activities within the service.
    /// </summary>
    private readonly ILogger<MarkdigMarkdownService> _logger = logger;

    /// <summary>
    /// Represents the Markdown processing pipeline used for rendering Markdown to HTML.
    /// </summary>
    /// <remarks>
    /// The pipeline is configured with various extensions, including support for:
    /// - Pipe tables
    /// - Advanced Markdown extensions
    /// - Syntax highlighting using ColorCode with a dark style dictionary
    /// - Automatic link generation
    /// - Emoji and smiley parsing
    /// - Media links
    /// - Citations
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
    /// Converts the provided Markdown string into its corresponding HTML representation asynchronously.
    /// </summary>
    /// <param name="markdown">The Markdown text to be converted into HTML.</param>
    /// <returns>A task representing the asynchronous operation, with a string result containing the converted HTML.</returns>
    public Task<string> RenderMarkdownAsHtmlAsync(string markdown)
    {
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        //_logger.LogDebug("Rendered markdown to HTML: {html}", html);

        return Task.FromResult(html);
    }
}
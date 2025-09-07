using System.IO;
using System.Text;
using System.Threading.Tasks;
using ColorCode.Styling;
using Markdig;
using Markdig.Renderers.Html.Inlines;
using Markdown.ColorCode;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

/// <summary>
/// Service class for processing Markdown content, supporting conversion to both HTML
/// and plain text formats. Utilizes the Markdig library to parse and render the Markdown,
/// enabling customization and extensibility of the Markdown rendering pipeline.
/// Implements the <see cref="IMarkdownService"/> interface.
/// </summary>
public class MarkdigMarkdownService(ILogger<MarkdigMarkdownService> logger) : IMarkdownService
{
    /// <summary>
    /// Provides a mechanism for logging diagnostic information, errors, warnings, and other runtime
    /// details specific to the operations of the <see cref="MarkdigMarkdownService"/> class, aiding
    /// in debugging and monitoring Markdown rendering workflows.
    /// </summary>
    private readonly ILogger<MarkdigMarkdownService> _logger = logger;

    /// <summary>
    /// Converts the provided Markdown string to its equivalent HTML representation asynchronously.
    /// </summary>
    /// <param name="markdown">The input Markdown string to be converted to HTML.</param>
    /// <param name="shouldRenderFencedCodeBlocks">A boolean flag indicating whether fenced code blocks in the Markdown should be rendered.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the HTML string generated from the Markdown input.</returns>
    public Task<string> RenderMarkdownAsHtmlAsync(string markdown, bool shouldRenderFencedCodeBlocks = false)
    {
        var writer = new StringWriter(new StringBuilder(100000));
        var renderer = new Markdig.Renderers.HtmlRenderer(writer);

        renderer.ObjectRenderers.RemoveAll(r => r is LinkInlineRenderer);
        renderer.ObjectRenderers.Add(new AesirLinkRenderer());

        var pipeline = GetMarkdownPipeline(shouldRenderFencedCodeBlocks);
        pipeline.Setup(renderer);

        var doc = Markdig.Markdown.Parse(markdown, pipeline);
        renderer.Render(doc);
        writer.Flush();

        var html = writer.ToString();

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
        var plainText = Markdig.Markdown.ToPlainText(markdown, GetMarkdownPipeline());

        return Task.FromResult(plainText);
    }

    /// <summary>
    /// Creates and configures a Markdown pipeline for processing Markdown content,
    /// optionally supporting syntax highlighting for fenced code blocks.
    /// </summary>
    /// <param name="useColorCode">Indicates whether the pipeline should enable syntax highlighting for fenced code blocks using ColorCode.</param>
    /// <returns>A configured instance of <see cref="MarkdownPipeline"/> for processing Markdown content.</returns>
    private MarkdownPipeline GetMarkdownPipeline(bool useColorCode = false)
    {
        var builder = new MarkdownPipelineBuilder()
            .UseEmojiAndSmiley()
            .UseSoftlineBreakAsHardlineBreak()
            .UseDiagrams()
            .EnableTrackTrivia()
            //.UseJiraLinks()
            // The next lines are just UseAdvancedExtensions() so we dont dup them
            //.UseAdvancedExtensions()
            .UseAlertBlocks()
            .UseAbbreviations()
            .UseAutoIdentifiers()
            .UseCitations()
            .UseCustomContainers()
            .UseDefinitionLists()
            .UseEmphasisExtras()
            .UseFigures()
            .UseFooters()
            .UseFootnotes()
            .UseGridTables()
            .UseMathematics()
            .UseMediaLinks()
            .UsePipeTables()
            .UseListExtras()
            .UseTaskLists()
            .UseDiagrams()
            .UseAutoLinks()
            .UseGenericAttributes();

        if (useColorCode)
            builder.UseColorCode(styleDictionary: StyleDictionary.DefaultDark);
        
        return builder.Build();
    }
}
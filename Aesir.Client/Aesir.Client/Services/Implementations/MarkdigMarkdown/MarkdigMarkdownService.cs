using System.Threading.Tasks;
using ColorCode.Styling;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

public class MarkdigMarkdownService(ILogger<MarkdigMarkdownService> logger) : IMarkdownService
{
    private readonly ILogger<MarkdigMarkdownService> _logger = logger;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAdvancedExtensions()
        .UseColorCode(styleDictionary: StyleDictionary.DefaultDark)
        .UseAutoLinks()
        .UseEmojiAndSmiley()
        .UseMediaLinks()
        .UseCitations()
        .Build();

    public Task<string> RenderMarkdownAsHtmlAsync(string markdown)
    {
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        //_logger.LogDebug("Rendered markdown to HTML: {html}", html);
        
        return Task.FromResult(html);
    }
}
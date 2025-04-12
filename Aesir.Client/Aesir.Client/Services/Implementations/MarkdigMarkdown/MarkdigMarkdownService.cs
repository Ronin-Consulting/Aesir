using System.Threading.Tasks;
using ColorCode.Styling;
using Markdig;
using Markdown.ColorCode;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.MarkdigMarkdown;

public class MarkdigMarkdownService : IMarkdownService
{
    private readonly ILogger<MarkdigMarkdownService> _logger;
    private readonly MarkdownPipeline _pipeline;
    
    public MarkdigMarkdownService(ILogger<MarkdigMarkdownService> logger)
    {
        _logger = logger;
        _pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseAdvancedExtensions()
            .UseColorCode(styleDictionary: StyleDictionary.DefaultDark)
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .UseMediaLinks()
            .UseCitations()
            .Build();
    }

    public Task<string> RenderMarkdownAsHtmlAsync(string markdown)
    {
        var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        //_logger.LogDebug("Rendered markdown to HTML: {html}", html);
        
        return Task.FromResult(html);
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

public class ContentProcessingService : IContentProcessingService
{
    private readonly IPdfViewerService _pdfViewerService;
    private readonly ILogger<ContentProcessingService> _logger;

    public ContentProcessingService(
        IPdfViewerService pdfViewerService,
        ILogger<ContentProcessingService> logger)
    {
        _pdfViewerService = pdfViewerService ?? throw new ArgumentNullException(nameof(pdfViewerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string ProcessThinkingModelContent(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        try
        {
            // Extract anything between <think> tags (including the tags themselves)
            var startIndex = input.IndexOf("<think>", StringComparison.InvariantCultureIgnoreCase);

            if (startIndex < 0) return input;

            var endIndex = input.IndexOf("</think>", startIndex, StringComparison.InvariantCultureIgnoreCase);

            if (endIndex >= 0)
            {
                // Log the thinking content for debugging
                var thinkingContent = input.Substring(startIndex + "<think>".Length, endIndex - startIndex - "<think>".Length);
                _logger.LogDebug("Thinking model content: {ThinkingContent}", thinkingContent);

                // Remove everything from start of <think> to end of </think> (including tags)
                input = input.Remove(startIndex, (endIndex + "</think>".Length) - startIndex);
            }
            else
            {
                // If closing tag is missing, just remove the opening tag as before
                _logger.LogWarning("Unclosed <think> tag found in content");
                input = input.Replace("<think>", "");
            }

            return input;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing thinking model content");
            return input; // Return original input if processing fails
        }
    }

    public void HandleLinkClick(string link, Dictionary<string, string> attributes)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                _logger.LogWarning("Attempted to handle empty link click");
                return;
            }

            _pdfViewerService.ShowPdfAsync(link);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling link click for: {Link}", link);
        }
    }
}
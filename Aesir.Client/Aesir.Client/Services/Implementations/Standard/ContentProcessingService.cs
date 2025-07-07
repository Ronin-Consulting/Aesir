using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Aesir.Client.Services.Implementations.Standard;

/// <summary>
/// Provides implementation for processing and handling specific content operations,
/// such as extracting and removing specific tagged sections and handling link interactions.
/// </summary>
public class ContentProcessingService(
    IPdfViewerService pdfViewerService,
    ILogger<ContentProcessingService> logger)
    : IContentProcessingService
{
    /// <summary>
    /// Represents the service responsible for displaying PDF files.
    /// </summary>
    private readonly IPdfViewerService _pdfViewerService =
        pdfViewerService ?? throw new ArgumentNullException(nameof(pdfViewerService));

    /// <summary>
    /// Provides logging capabilities for the <see cref="ContentProcessingService"/> class.
    /// Used to log debug information, warnings, and errors encountered during content processing
    /// and link handling operations.
    /// </summary>
    private readonly ILogger<ContentProcessingService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Processes the provided input string to remove any content enclosed within <think> tags,
    /// including the tags themselves. Logs the extracted thinking model content for debugging purposes
    /// and handles potential formatting issues such as unclosed tags gracefully.
    /// </summary>
    /// <param name="input">The string containing the content to be processed.</param>
    /// <returns>
    /// A string with the content enclosed within <think> tags removed.
    /// If the input is null or empty, or if no <think> tags are present, the original input is returned.
    /// </returns>
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
                var thinkingContent =
                    input.Substring(startIndex + "<think>".Length, endIndex - startIndex - "<think>".Length);
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

    /// <summary>
    /// Handles the event of a link being clicked, performing any necessary processing
    /// and delegating actions based on the provided link and attributes.
    /// </summary>
    /// <param name="link">The URL or identifier of the link that was clicked.</param>
    /// <param name="attributes">A dictionary containing attributes related to the link, such as metadata values or associated parameters.</param>
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
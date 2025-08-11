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
    /// Handles the event of a link being clicked, performing any necessary processing
    /// and delegating actions based on the provided link and attributes.
    /// </summary>
    /// <param name="link">The URL or identifier of the link that was clicked.</param>
    /// <param name="attributes">A dictionary containing attributes related to the link, such as metadata values or associated parameters.</param>
    public void HandleLinkClick(string link, Dictionary<string, string> attributes)
    {
        try
        {
            // only process aesir rewritten uris
            if (attributes.ContainsKey("data-href"))
            {
                _pdfViewerService.ShowPdfAsync(attributes["data-href"]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling link click for: {Link}", link);
        }
    }
}
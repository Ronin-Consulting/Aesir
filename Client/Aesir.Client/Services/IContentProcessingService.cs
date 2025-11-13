using System.Collections.Generic;

namespace Aesir.Client.Services;

/// <summary>
/// Defines the contract for processing and handling content-related operations within the application.
/// </summary>
public interface IContentProcessingService
{
    /// <summary>
    /// Handles the event of a link being clicked, performing processing
    /// based on the specified link and its associated attributes.
    /// </summary>
    /// <param name="link">The URL or identifier of the link to process.</param>
    /// <param name="attributes">A dictionary containing key-value attributes related to the link, such as additional metadata or parameters.</param>
    void HandleLinkClick(string link, Dictionary<string, string> attributes);
}
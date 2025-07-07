using System.Collections.Generic;

namespace Aesir.Client.Services;

/// <summary>
/// Defines the contract for processing and handling content-related operations within the application.
/// </summary>
public interface IContentProcessingService
{
    /// <summary>
    /// Processes the provided input string by removing content enclosed within &lt;think&gt; tags,
    /// including the tags themselves. Handles potential formatting issues such as unclosed or nested
    /// tags, gracefully ensuring robust processing.
    /// </summary>
    /// <param name="input">The string containing content to be processed for &lt;think&gt; tags.</param>
    /// <returns>
    /// A string with all &lt;think&gt; tags and their enclosed content removed. If the input is null,
    /// empty, or contains no &lt;think&gt; tags, the original input string is returned.
    /// </returns>
    string ProcessThinkingModelContent(string input);

    /// <summary>
    /// Handles the event of a link being clicked, performing processing
    /// based on the specified link and its associated attributes.
    /// </summary>
    /// <param name="link">The URL or identifier of the link to process.</param>
    /// <param name="attributes">A dictionary containing key-value attributes related to the link, such as additional metadata or parameters.</param>
    void HandleLinkClick(string link, Dictionary<string, string> attributes);
}
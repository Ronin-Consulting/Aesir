using Aesir.Client.Web.Modules.Chat.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing citation viewer state across components.
/// </summary>
public interface ICitationStateService
{
    /// <summary>
    /// Gets the currently viewed citation, or null if the viewer is closed.
    /// </summary>
    CitationInfo? CurrentCitation { get; }

    /// <summary>
    /// Gets whether the citation viewer is currently open.
    /// </summary>
    bool IsViewerOpen { get; }

    /// <summary>
    /// Event fired when the current citation changes (opened, closed, or changed).
    /// </summary>
    event Action? OnCitationChanged;

    /// <summary>
    /// Event fired when the viewer is requested to open.
    /// </summary>
    event Action<CitationInfo>? OnCitationOpened;

    /// <summary>
    /// Event fired when the viewer is requested to close.
    /// </summary>
    event Action? OnCitationClosed;

    /// <summary>
    /// Opens the citation viewer with the specified citation.
    /// </summary>
    /// <param name="citation">The citation to view.</param>
    Task OpenCitationAsync(CitationInfo citation);

    /// <summary>
    /// Opens the citation viewer by parsing a citation URL.
    /// </summary>
    /// <param name="url">The citation URL to parse and open.</param>
    /// <returns>True if the URL was a valid citation and was opened, false otherwise.</returns>
    Task<bool> OpenCitationByUrlAsync(string url);

    /// <summary>
    /// Closes the citation viewer.
    /// </summary>
    void CloseCitation();

    /// <summary>
    /// Navigates to a specific page in the current document (for multi-page documents).
    /// </summary>
    /// <param name="pageNumber">The page number to navigate to (1-based).</param>
    void NavigateToPage(int pageNumber);
}

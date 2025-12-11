using Aesir.Client.Web.Modules.Chat.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing citation viewer state across components.
/// </summary>
public class CitationStateService : ICitationStateService
{
    private readonly ICitationLinkParser _citationLinkParser;
    private CitationInfo? _currentCitation;

    public CitationStateService(ICitationLinkParser citationLinkParser)
    {
        _citationLinkParser = citationLinkParser;
    }

    /// <inheritdoc />
    public CitationInfo? CurrentCitation => _currentCitation;

    /// <inheritdoc />
    public bool IsViewerOpen => _currentCitation != null;

    /// <inheritdoc />
    public event Action? OnCitationChanged;

    /// <inheritdoc />
    public event Action<CitationInfo>? OnCitationOpened;

    /// <inheritdoc />
    public event Action? OnCitationClosed;

    /// <inheritdoc />
    public Task OpenCitationAsync(CitationInfo citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        _currentCitation = citation;

        OnCitationOpened?.Invoke(citation);
        OnCitationChanged?.Invoke();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> OpenCitationByUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Task.FromResult(false);
        }

        var citation = _citationLinkParser.ParseCitationLink(url);
        if (citation == null)
        {
            return Task.FromResult(false);
        }

        _currentCitation = citation;

        OnCitationOpened?.Invoke(citation);
        OnCitationChanged?.Invoke();

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public void CloseCitation()
    {
        if (_currentCitation == null)
        {
            return;
        }

        _currentCitation = null;

        OnCitationClosed?.Invoke();
        OnCitationChanged?.Invoke();
    }

    /// <inheritdoc />
    public void NavigateToPage(int pageNumber)
    {
        if (_currentCitation == null || pageNumber < 1)
        {
            return;
        }

        // Create a new citation info with the updated page number
        _currentCitation = _currentCitation with { PageNumber = pageNumber };

        OnCitationChanged?.Invoke();
    }
}

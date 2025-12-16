using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Implementation of IChatHistoryService that manages chat history with caching.
/// </summary>
public class ChatHistoryService : IChatHistoryService
{
    // TODO: Replace with claims-based user ID from authentication context
    private const string UserIdValue = "blangford@gmail.com";

    private readonly IChatApiService _chatApiService;
    private readonly IChatSessionNotifier _chatSessionNotifier;
    private List<AesirChatSessionItem> _sessions = [];
    private bool _isLoading;
    private string? _searchTerm;

    public ChatHistoryService(IChatApiService chatApiService, IChatSessionNotifier chatSessionNotifier)
    {
        _chatApiService = chatApiService;
        _chatSessionNotifier = chatSessionNotifier;

        // Subscribe to session created events from other modules (e.g., HandsFree)
        _chatSessionNotifier.OnSessionCreated += HandleSessionCreated;
    }

    private async void HandleSessionCreated(Guid sessionId)
    {
        // Refresh the sessions list when a new session is created from another module
        await LoadSessionsAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string UserId => UserIdValue;

    /// <inheritdoc />
    public IReadOnlyList<AesirChatSessionItem> Sessions => _sessions.AsReadOnly();

    /// <inheritdoc />
    public bool IsLoading => _isLoading;

    /// <inheritdoc />
    public string? SearchTerm => _searchTerm;

    /// <inheritdoc />
    public event Action? OnSessionsChanged;

    /// <inheritdoc />
    public event Action<Guid>? OnSessionSelected;

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> LoadSessionsAsync(CancellationToken ct = default)
    {
        _isLoading = true;
        _searchTerm = null;
        NotifyChanged();

        try
        {
            var result = await _chatApiService.GetChatSessionsAsync(UserId, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _sessions = result.Value?.OrderByDescending(s => s.UpdatedAt).ToList() ?? [];
            }

            return result;
        }
        finally
        {
            _isLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public async Task<ApiResult<AesirChatSession?>> GetSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        return await _chatApiService.GetChatSessionAsync(sessionId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ApiResult> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var result = await _chatApiService.DeleteChatSessionAsync(sessionId, ct).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Remove from local cache
            _sessions.RemoveAll(s => s.Id == sessionId);
            NotifyChanged();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ApiResult> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken ct = default)
    {
        var result = await _chatApiService.UpdateChatSessionTitleAsync(sessionId, title, ct).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Update in local cache
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.Title = title;
                NotifyChanged();
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ApiResult> UpdateSessionStarredAsync(Guid sessionId, bool isStarred, CancellationToken ct = default)
    {
        var result = await _chatApiService.UpdateSessionStarredAsync(sessionId, isStarred, ct).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            // Update in local cache
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.IsStarred = isStarred;
                NotifyChanged();
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> SearchSessionsAsync(string searchTerm, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await LoadSessionsAsync(ct).ConfigureAwait(false);
        }

        _isLoading = true;
        _searchTerm = searchTerm;
        NotifyChanged();

        try
        {
            var result = await _chatApiService.SearchChatSessionsAsync(UserId, searchTerm, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _sessions = result.Value?.OrderByDescending(s => s.UpdatedAt).ToList() ?? [];
            }

            return result;
        }
        finally
        {
            _isLoading = false;
            NotifyChanged();
        }
    }

    /// <inheritdoc />
    public async Task ClearSearchAsync(CancellationToken ct = default)
    {
        _searchTerm = null;
        await LoadSessionsAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void SelectSession(Guid sessionId)
    {
        OnSessionSelected?.Invoke(sessionId);
    }

    /// <inheritdoc />
    public async Task NotifySessionCreatedAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Refresh the sessions list to include the new session
        await LoadSessionsAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task NotifySessionUpdatedAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Refresh the sessions list to get the updated title
        await LoadSessionsAsync(ct).ConfigureAwait(false);
    }

    private void NotifyChanged()
    {
        OnSessionsChanged?.Invoke();
    }
}

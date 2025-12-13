using Aesir.Client.Web.Infrastructure.Http;
using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing chat history with caching and state notifications.
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Gets the current list of cached sessions.
    /// </summary>
    IReadOnlyList<AesirChatSessionItem> Sessions { get; }

    /// <summary>
    /// Gets whether sessions are currently being loaded.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the current search term.
    /// </summary>
    string? SearchTerm { get; }

    /// <summary>
    /// Event raised when the sessions list changes.
    /// </summary>
    event Action? OnSessionsChanged;

    /// <summary>
    /// Event raised when a session is selected.
    /// </summary>
    event Action<Guid>? OnSessionSelected;

    /// <summary>
    /// Loads or refreshes the sessions list for the current user.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> LoadSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific chat session by ID.
    /// </summary>
    Task<ApiResult<AesirChatSession?>> GetSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a chat session.
    /// </summary>
    Task<ApiResult> DeleteSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Updates the title of a chat session.
    /// </summary>
    Task<ApiResult> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken ct = default);

    /// <summary>
    /// Updates the starred status of a chat session.
    /// </summary>
    Task<ApiResult> UpdateSessionStarredAsync(Guid sessionId, bool isStarred, CancellationToken ct = default);

    /// <summary>
    /// Searches sessions by term.
    /// </summary>
    Task<ApiResult<IReadOnlyList<AesirChatSessionItem>>> SearchSessionsAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Clears the search filter and reloads all sessions.
    /// </summary>
    Task ClearSearchAsync(CancellationToken ct = default);

    /// <summary>
    /// Selects a session (triggers OnSessionSelected event).
    /// </summary>
    void SelectSession(Guid sessionId);

    /// <summary>
    /// Notifies that a new session was created (refreshes the list).
    /// </summary>
    Task NotifySessionCreatedAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Notifies that a session was updated (refreshes the list to get new title).
    /// </summary>
    Task NotifySessionUpdatedAsync(Guid sessionId, CancellationToken ct = default);
}

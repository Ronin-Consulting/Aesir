using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for managing chat state including selected agent and current conversation.
/// </summary>
public interface IChatStateService
{
    /// <summary>
    /// Gets the currently selected agent.
    /// </summary>
    AesirAgentBase? SelectedAgent { get; }

    /// <summary>
    /// Gets the ID of the currently selected agent.
    /// </summary>
    Guid? SelectedAgentId { get; }

    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    Guid? CurrentSessionId { get; }

    /// <summary>
    /// Gets the current session title.
    /// </summary>
    string? CurrentSessionTitle { get; }

    /// <summary>
    /// Gets whether a chat is currently active.
    /// </summary>
    bool HasActiveChat { get; }

    /// <summary>
    /// Event raised when the selected agent changes.
    /// </summary>
    event Action? OnAgentChanged;

    /// <summary>
    /// Event raised when the conversation changes.
    /// </summary>
    event Action? OnConversationChanged;

    /// <summary>
    /// Event raised when a new chat is started.
    /// </summary>
    event Action? OnNewChat;

    /// <summary>
    /// Event raised when the current session changes.
    /// </summary>
    event Action? OnSessionChanged;

    /// <summary>
    /// Event raised when a document is deleted from any location.
    /// </summary>
    event Action<Guid>? OnDocumentDeleted;

    /// <summary>
    /// Selects an agent for the chat.
    /// </summary>
    void SelectAgent(AesirAgentBase? agent);

    /// <summary>
    /// Refreshes the currently selected agent by fetching updated data from the server.
    /// This should be called when configuration changes (e.g., inference engine settings).
    /// </summary>
    /// <param name="agents">The updated list of agents from the server.</param>
    void RefreshSelectedAgent(IEnumerable<AesirAgentBase> agents);

    /// <summary>
    /// Sets the current session ID and optionally its title.
    /// </summary>
    void SetCurrentSession(Guid? sessionId, string? title = null);

    /// <summary>
    /// Updates the current session title.
    /// </summary>
    void SetCurrentSessionTitle(string? title);

    /// <summary>
    /// Starts a new chat conversation.
    /// </summary>
    void StartNewChat();

    /// <summary>
    /// Notifies that the conversation has changed.
    /// </summary>
    void NotifyConversationChanged();

    /// <summary>
    /// Notifies that a document has been deleted.
    /// </summary>
    /// <param name="documentId">The ID of the deleted document.</param>
    void NotifyDocumentDeleted(Guid documentId);

    #region Tool Toggle State

    /// <summary>
    /// Gets the set of tool IDs that are currently disabled for the current conversation.
    /// </summary>
    IReadOnlySet<Guid> DisabledToolIds { get; }

    /// <summary>
    /// Event raised when tool toggle state changes.
    /// </summary>
    event Action? OnToolTogglesChanged;

    /// <summary>
    /// Sets whether a tool is enabled or disabled.
    /// </summary>
    /// <param name="toolId">The tool ID.</param>
    /// <param name="enabled">True to enable, false to disable.</param>
    /// <returns>A task representing the async operation (includes localStorage persistence).</returns>
    Task SetToolEnabledAsync(Guid toolId, bool enabled);

    /// <summary>
    /// Checks if a tool is currently enabled.
    /// </summary>
    /// <param name="toolId">The tool ID.</param>
    /// <returns>True if enabled (not in disabled set), false otherwise.</returns>
    bool IsToolEnabled(Guid toolId);

    /// <summary>
    /// Clears all tool toggle state for the current conversation.
    /// </summary>
    /// <returns>A task representing the async operation (includes localStorage cleanup).</returns>
    Task ClearToolTogglesAsync();

    /// <summary>
    /// Gets the disabled tool IDs for a specific conversation.
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    /// <returns>Set of disabled tool IDs, or empty set if none.</returns>
    IReadOnlySet<Guid> GetDisabledToolIdsForSession(Guid sessionId);

    /// <summary>
    /// Restores tool toggle state for a specific conversation (in-memory only).
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    void RestoreToolTogglesForSession(Guid sessionId);

    /// <summary>
    /// Restores tool toggle state for a specific conversation from memory or localStorage.
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RestoreToolTogglesForSessionAsync(Guid sessionId);

    #endregion

    #region Think Level State

    /// <summary>
    /// Gets the currently selected think level for the current conversation.
    /// </summary>
    ThinkValue? SelectedThinkLevel { get; }

    /// <summary>
    /// Event raised when the think level changes.
    /// </summary>
    event Action? OnThinkLevelChanged;

    /// <summary>
    /// Sets the think level for the current conversation.
    /// </summary>
    /// <param name="level">The think level to set (null, false, "low", "medium", "high").</param>
    /// <returns>A task representing the async operation (includes localStorage persistence).</returns>
    Task SetThinkLevelAsync(ThinkValue? level);

    /// <summary>
    /// Clears the think level for the current conversation (resets to default).
    /// </summary>
    /// <returns>A task representing the async operation (includes localStorage cleanup).</returns>
    Task ClearThinkLevelAsync();

    /// <summary>
    /// Gets the think level for a specific conversation from memory or localStorage.
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    /// <returns>The stored think level, or null if not set.</returns>
    Task<ThinkValue?> GetThinkLevelForSessionAsync(Guid sessionId);

    /// <summary>
    /// Restores think level state for a specific conversation from memory or localStorage.
    /// </summary>
    /// <param name="sessionId">The conversation session ID.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RestoreThinkLevelForSessionAsync(Guid sessionId);

    #endregion
}

using Aesir.Common.Models;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Shared service for storing chat preferences (tool toggles, think level) per session.
/// Used by both Chat and HandsFree modules to ensure consistent settings across modes.
/// Backed by localStorage for persistence.
/// </summary>
public interface IChatPreferencesService
{
    #region Tool Toggles

    /// <summary>
    /// Gets the set of disabled tool IDs for a session from localStorage.
    /// </summary>
    /// <param name="sessionId">The session ID, or null for a new session.</param>
    /// <returns>Set of disabled tool IDs, or empty set if none.</returns>
    Task<IReadOnlySet<Guid>> GetDisabledToolIdsAsync(Guid? sessionId);

    /// <summary>
    /// Gets the cached disabled tool IDs for a session (memory only, no localStorage access).
    /// </summary>
    /// <param name="sessionId">The session ID, or null for a new session.</param>
    /// <returns>Set of disabled tool IDs, or empty set if none cached.</returns>
    IReadOnlySet<Guid> GetDisabledToolIdsCached(Guid? sessionId);

    /// <summary>
    /// Sets whether a tool is enabled or disabled for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="toolId">The tool ID.</param>
    /// <param name="enabled">True to enable, false to disable.</param>
    Task SetToolEnabledAsync(Guid? sessionId, Guid toolId, bool enabled);

    /// <summary>
    /// Clears all tool toggle state for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    Task ClearToolTogglesAsync(Guid? sessionId);

    /// <summary>
    /// Restores tool toggle state from localStorage to memory cache.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    Task RestoreToolTogglesFromStorageAsync(Guid? sessionId);

    /// <summary>
    /// Checks if a tool is enabled for the given session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="toolId">The tool ID.</param>
    /// <returns>True if enabled (not in disabled set).</returns>
    bool IsToolEnabled(Guid? sessionId, Guid toolId);

    /// <summary>
    /// Event raised when tool toggle state changes for any session.
    /// </summary>
    event Action? OnToolTogglesChanged;

    #endregion

    #region Think Level

    /// <summary>
    /// Gets the think level for a session.
    /// </summary>
    /// <param name="sessionId">The session ID, or null for a new session.</param>
    /// <returns>The think level, or null if not set.</returns>
    Task<ThinkValue?> GetThinkLevelAsync(Guid? sessionId);

    /// <summary>
    /// Gets the cached think level for a session (memory only, no localStorage access).
    /// </summary>
    /// <param name="sessionId">The session ID, or null for a new session.</param>
    /// <returns>The think level, or null if not cached.</returns>
    ThinkValue? GetThinkLevelCached(Guid? sessionId);

    /// <summary>
    /// Sets the think level for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="level">The think level, or null to clear.</param>
    Task SetThinkLevelAsync(Guid? sessionId, ThinkValue? level);

    /// <summary>
    /// Clears the think level for a session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    Task ClearThinkLevelAsync(Guid? sessionId);

    /// <summary>
    /// Restores think level from localStorage to memory cache.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    Task RestoreThinkLevelFromStorageAsync(Guid? sessionId);

    /// <summary>
    /// Event raised when think level changes for any session.
    /// </summary>
    event Action? OnThinkLevelChanged;

    #endregion
}

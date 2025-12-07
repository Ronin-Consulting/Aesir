using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

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
    /// Selects an agent for the chat.
    /// </summary>
    void SelectAgent(AesirAgentBase? agent);

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
}

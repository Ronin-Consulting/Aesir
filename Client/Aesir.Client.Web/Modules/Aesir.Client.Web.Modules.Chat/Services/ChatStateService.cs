using Aesir.Common.Models;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing chat state including selected agent and current conversation.
/// </summary>
public class ChatStateService : IChatStateService
{
    private const string SelectedAgentIdKey = "aesir_selected_agent_id";

    /// <inheritdoc />
    public AesirAgentBase? SelectedAgent { get; private set; }

    /// <inheritdoc />
    public Guid? SelectedAgentId => SelectedAgent?.Id;

    /// <inheritdoc />
    public Guid? CurrentSessionId { get; private set; }

    /// <inheritdoc />
    public string? CurrentSessionTitle { get; private set; }

    /// <inheritdoc />
    public bool HasActiveChat => CurrentSessionId.HasValue;

    /// <inheritdoc />
    public event Action? OnAgentChanged;

    /// <inheritdoc />
    public event Action? OnConversationChanged;

    /// <inheritdoc />
    public event Action? OnNewChat;

    /// <inheritdoc />
    public event Action? OnSessionChanged;

    /// <inheritdoc />
    public void SelectAgent(AesirAgentBase? agent)
    {
        if (SelectedAgent?.Id != agent?.Id)
        {
            SelectedAgent = agent;
            OnAgentChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public void SetCurrentSession(Guid? sessionId, string? title = null)
    {
        var sessionChanged = CurrentSessionId != sessionId;
        var titleChanged = CurrentSessionTitle != title;

        if (sessionChanged || titleChanged)
        {
            CurrentSessionId = sessionId;
            CurrentSessionTitle = title;
            OnConversationChanged?.Invoke();

            if (sessionChanged)
            {
                OnSessionChanged?.Invoke();
            }
        }
    }

    /// <inheritdoc />
    public void SetCurrentSessionTitle(string? title)
    {
        if (CurrentSessionTitle != title)
        {
            CurrentSessionTitle = title;
            OnConversationChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public void StartNewChat()
    {
        CurrentSessionId = null;
        CurrentSessionTitle = null;
        OnNewChat?.Invoke();
    }

    /// <inheritdoc />
    public void NotifyConversationChanged()
    {
        OnConversationChanged?.Invoke();
    }
}

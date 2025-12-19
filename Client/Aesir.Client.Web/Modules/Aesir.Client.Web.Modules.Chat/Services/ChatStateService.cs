using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing chat state including selected agent and current conversation.
/// Tool toggles and think level are delegated to IChatPreferencesService for shared access.
/// </summary>
public class ChatStateService : IChatStateService
{
    private const string SelectedAgentIdKey = "aesir_selected_agent_id";

    private readonly IJSRuntime _jsRuntime;
    private readonly IChatPreferencesService _preferencesService;
    private readonly ILogger<ChatStateService>? _logger;

    public ChatStateService(
        IJSRuntime jsRuntime,
        IChatPreferencesService preferencesService,
        ILogger<ChatStateService>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _preferencesService = preferencesService;
        _logger = logger;

        // Forward events from preferences service
        _preferencesService.OnToolTogglesChanged += () => OnToolTogglesChanged?.Invoke();
        _preferencesService.OnThinkLevelChanged += () => OnThinkLevelChanged?.Invoke();
    }

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
    public event Action<Guid>? OnDocumentDeleted;

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
    public void RefreshSelectedAgent(IEnumerable<AesirAgentBase> agents)
    {
        if (SelectedAgent == null)
            return;

        // Find the updated agent with the same ID
        var updatedAgent = agents.FirstOrDefault(a => a.Id == SelectedAgent.Id);
        if (updatedAgent != null)
        {
            SelectedAgent = updatedAgent;
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

    /// <inheritdoc />
    public void NotifyDocumentDeleted(Guid documentId)
    {
        OnDocumentDeleted?.Invoke(documentId);
    }

    #region Tool Toggle State (Delegated to IChatPreferencesService)

    /// <inheritdoc />
    public IReadOnlySet<Guid> DisabledToolIds => _preferencesService.GetDisabledToolIdsCached(CurrentSessionId);

    /// <inheritdoc />
    public event Action? OnToolTogglesChanged;

    /// <inheritdoc />
    public Task SetToolEnabledAsync(Guid toolId, bool enabled)
        => _preferencesService.SetToolEnabledAsync(CurrentSessionId, toolId, enabled);

    /// <inheritdoc />
    public bool IsToolEnabled(Guid toolId)
        => _preferencesService.IsToolEnabled(CurrentSessionId, toolId);

    /// <inheritdoc />
    public Task ClearToolTogglesAsync()
        => _preferencesService.ClearToolTogglesAsync(CurrentSessionId);

    /// <inheritdoc />
    public IReadOnlySet<Guid> GetDisabledToolIdsForSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _logger?.LogWarning("GetDisabledToolIdsForSession called with empty Guid");
            return new HashSet<Guid>();
        }

        return _preferencesService.GetDisabledToolIdsCached(sessionId);
    }

    /// <inheritdoc />
    public void RestoreToolTogglesForSession(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _logger?.LogWarning("RestoreToolTogglesForSession called with empty Guid");
            OnToolTogglesChanged?.Invoke();
            return;
        }

        // For synchronous restore, we can only use cached values
        // The caller should use RestoreToolTogglesForSessionAsync for full restore
        OnToolTogglesChanged?.Invoke();
    }

    /// <inheritdoc />
    public async Task RestoreToolTogglesForSessionAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _logger?.LogWarning("RestoreToolTogglesForSessionAsync called with empty Guid");
            await _preferencesService.RestoreToolTogglesFromStorageAsync(null);
            return;
        }

        await _preferencesService.RestoreToolTogglesFromStorageAsync(sessionId);
    }

    #endregion

    #region Think Level State (Delegated to IChatPreferencesService)

    /// <inheritdoc />
    public ThinkValue? SelectedThinkLevel => _preferencesService.GetThinkLevelCached(CurrentSessionId);

    /// <inheritdoc />
    public event Action? OnThinkLevelChanged;

    /// <inheritdoc />
    public Task SetThinkLevelAsync(ThinkValue? level)
        => _preferencesService.SetThinkLevelAsync(CurrentSessionId, level);

    /// <inheritdoc />
    public Task ClearThinkLevelAsync()
        => _preferencesService.ClearThinkLevelAsync(CurrentSessionId);

    /// <inheritdoc />
    public async Task<ThinkValue?> GetThinkLevelForSessionAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _logger?.LogWarning("GetThinkLevelForSessionAsync called with empty Guid");
            return null;
        }

        return await _preferencesService.GetThinkLevelAsync(sessionId);
    }

    /// <inheritdoc />
    public async Task RestoreThinkLevelForSessionAsync(Guid sessionId)
    {
        if (sessionId == Guid.Empty)
        {
            _logger?.LogWarning("RestoreThinkLevelForSessionAsync called with empty Guid");
            await _preferencesService.RestoreThinkLevelFromStorageAsync(null);
            return;
        }

        await _preferencesService.RestoreThinkLevelFromStorageAsync(sessionId);
    }

    #endregion
}

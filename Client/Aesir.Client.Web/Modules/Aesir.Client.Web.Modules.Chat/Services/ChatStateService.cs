using Aesir.Common.Models;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Modules.Chat.Services;

/// <summary>
/// Service for managing chat state including selected agent and current conversation.
/// </summary>
public class ChatStateService : IChatStateService
{
    private const string SelectedAgentIdKey = "aesir_selected_agent_id";
    private const string ThinkLevelKeyPrefix = "aesir_think_level_";

    private readonly IJSRuntime _jsRuntime;

    public ChatStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    // Tool toggle state: maps session ID to set of disabled tool IDs
    private readonly Dictionary<Guid, HashSet<Guid>> _sessionToolToggles = new();
    private HashSet<Guid> _currentDisabledToolIds = new();
    private static readonly IReadOnlySet<Guid> EmptyGuidSet = new HashSet<Guid>();

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

    #region Tool Toggle State

    /// <inheritdoc />
    public IReadOnlySet<Guid> DisabledToolIds => _currentDisabledToolIds;

    /// <inheritdoc />
    public event Action? OnToolTogglesChanged;

    /// <inheritdoc />
    public void SetToolEnabled(Guid toolId, bool enabled)
    {
        bool changed;
        if (enabled)
        {
            changed = _currentDisabledToolIds.Remove(toolId);
        }
        else
        {
            changed = _currentDisabledToolIds.Add(toolId);
        }

        if (changed)
        {
            // Persist to session storage if we have an active session
            if (CurrentSessionId.HasValue)
            {
                _sessionToolToggles[CurrentSessionId.Value] = new HashSet<Guid>(_currentDisabledToolIds);
            }

            OnToolTogglesChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public bool IsToolEnabled(Guid toolId)
    {
        return !_currentDisabledToolIds.Contains(toolId);
    }

    /// <inheritdoc />
    public void ClearToolToggles()
    {
        if (_currentDisabledToolIds.Count > 0)
        {
            _currentDisabledToolIds.Clear();

            // Clear session storage too if we have an active session
            if (CurrentSessionId.HasValue)
            {
                _sessionToolToggles.Remove(CurrentSessionId.Value);
            }

            OnToolTogglesChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public IReadOnlySet<Guid> GetDisabledToolIdsForSession(Guid sessionId)
    {
        return _sessionToolToggles.TryGetValue(sessionId, out var disabledIds)
            ? disabledIds
            : EmptyGuidSet;
    }

    /// <inheritdoc />
    public void RestoreToolTogglesForSession(Guid sessionId)
    {
        if (_sessionToolToggles.TryGetValue(sessionId, out var disabledIds))
        {
            _currentDisabledToolIds = new HashSet<Guid>(disabledIds);
        }
        else
        {
            _currentDisabledToolIds = new HashSet<Guid>();
        }

        OnToolTogglesChanged?.Invoke();
    }

    #endregion

    #region Think Level State

    // Think level state: maps session ID to think level (in-memory cache)
    private readonly Dictionary<Guid, ThinkValue> _sessionThinkLevels = new();
    private ThinkValue? _currentThinkLevel;

    /// <inheritdoc />
    public ThinkValue? SelectedThinkLevel => _currentThinkLevel;

    /// <inheritdoc />
    public event Action? OnThinkLevelChanged;

    /// <inheritdoc />
    public async Task SetThinkLevelAsync(ThinkValue? level)
    {
        if (_currentThinkLevel?.ToString() != level?.ToString())
        {
            _currentThinkLevel = level;

            // Persist to in-memory cache and localStorage if we have an active session
            if (CurrentSessionId.HasValue && level.HasValue)
            {
                _sessionThinkLevels[CurrentSessionId.Value] = level.Value;
                await SaveThinkLevelToStorageAsync(CurrentSessionId.Value, level.Value);
            }
            else if (CurrentSessionId.HasValue)
            {
                _sessionThinkLevels.Remove(CurrentSessionId.Value);
                await RemoveThinkLevelFromStorageAsync(CurrentSessionId.Value);
            }

            OnThinkLevelChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task ClearThinkLevelAsync()
    {
        if (_currentThinkLevel.HasValue)
        {
            _currentThinkLevel = null;

            // Clear in-memory cache and localStorage if we have an active session
            if (CurrentSessionId.HasValue)
            {
                _sessionThinkLevels.Remove(CurrentSessionId.Value);
                await RemoveThinkLevelFromStorageAsync(CurrentSessionId.Value);
            }

            OnThinkLevelChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task<ThinkValue?> GetThinkLevelForSessionAsync(Guid sessionId)
    {
        // First check in-memory cache
        if (_sessionThinkLevels.TryGetValue(sessionId, out var level))
        {
            return level;
        }

        // Try to load from localStorage
        var storedLevel = await LoadThinkLevelFromStorageAsync(sessionId);
        if (storedLevel.HasValue)
        {
            // Cache it in memory
            _sessionThinkLevels[sessionId] = storedLevel.Value;
        }

        return storedLevel;
    }

    /// <inheritdoc />
    public async Task RestoreThinkLevelForSessionAsync(Guid sessionId)
    {
        var level = await GetThinkLevelForSessionAsync(sessionId);
        _currentThinkLevel = level;
        OnThinkLevelChanged?.Invoke();
    }

    /// <summary>
    /// Saves the think level to localStorage for a specific session.
    /// </summary>
    private async Task SaveThinkLevelToStorageAsync(Guid sessionId, ThinkValue level)
    {
        try
        {
            var key = $"{ThinkLevelKeyPrefix}{sessionId}";
            var value = level.ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch
        {
            // localStorage not available, continue without persistence
        }
    }

    /// <summary>
    /// Removes the think level from localStorage for a specific session.
    /// </summary>
    private async Task RemoveThinkLevelFromStorageAsync(Guid sessionId)
    {
        try
        {
            var key = $"{ThinkLevelKeyPrefix}{sessionId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch
        {
            // localStorage not available, continue without persistence
        }
    }

    /// <summary>
    /// Loads the think level from localStorage for a specific session.
    /// </summary>
    private async Task<ThinkValue?> LoadThinkLevelFromStorageAsync(Guid sessionId)
    {
        try
        {
            var key = $"{ThinkLevelKeyPrefix}{sessionId}";
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            // Parse the stored value back to ThinkValue
            var lowerValue = value.ToLowerInvariant();
            if (lowerValue == "false") return new ThinkValue(false);
            if (lowerValue == "true") return new ThinkValue(true);
            if (lowerValue == ThinkValue.Low) return new ThinkValue(ThinkValue.Low);
            if (lowerValue == ThinkValue.Medium) return new ThinkValue(ThinkValue.Medium);
            if (lowerValue == ThinkValue.High) return new ThinkValue(ThinkValue.High);
            return null;
        }
        catch
        {
            // localStorage not available, return null
            return null;
        }
    }

    #endregion
}

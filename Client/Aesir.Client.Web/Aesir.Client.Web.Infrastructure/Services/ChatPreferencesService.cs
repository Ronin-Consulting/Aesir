using System.Text.Json;
using Aesir.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// localStorage-backed service for storing chat preferences (tool toggles, think level) per session.
/// Used by both Chat and HandsFree modules.
/// </summary>
public class ChatPreferencesService : IChatPreferencesService
{
    private const string ThinkLevelKeyPrefix = "aesir_think_level_";
    private const string DisabledToolsKeyPrefix = "aesir_disabled_tools_";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ChatPreferencesService>? _logger;

    // In-memory cache: session ID -> disabled tool IDs
    private readonly Dictionary<Guid, HashSet<Guid>> _sessionToolToggles = new();

    // In-memory cache: session ID -> think level
    private readonly Dictionary<Guid, ThinkValue> _sessionThinkLevels = new();

    // Cache for sessions without an ID yet (new conversations)
    private HashSet<Guid> _noSessionDisabledTools = new();
    private ThinkValue? _noSessionThinkLevel;

    private static readonly IReadOnlySet<Guid> EmptyGuidSet = new HashSet<Guid>();

    /// <summary>
    /// Creates a new ChatPreferencesService.
    /// </summary>
    public ChatPreferencesService(IJSRuntime jsRuntime, ILogger<ChatPreferencesService>? logger = null)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    #region Tool Toggles

    /// <inheritdoc />
    public event Action? OnToolTogglesChanged;

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> GetDisabledToolIdsAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            return _noSessionDisabledTools;
        }

        // Check memory cache first
        if (_sessionToolToggles.TryGetValue(sessionId.Value, out var cached))
        {
            return cached;
        }

        // Load from localStorage
        var stored = await LoadDisabledToolsFromStorageAsync(sessionId.Value);
        if (stored != null && stored.Count > 0)
        {
            _sessionToolToggles[sessionId.Value] = stored;
            return stored;
        }

        return EmptyGuidSet;
    }

    /// <inheritdoc />
    public IReadOnlySet<Guid> GetDisabledToolIdsCached(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            return _noSessionDisabledTools;
        }

        return _sessionToolToggles.TryGetValue(sessionId.Value, out var cached)
            ? cached
            : EmptyGuidSet;
    }

    /// <inheritdoc />
    public async Task SetToolEnabledAsync(Guid? sessionId, Guid toolId, bool enabled)
    {
        HashSet<Guid> disabledTools;

        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            disabledTools = _noSessionDisabledTools;
        }
        else
        {
            if (!_sessionToolToggles.TryGetValue(sessionId.Value, out disabledTools!))
            {
                disabledTools = new HashSet<Guid>();
                _sessionToolToggles[sessionId.Value] = disabledTools;
            }
        }

        bool changed;
        if (enabled)
        {
            changed = disabledTools.Remove(toolId);
        }
        else
        {
            changed = disabledTools.Add(toolId);
        }

        if (changed)
        {
            // Persist to localStorage if we have a session ID
            if (sessionId.HasValue && sessionId.Value != Guid.Empty)
            {
                await SaveDisabledToolsToStorageAsync(sessionId.Value, disabledTools);
            }

            OnToolTogglesChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task ClearToolTogglesAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            if (_noSessionDisabledTools.Count > 0)
            {
                _noSessionDisabledTools.Clear();
                OnToolTogglesChanged?.Invoke();
            }
            return;
        }

        if (_sessionToolToggles.TryGetValue(sessionId.Value, out var disabledTools) && disabledTools.Count > 0)
        {
            _sessionToolToggles.Remove(sessionId.Value);
            await RemoveDisabledToolsFromStorageAsync(sessionId.Value);
            OnToolTogglesChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task RestoreToolTogglesFromStorageAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            _noSessionDisabledTools.Clear();
            OnToolTogglesChanged?.Invoke();
            return;
        }

        var stored = await LoadDisabledToolsFromStorageAsync(sessionId.Value);
        if (stored != null && stored.Count > 0)
        {
            _sessionToolToggles[sessionId.Value] = stored;
        }
        else
        {
            _sessionToolToggles.Remove(sessionId.Value);
        }

        OnToolTogglesChanged?.Invoke();
    }

    /// <inheritdoc />
    public bool IsToolEnabled(Guid? sessionId, Guid toolId)
    {
        var disabled = GetDisabledToolIdsCached(sessionId);
        return !disabled.Contains(toolId);
    }

    private async Task SaveDisabledToolsToStorageAsync(Guid sessionId, HashSet<Guid> disabledToolIds)
    {
        try
        {
            var key = $"{DisabledToolsKeyPrefix}{sessionId}";
            if (disabledToolIds.Count == 0)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            else
            {
                var json = JsonSerializer.Serialize(disabledToolIds);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save disabled tools to localStorage for session {SessionId}", sessionId);
        }
    }

    private async Task<HashSet<Guid>?> LoadDisabledToolsFromStorageAsync(Guid sessionId)
    {
        try
        {
            var key = $"{DisabledToolsKeyPrefix}{sessionId}";
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<HashSet<Guid>>(json);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load disabled tools from localStorage for session {SessionId}", sessionId);
            return null;
        }
    }

    private async Task RemoveDisabledToolsFromStorageAsync(Guid sessionId)
    {
        try
        {
            var key = $"{DisabledToolsKeyPrefix}{sessionId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove disabled tools from localStorage for session {SessionId}", sessionId);
        }
    }

    #endregion

    #region Think Level

    /// <inheritdoc />
    public event Action? OnThinkLevelChanged;

    /// <inheritdoc />
    public async Task<ThinkValue?> GetThinkLevelAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            return _noSessionThinkLevel;
        }

        // Check memory cache first
        if (_sessionThinkLevels.TryGetValue(sessionId.Value, out var cached))
        {
            return cached;
        }

        // Load from localStorage
        var stored = await LoadThinkLevelFromStorageAsync(sessionId.Value);
        if (stored.HasValue)
        {
            _sessionThinkLevels[sessionId.Value] = stored.Value;
            return stored;
        }

        return null;
    }

    /// <inheritdoc />
    public ThinkValue? GetThinkLevelCached(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            return _noSessionThinkLevel;
        }

        return _sessionThinkLevels.TryGetValue(sessionId.Value, out var cached)
            ? (ThinkValue?)cached
            : null;
    }

    /// <inheritdoc />
    public async Task SetThinkLevelAsync(Guid? sessionId, ThinkValue? level)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            if (_noSessionThinkLevel?.ToString() != level?.ToString())
            {
                _noSessionThinkLevel = level;
                OnThinkLevelChanged?.Invoke();
            }
            return;
        }

        var currentLevel = _sessionThinkLevels.TryGetValue(sessionId.Value, out var existing)
            ? (ThinkValue?)existing
            : null;

        if (currentLevel?.ToString() != level?.ToString())
        {
            if (level.HasValue)
            {
                _sessionThinkLevels[sessionId.Value] = level.Value;
                await SaveThinkLevelToStorageAsync(sessionId.Value, level.Value);
            }
            else
            {
                _sessionThinkLevels.Remove(sessionId.Value);
                await RemoveThinkLevelFromStorageAsync(sessionId.Value);
            }

            OnThinkLevelChanged?.Invoke();
        }
    }

    /// <inheritdoc />
    public async Task ClearThinkLevelAsync(Guid? sessionId)
    {
        await SetThinkLevelAsync(sessionId, null);
    }

    /// <inheritdoc />
    public async Task RestoreThinkLevelFromStorageAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            _noSessionThinkLevel = null;
            OnThinkLevelChanged?.Invoke();
            return;
        }

        var stored = await LoadThinkLevelFromStorageAsync(sessionId.Value);
        if (stored.HasValue)
        {
            _sessionThinkLevels[sessionId.Value] = stored.Value;
        }
        else
        {
            _sessionThinkLevels.Remove(sessionId.Value);
        }

        OnThinkLevelChanged?.Invoke();
    }

    private async Task SaveThinkLevelToStorageAsync(Guid sessionId, ThinkValue level)
    {
        try
        {
            var key = $"{ThinkLevelKeyPrefix}{sessionId}";
            var value = level.ToString();
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save think level to localStorage for session {SessionId}", sessionId);
        }
    }

    private async Task RemoveThinkLevelFromStorageAsync(Guid sessionId)
    {
        try
        {
            var key = $"{ThinkLevelKeyPrefix}{sessionId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove think level from localStorage for session {SessionId}", sessionId);
        }
    }

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
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load think level from localStorage for session {SessionId}", sessionId);
            return null;
        }
    }

    #endregion
}

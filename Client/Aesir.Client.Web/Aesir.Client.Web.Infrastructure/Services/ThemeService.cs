using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for managing application theme with localStorage persistence.
/// </summary>
public class ThemeService : IThemeService
{
    private const string StorageKey = "aesir-theme-dark-mode";
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = true; // Default to dark mode (matches landing page)
    private bool _isInitialized;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode => _isDarkMode;

    public event EventHandler<bool>? ThemeChanged;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(stored) && bool.TryParse(stored, out var isDark))
            {
                _isDarkMode = isDark;
            }
        }
        catch
        {
            // localStorage not available (SSR or restricted), use default
        }

        _isInitialized = true;
    }

    public async Task ToggleThemeAsync()
    {
        await SetThemeAsync(!_isDarkMode);
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        if (_isDarkMode == isDarkMode && _isInitialized) return;

        _isDarkMode = isDarkMode;

        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, isDarkMode.ToString().ToLower());
        }
        catch
        {
            // localStorage not available, continue without persistence
        }

        ThemeChanged?.Invoke(this, isDarkMode);
    }
}

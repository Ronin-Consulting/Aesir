namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for managing application theme (light/dark mode).
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets whether dark mode is currently enabled.
    /// </summary>
    bool IsDarkMode { get; }

    /// <summary>
    /// Event raised when the theme changes.
    /// </summary>
    event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// Toggles between light and dark mode.
    /// </summary>
    Task ToggleThemeAsync();

    /// <summary>
    /// Sets the theme explicitly.
    /// </summary>
    /// <param name="isDarkMode">True for dark mode, false for light mode.</param>
    Task SetThemeAsync(bool isDarkMode);

    /// <summary>
    /// Initializes the theme service and loads saved preference.
    /// </summary>
    Task InitializeAsync();
}

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for detecting the runtime platform environment.
/// Distinguishes between Tauri desktop, standard browser, and mobile platforms.
/// </summary>
public interface IPlatformDetectionService
{
    /// <summary>
    /// Gets whether the application is running within a Tauri desktop wrapper.
    /// </summary>
    bool IsTauri { get; }

    /// <summary>
    /// Gets whether the application is running on a desktop platform (Tauri or desktop browser).
    /// </summary>
    bool IsDesktop { get; }

    /// <summary>
    /// Gets whether the application is running in a standard web browser.
    /// </summary>
    bool IsBrowser { get; }

    /// <summary>
    /// Gets whether the application is running on a mobile device.
    /// </summary>
    bool IsMobile { get; }

    /// <summary>
    /// Gets the detected operating system name ("windows", "macos", "linux", "ios", "android", "unknown").
    /// </summary>
    string OperatingSystem { get; }

    /// <summary>
    /// Initializes the platform detection by performing JavaScript interop checks.
    /// Should be called once during application startup.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Gets whether native file operations are available (requires Tauri).
    /// </summary>
    bool SupportsNativeFileOperations { get; }

    /// <summary>
    /// Gets whether Quick Look is available (macOS only in Tauri).
    /// </summary>
    bool SupportsQuickLook { get; }
}

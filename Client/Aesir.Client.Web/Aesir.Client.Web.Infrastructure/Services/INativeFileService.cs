namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Service for native file operations, primarily used in Tauri desktop environment.
/// Provides methods for opening files with system applications and saving files via native dialogs.
/// </summary>
public interface INativeFileService
{
    /// <summary>
    /// Opens a file with the system's default application.
    /// In Tauri, uses the shell plugin. In browser, opens in new tab.
    /// </summary>
    /// <param name="filePath">Path or URL to the file</param>
    /// <returns>True if the file was opened successfully</returns>
    Task<bool> OpenFileNativeAsync(string filePath);

    /// <summary>
    /// Opens a URL in the system's default browser.
    /// </summary>
    /// <param name="url">URL to open</param>
    /// <returns>True if the URL was opened successfully</returns>
    Task<bool> OpenUrlNativeAsync(string url);

    /// <summary>
    /// Saves content to a file using native save dialog (Tauri) or browser download.
    /// </summary>
    /// <param name="filename">Suggested filename</param>
    /// <param name="content">File content as byte array</param>
    /// <returns>The saved file path (Tauri) or filename (browser), null if cancelled</returns>
    Task<string?> SaveToDownloadsAsync(string filename, byte[] content);

    /// <summary>
    /// Triggers macOS Quick Look for a file preview.
    /// Only works on macOS in Tauri environment.
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>True if Quick Look was triggered successfully</returns>
    Task<bool> ShowQuickLookAsync(string filePath);

    /// <summary>
    /// Downloads a file from URL and opens it with native application.
    /// In browser, simply opens the URL in a new tab.
    /// </summary>
    /// <param name="url">URL of the file to download</param>
    /// <param name="filename">Filename to use for the downloaded file</param>
    /// <returns>True if successful</returns>
    Task<bool> DownloadAndOpenNativeAsync(string url, string filename);

    /// <summary>
    /// Downloads a file from URL and saves it using native save dialog.
    /// In Tauri, shows a native save file dialog. In browser, triggers download.
    /// </summary>
    /// <param name="url">URL of the file to download</param>
    /// <param name="filename">Suggested filename for the save dialog</param>
    /// <returns>The saved file path, or null if cancelled</returns>
    Task<string?> SaveFileWithDialogAsync(string url, string filename);

    /// <summary>
    /// Gets whether native file operations are available.
    /// </summary>
    bool IsNativeAvailable { get; }

    /// <summary>
    /// Gets whether Quick Look is available (macOS in Tauri only).
    /// </summary>
    bool IsQuickLookAvailable { get; }
}

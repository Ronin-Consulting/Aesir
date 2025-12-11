using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of native file operations using JavaScript interop.
/// Provides Tauri-specific functionality when available, with browser fallbacks.
/// </summary>
public class NativeFileService : INativeFileService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IPlatformDetectionService _platformDetection;

    public NativeFileService(IJSRuntime jsRuntime, IPlatformDetectionService platformDetection)
    {
        _jsRuntime = jsRuntime;
        _platformDetection = platformDetection;
    }

    /// <inheritdoc />
    public bool IsNativeAvailable => _platformDetection.IsTauri;

    /// <inheritdoc />
    public bool IsQuickLookAvailable => _platformDetection.SupportsQuickLook;

    /// <inheritdoc />
    public async Task<bool> OpenFileNativeAsync(string filePath)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("aesirNativeFile.openFileNative", filePath);
        }
        catch
        {
            // Fall back to opening in browser
            await _jsRuntime.InvokeVoidAsync("window.open", filePath, "_blank");
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<bool> OpenUrlNativeAsync(string url)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("aesirNativeFile.openUrlNative", url);
        }
        catch
        {
            // Fall back to window.open
            await _jsRuntime.InvokeVoidAsync("window.open", url, "_blank");
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<string?> SaveToDownloadsAsync(string filename, byte[] content)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("aesirNativeFile.saveToDownloads", filename, content);
        }
        catch
        {
            // Fall back to browser download
            await _jsRuntime.InvokeVoidAsync("downloadFile",
                $"data:application/octet-stream;base64,{Convert.ToBase64String(content)}",
                filename);
            return filename;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ShowQuickLookAsync(string filePath)
    {
        if (!IsQuickLookAvailable)
        {
            return false;
        }

        try
        {
            return await _jsRuntime.InvokeAsync<bool>("aesirNativeFile.showQuickLook", filePath);
        }
        catch
        {
            // Quick Look not available, fall back to regular open
            return await OpenFileNativeAsync(filePath);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DownloadAndOpenNativeAsync(string url, string filename)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("aesirNativeFile.downloadAndOpenNative", url, filename);
        }
        catch
        {
            // Fall back to opening in browser
            await _jsRuntime.InvokeVoidAsync("window.open", url, "_blank");
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<string?> SaveFileWithDialogAsync(string url, string filename)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("aesirNativeFile.saveFileWithDialog", url, filename);
        }
        catch
        {
            // Fall back to browser download
            await _jsRuntime.InvokeVoidAsync("downloadFile", url, filename);
            return filename;
        }
    }
}

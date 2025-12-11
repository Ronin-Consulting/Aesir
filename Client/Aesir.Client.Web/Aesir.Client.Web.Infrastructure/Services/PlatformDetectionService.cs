using Microsoft.JSInterop;

namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implements platform detection by checking for Tauri runtime environment
/// and browser/OS characteristics via JavaScript interop.
/// </summary>
public class PlatformDetectionService : IPlatformDetectionService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public PlatformDetectionService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public bool IsTauri { get; private set; }

    /// <inheritdoc />
    public bool IsDesktop { get; private set; }

    /// <inheritdoc />
    public bool IsBrowser { get; private set; } = true;

    /// <inheritdoc />
    public bool IsMobile { get; private set; }

    /// <inheritdoc />
    public string OperatingSystem { get; private set; } = "unknown";

    /// <inheritdoc />
    public bool SupportsNativeFileOperations => IsTauri;

    /// <inheritdoc />
    public bool SupportsQuickLook => IsTauri && OperatingSystem == "macos";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_initialized) return;

        try
        {
            var platformInfo = await _jsRuntime.InvokeAsync<PlatformInfo>("aesirPlatform.detect");

            IsTauri = platformInfo.IsTauri;
            IsDesktop = platformInfo.IsDesktop;
            IsMobile = platformInfo.IsMobile;
            IsBrowser = !IsTauri;
            OperatingSystem = platformInfo.OperatingSystem?.ToLowerInvariant() ?? "unknown";

            _initialized = true;
        }
        catch
        {
            // If JavaScript interop fails, assume browser environment
            IsTauri = false;
            IsDesktop = true;
            IsBrowser = true;
            IsMobile = false;
            OperatingSystem = "unknown";
            _initialized = true;
        }
    }

    /// <summary>
    /// Internal record for deserializing JavaScript platform detection results.
    /// </summary>
    private record PlatformInfo
    {
        public bool IsTauri { get; init; }
        public bool IsDesktop { get; init; }
        public bool IsMobile { get; init; }
        public string? OperatingSystem { get; init; }
    }
}

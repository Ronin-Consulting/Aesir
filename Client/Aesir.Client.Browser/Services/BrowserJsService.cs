using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Aesir.Client.Browser.Services;

/// <summary>
/// Provides services for interacting with JavaScript in the browser environment.
/// </summary>
public class BrowserJsService
{
    /// <summary>
    /// The BrowserJsService class provides functionalities to interact with JavaScript hosted within the browser.
    /// </summary>
    public BrowserJsService()
    {
        JSHost.ImportAsync("aesir.js", "../aesir.js");
    }

    /// <summary>
    /// Opens a new browser window with the specified URL.
    /// </summary>
    /// <param name="url">The URL to open in the new window.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the window was successfully opened.</returns>
    public async Task<bool> OpenNewWindowAsync(string url)
    {
        var result = JsInterop.OpenNewWindow(url);
        
        return await Task.FromResult(result);
    }
}

/// <summary>
/// Represents a static partial class that provides JavaScript interop functionality
/// for browser-related operations through the use of JSHost and JSImport mechanisms.
/// </summary>
/// <remarks>
/// This class enables seamless interaction between C# and JavaScript by exposing
/// methods implemented in an external JavaScript file. Specifically, it supports
/// operations such as opening a new browser window via the JavaScript function
/// mapped using JSImport attributes.
/// </remarks>
internal static partial class JsInterop
{
    /// <summary>
    /// Opens a new browser window with the specified URL.
    /// </summary>
    /// <param name="url">The URL to be opened in the new browser window.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the operation was successful.</returns>
    [JSImport("openNewWindow", "aesir.js")]
    public static partial bool OpenNewWindow(string url);
}
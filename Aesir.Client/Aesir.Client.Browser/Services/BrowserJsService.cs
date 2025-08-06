using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Aesir.Client.Browser.Services;

public class BrowserJsService
{   
    public BrowserJsService()
    {
        JSHost.ImportAsync("aesir.js", "../aesir.js");
    }
    
    public async Task<bool> OpenNewWindowAsync(string url)
    {
        var result = JsInterop.OpenNewWindow(url);
        
        return await Task.FromResult(result);
    }
}

internal static partial class JsInterop
{
    [JSImport("openNewWindow", "aesir.js")]
    public static partial bool OpenNewWindow(string url);
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Aesir.Client.Services;

namespace Aesir.Client.Browser.Services;

public class BrowserSpeechService : ISpeechService
{
    public BrowserSpeechService()
    {
        JSHost.ImportAsync("speech.js", "../speech.js");
    }
    
    public async Task SpeakAsync(string text)
    {
        await Task.CompletedTask;
        
        SpeechInterop.SpeakText(text);
    }

    public Task StopSpeaking()
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<string> ListenAsync(Action<int>? silenceDetectedAction = null)
    {
        throw new NotImplementedException();
    }

    public Task StopListening()
    {
        throw new NotImplementedException();
    }
}


internal static partial class SpeechInterop
{
    // [JSImport("globalThis.document.createElement")]
    // public static partial JSObject CreateElement(string tagName);

    [JSImport("speakText", "speech.js")]
    public static partial void SpeakText(string text);
}
Yes! **NativeControlHost** is actually the perfect solution for this - it lets you embed platform-native controls that run on their own threads, completely bypassing Avalonia's single-threaded UI limitation.

## NativeControlHost Approach

Each native control gets its own rendering context and can leverage separate threads for video decoding/rendering:

```csharp
public class NativeVideoPlayer : NativeControlHost
{
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _libVLC = new LibVLC(
            "--no-audio",
            "--avcodec-hw=any" // Hardware acceleration
        );
        
        _mediaPlayer = new MediaPlayer(_libVLC);
        
        // Platform-specific handle creation
        if (OperatingSystem.IsWindows())
        {
            var hwnd = CreateWindowsControl(parent);
            _mediaPlayer.Hwnd = hwnd;
            return new PlatformHandle(hwnd, "HWND");
        }
        else if (OperatingSystem.IsLinux())
        {
            var xid = CreateLinuxControl(parent);
            _mediaPlayer.XWindow = xid;
            return new PlatformHandle(new IntPtr(xid), "XID");
        }
        // Similar for macOS with NSView
        
        throw new PlatformNotSupportedException();
    }
    
    public void SetSource(string url)
    {
        var media = new Media(_libVLC, url, FromType.FromLocation);
        _mediaPlayer.Play(media);
    }
    
    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
        base.DestroyNativeControlCore(control);
    }
}
```

## Key Benefits

**True Parallelism:**
- Each native control runs in its own thread/process context
- Video decoding happens independently per stream
- No contention on Avalonia's UI thread
- GPU can process multiple streams in parallel

**Better Performance:**
- Direct GPU rendering (no bitmap copying to Avalonia)
- Platform-optimized video pipelines
- Lower CPU usage overall

## Alternative: WebView Per Stream

Another interesting approach for RTSP/HLS streams:

```csharp
public class WebVideoPlayer : NativeControlHost
{
    private IWebView _webView; // Using CefSharp or WebViewControl
    
    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        // Create embedded browser showing video player HTML
        _webView = CreatePlatformWebView(parent);
        return _webView.Handle;
    }
    
    public void SetSource(string url)
    {
        _webView.LoadHtml($@"
            <video autoplay muted style='width:100%;height:100%'>
                <source src='{url}' type='video/mp4'>
            </video>
        ");
    }
}
```

This leverages the browser's highly optimized video engine (which uses multiple threads internally).

## Practical Implementation

Your configuration-driven setup would look like:

```csharp
public class VideoGridView : UserControl
{
    private readonly Grid _grid;
    private readonly List<NativeVideoPlayer> _players = new();
    
    public void LoadConfiguration(VideoConfig config)
    {
        // Clear existing
        _grid.Children.Clear();
        _players.ForEach(p => p.Dispose());
        _players.Clear();
        
        // Setup grid layout (e.g., 3x3 for 9 feeds)
        SetupGrid(config.Sources.Count);
        
        // Create native player per source
        foreach (var source in config.Sources)
        {
            var player = new NativeVideoPlayer();
            Grid.SetRow(player, source.Row);
            Grid.SetColumn(player, source.Column);
            
            _grid.Children.Add(player);
            player.SetSource(source.Url);
            _players.Add(player);
        }
    }
}
```

## Important Considerations

**Cross-platform complexity:**
- You'll need platform-specific implementations for Windows/Linux/macOS
- Handle creation differs per platform
- Consider using LibVLCSharp's built-in platform abstractions

**Memory management:**
- Each native control allocates significant resources
- Implement proper disposal patterns
- Monitor total memory usage with many streams

**Layout challenges:**
- Native controls don't always play nice with Avalonia's layout system
- You might need explicit sizing rather than relying on layout passes

**The payoff:** With NativeControlHost, you get true multi-threaded video rendering with minimal overhead on Avalonia's UI thread. Each stream is essentially independent, making 8-10 concurrent feeds very achievable even on modest hardware.

Would you like me to show a more complete example with platform-specific handle creation, or discuss how to handle dynamic adding/removing of streams?
# VA-003: Implement Platform-Specific Video Handles

**Epic**: VISUAL_AGENT_UX
**Phase**: 1 - Foundation
**Priority**: High
**Estimate**: 8 hours

## Description
Implement platform-specific native window handle creation for Windows (HWND), macOS (NSView), and Linux (XID) to enable LibVLC video rendering on each platform.

## Acceptance Criteria
- [ ] `WindowsVideoPlayerHandle` class created for Win32 HWND
- [ ] `MacOsVideoPlayerHandle` class created for NSView
- [ ] `LinuxVideoPlayerHandle` class created for X11 XID
- [ ] Platform detection logic routes to correct implementation
- [ ] LibVLC MediaPlayer correctly receives platform handle
- [ ] Video renders in native control on each platform
- [ ] Handle cleanup properly releases platform resources

## Technical Details

### File Structure
```
Aesir.Client/Aesir.Client.Desktop/Services/LibVlc/
├── IVideoPlayerHandle.cs                    # Common interface
├── WindowsVideoPlayerHandle.cs              # Windows implementation
├── MacOsVideoPlayerHandle.cs                # macOS implementation
└── LinuxVideoPlayerHandle.cs                # Linux implementation
```

### Interface Definition
```csharp
public interface IVideoPlayerHandle
{
    IPlatformHandle CreateHandle(IPlatformHandle parent);
    void AttachMediaPlayer(MediaPlayer mediaPlayer);
    void Dispose();
}
```

### Windows Implementation (HWND)
```csharp
public class WindowsVideoPlayerHandle : IVideoPlayerHandle
{
    private IntPtr _hwnd;

    public IPlatformHandle CreateHandle(IPlatformHandle parent)
    {
        // Create Win32 window using user32.dll
        _hwnd = CreateWindowEx(...);
        return new PlatformHandle(_hwnd, "HWND");
    }

    public void AttachMediaPlayer(MediaPlayer mediaPlayer)
    {
        mediaPlayer.Hwnd = _hwnd;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowEx(...);
}
```

### macOS Implementation (NSView)
```csharp
public class MacOsVideoPlayerHandle : IVideoPlayerHandle
{
    private IntPtr _nsView;

    public IPlatformHandle CreateHandle(IPlatformHandle parent)
    {
        // Create NSView using Objective-C runtime
        _nsView = objc_msgSend(...);
        return new PlatformHandle(_nsView, "NSView");
    }

    public void AttachMediaPlayer(MediaPlayer mediaPlayer)
    {
        mediaPlayer.NsObject = _nsView;
    }
}
```

### Linux Implementation (XID)
```csharp
public class LinuxVideoPlayerHandle : IVideoPlayerHandle
{
    private uint _xid;

    public IPlatformHandle CreateHandle(IPlatformHandle parent)
    {
        // Create X11 window using libX11
        _xid = XCreateWindow(...);
        return new PlatformHandle(new IntPtr(_xid), "XID");
    }

    public void AttachMediaPlayer(MediaPlayer mediaPlayer)
    {
        mediaPlayer.XWindow = _xid;
    }
}
```

### Platform Detection in NativeVideoPlayerControl
```csharp
protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
{
    _libVLC = new LibVLC("--no-audio", "--avcodec-hw=any");
    _mediaPlayer = new MediaPlayer(_libVLC);

    IVideoPlayerHandle handle;
    if (OperatingSystem.IsWindows())
        handle = new WindowsVideoPlayerHandle();
    else if (OperatingSystem.IsMacOS())
        handle = new MacOsVideoPlayerHandle();
    else if (OperatingSystem.IsLinux())
        handle = new LinuxVideoPlayerHandle();
    else
        throw new PlatformNotSupportedException();

    var platformHandle = handle.CreateHandle(parent);
    handle.AttachMediaPlayer(_mediaPlayer);

    return platformHandle;
}
```

## Files to Create
- `Aesir.Client/Aesir.Client.Desktop/Services/LibVlc/IVideoPlayerHandle.cs`
- `Aesir.Client/Aesir.Client.Desktop/Services/LibVlc/WindowsVideoPlayerHandle.cs`
- `Aesir.Client/Aesir.Client.Desktop/Services/LibVlc/MacOsVideoPlayerHandle.cs`
- `Aesir.Client/Aesir.Client.Desktop/Services/LibVlc/LinuxVideoPlayerHandle.cs`

## Files to Modify
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/NativeVideoPlayerControl.axaml.cs`

## Dependencies
- VA-002 (NativeVideoPlayerControl base)

## Testing
- [ ] Video renders on Windows with test RTSP stream
- [ ] Video renders on macOS with test RTSP stream (if available)
- [ ] Video renders on Linux with test RTSP stream (if available)
- [ ] Handle disposal doesn't crash application
- [ ] Multiple instances can coexist
- [ ] Window resizing updates video dimensions

## Notes
- Use conditional compilation (`#if WINDOWS`) if needed
- Reference platform-specific P/Invoke declarations
- Test with FFmpeg-generated RTSP stream (see VIDEO_STREAMING.md)
- Consider thread safety for handle creation/destruction
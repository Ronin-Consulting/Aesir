# VA-002: Create NativeVideoPlayerControl Base Implementation

**Epic**: VISUAL_AGENT_UX
**Phase**: 1 - Foundation
**Priority**: High

## Description
Create the core `NativeVideoPlayerControl` class that inherits from Avalonia's `NativeControlHost` to embed platform-native video players with LibVLC integration.

## Acceptance Criteria
- [ ] `NativeVideoPlayerControl.axaml` created with basic structure
- [ ] `NativeVideoPlayerControl.axaml.cs` implements `NativeControlHost`
- [ ] `CreateNativeControlCore()` method scaffolded with platform detection
- [ ] `DestroyNativeControlCore()` method implements proper cleanup
- [ ] LibVLC and MediaPlayer instances managed correctly
- [ ] Control exposes basic properties: `Source`, `IsPlaying`, `Volume`
- [ ] Control can play a test video file or RTSP stream

## Technical Details

### File Structure
```
Aesir.Client/Aesir.Client/Controls/VideoPlayer/
├── NativeVideoPlayerControl.axaml
└── NativeVideoPlayerControl.axaml.cs
```

### Key Implementation Points

**XAML (NativeVideoPlayerControl.axaml)**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Aesir.Client.Controls.VideoPlayer.NativeVideoPlayerControl">
    <!-- NativeControlHost will be created in code-behind -->
</UserControl>
```

**Code-Behind Structure**
```csharp
public partial class NativeVideoPlayerControl : NativeControlHost
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;

    // Avalonia StyledProperties
    public static readonly StyledProperty<string?> SourceProperty = ...;
    public static readonly StyledProperty<bool> IsPlayingProperty = ...;
    public static readonly StyledProperty<double> VolumeProperty = ...;

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        _libVLC = new LibVLC("--no-audio", "--avcodec-hw=any");
        _mediaPlayer = new MediaPlayer(_libVLC);

        // Platform-specific handle creation (to be implemented in VA-003)
        return CreatePlatformSpecificHandle(parent);
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

### LibVLC Initialization Options
- `--no-audio`: Disable audio (for surveillance feeds)
- `--avcodec-hw=any`: Enable hardware acceleration
- `--network-caching=300`: Set network cache (ms)
- `--rtsp-tcp`: Force RTSP over TCP (more reliable)

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/NativeVideoPlayerControl.axaml`
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/NativeVideoPlayerControl.axaml.cs`

## Dependencies
- VA-001 (LibVLCSharp dependencies)

## Testing
- [ ] Control can be instantiated in a test window
- [ ] Setting `Source` property loads media
- [ ] `IsPlaying` property reflects player state
- [ ] Volume control works (if audio enabled)
- [ ] No memory leaks on control disposal
- [ ] Exception handling for invalid sources

## Notes
- Platform-specific handle creation will be a stub until VA-003
- Focus on cross-platform abstractions in this ticket
- Consider async initialization for LibVLC
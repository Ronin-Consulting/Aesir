# VA-004: Create VideoPlayerViewModel

**Epic**: VISUAL_AGENT_UX
**Phase**: 1 - Foundation
**Priority**: High
**Estimate**: 4 hours

## Description
Create a ViewModel to manage the state and behavior of a single video player instance, including playback controls, status monitoring, and property bindings for the UI.

## Acceptance Criteria
- [ ] `VideoPlayerViewModel` class created inheriting from `ObservableRecipient`
- [ ] Observable properties for: `StreamUrl`, `IsPlaying`, `IsPaused`, `Status`, `FrameRate`, `Latency`
- [ ] Commands implemented: `PlayCommand`, `PauseCommand`, `StopCommand`, `ReloadCommand`
- [ ] Event handlers for MediaPlayer state changes
- [ ] Property change notifications working correctly
- [ ] Error handling for connection failures
- [ ] Integration with `NativeVideoPlayerControl` via data binding

## Technical Details

### File Structure
```
Aesir.Client/Aesir.Client/ViewModels/
└── VideoPlayerViewModel.cs
```

### ViewModel Implementation
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels;

public partial class VideoPlayerViewModel : ObservableRecipient
{
    [ObservableProperty]
    private string? _streamUrl;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private double _frameRate;

    [ObservableProperty]
    private int _latencyMs;

    [ObservableProperty]
    private double _volume = 0.0; // Muted by default for surveillance

    [ObservableProperty]
    private TimeSpan _position;

    [ObservableProperty]
    private TimeSpan _duration;

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (string.IsNullOrEmpty(StreamUrl))
            return;

        Status = "Connecting...";
        // Trigger play via control binding
        IsPlaying = true;
    }

    [RelayCommand]
    private void Pause()
    {
        IsPaused = !IsPaused;
        Status = IsPaused ? "Paused" : "Playing";
    }

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        Status = "Stopped";
        Position = TimeSpan.Zero;
    }

    [RelayCommand]
    private async Task ReloadAsync()
    {
        Stop();
        await Task.Delay(500); // Brief delay before reconnect
        await PlayAsync();
    }

    public void OnPlayerStateChanged(PlayerState state)
    {
        Status = state switch
        {
            PlayerState.Opening => "Opening...",
            PlayerState.Buffering => "Buffering...",
            PlayerState.Playing => "Playing",
            PlayerState.Paused => "Paused",
            PlayerState.Stopped => "Stopped",
            PlayerState.Error => "Error",
            _ => "Unknown"
        };

        IsPlaying = state == PlayerState.Playing;
        IsPaused = state == PlayerState.Paused;
    }

    public void OnPlayerError(string errorMessage)
    {
        Status = $"Error: {errorMessage}";
        IsPlaying = false;
    }

    public void UpdateStatistics(double fps, int latency)
    {
        FrameRate = fps;
        LatencyMs = latency;
    }
}
```

### Property Change Handling
```csharp
partial void OnStreamUrlChanged(string? value)
{
    if (!string.IsNullOrEmpty(value) && IsPlaying)
    {
        // Auto-reload if URL changes while playing
        _ = ReloadAsync();
    }
}

partial void OnVolumeChanged(double value)
{
    // Clamp volume to 0-100
    if (value < 0) Volume = 0;
    if (value > 100) Volume = 100;
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/ViewModels/VideoPlayerViewModel.cs`

## Files to Modify
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/NativeVideoPlayerControl.axaml` (add DataContext binding)
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/NativeVideoPlayerControl.axaml.cs` (wire up ViewModel events)

## Dependencies
- VA-002 (NativeVideoPlayerControl base)

## Testing
- [ ] Property changes trigger UI updates
- [ ] Commands execute without errors
- [ ] PlayCommand starts video playback
- [ ] PauseCommand toggles pause state
- [ ] StopCommand stops playback and resets position
- [ ] ReloadCommand reconnects stream
- [ ] Status property reflects player state accurately
- [ ] Error messages displayed when connection fails

## Integration with Control
```csharp
// In NativeVideoPlayerControl.axaml.cs
public VideoPlayerViewModel? ViewModel
{
    get => DataContext as VideoPlayerViewModel;
    set => DataContext = value;
}

protected override void OnDataContextChanged(EventArgs e)
{
    base.OnDataContextChanged(e);

    if (ViewModel != null)
    {
        // Wire up MediaPlayer events to ViewModel
        _mediaPlayer.Playing += (s, e) =>
            ViewModel.OnPlayerStateChanged(PlayerState.Playing);
        _mediaPlayer.EncounteredError += (s, e) =>
            ViewModel.OnPlayerError(_mediaPlayer.LastError);
    }
}
```

## Notes
- Use CommunityToolkit.Mvvm source generators for boilerplate
- Consider using weak event handlers to prevent memory leaks
- Add logging for state transitions
- Statistics update frequency should be configurable (e.g., 1 second intervals)
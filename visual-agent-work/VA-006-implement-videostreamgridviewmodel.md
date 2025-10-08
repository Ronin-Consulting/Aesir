# VA-006: Implement VideoStreamGridViewModel

**Epic**: VISUAL_AGENT_UX
**Phase**: 2 - Multi-Stream Support
**Priority**: High
**Estimate**: 4 hours

## Description
Create a ViewModel to manage the collection of video streams, grid layout configuration, and coordination of multiple `VideoPlayerViewModel` instances.

## Acceptance Criteria
- [ ] `VideoStreamGridViewModel` created inheriting from `ObservableRecipient`
- [ ] Observable collection of `VideoPlayerViewModel` instances
- [ ] Properties for `CurrentLayout`, `MaxStreams`
- [ ] Commands: `AddStreamCommand`, `RemoveStreamCommand`, `ChangeLayoutCommand`, `ClearAllCommand`
- [ ] Load stream configuration from API/database
- [ ] Automatic grid layout adjustment based on stream count
- [ ] State persistence (save/load configurations)

## Technical Details

### ViewModel Implementation
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels;

public partial class VideoStreamGridViewModel : ObservableRecipient
{
    [ObservableProperty]
    private GridLayout _currentLayout = GridLayout.Single;

    [ObservableProperty]
    private int _maxStreams = 16;

    [ObservableProperty]
    private ObservableCollection<VideoPlayerViewModel> _videoStreams = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _configurationName;

    private readonly IVideoStreamService _videoStreamService;

    public VideoStreamGridViewModel(IVideoStreamService videoStreamService)
    {
        _videoStreamService = videoStreamService;

        // Automatically adjust layout when stream count changes
        VideoStreams.CollectionChanged += (s, e) => AutoAdjustLayout();
    }

    [RelayCommand]
    private async Task AddStreamAsync(string? streamUrl = null)
    {
        if (VideoStreams.Count >= MaxStreams)
        {
            // Show error: Max streams reached
            return;
        }

        var viewModel = new VideoPlayerViewModel
        {
            StreamUrl = streamUrl
        };

        VideoStreams.Add(viewModel);

        if (!string.IsNullOrEmpty(streamUrl))
        {
            await viewModel.PlayCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand]
    private void RemoveStream(VideoPlayerViewModel stream)
    {
        stream.StopCommand.Execute(null);
        VideoStreams.Remove(stream);
    }

    [RelayCommand]
    private void ChangeLayout(GridLayout newLayout)
    {
        CurrentLayout = newLayout;

        // Trim streams if new layout has fewer slots
        int maxSlots = (int)newLayout;
        while (VideoStreams.Count > maxSlots)
        {
            RemoveStream(VideoStreams[VideoStreams.Count - 1]);
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var stream in VideoStreams)
        {
            stream.StopCommand.Execute(null);
        }

        VideoStreams.Clear();
    }

    [RelayCommand]
    private async Task LoadConfigurationAsync(Guid configId)
    {
        IsLoading = true;
        try
        {
            var config = await _videoStreamService.GetConfigurationAsync(configId);
            ConfigurationName = config.Name;

            ClearAll();

            foreach (var source in config.VideoSources)
            {
                await AddStreamAsync(source.Url);
            }

            ChangeLayout(config.Layout);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveConfigurationAsync()
    {
        var config = new VideoGridConfiguration
        {
            Name = ConfigurationName ?? "Untitled",
            Layout = CurrentLayout,
            VideoSources = VideoStreams
                .Select(vm => new VideoSourceConfig { Url = vm.StreamUrl })
                .ToList()
        };

        await _videoStreamService.SaveConfigurationAsync(config);
    }

    private void AutoAdjustLayout()
    {
        var streamCount = VideoStreams.Count;

        if (streamCount <= 1)
            CurrentLayout = GridLayout.Single;
        else if (streamCount <= 4)
            CurrentLayout = GridLayout.FourUp;
        else if (streamCount <= 9)
            CurrentLayout = GridLayout.NineUp;
        else
            CurrentLayout = GridLayout.SixteenUp;
    }

    public void StopAllStreams()
    {
        foreach (var stream in VideoStreams)
        {
            stream.StopCommand.Execute(null);
        }
    }

    protected override void OnDeactivated()
    {
        StopAllStreams();
        base.OnDeactivated();
    }
}
```

### Supporting Models
```csharp
public class VideoGridConfiguration
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public GridLayout Layout { get; set; }
    public List<VideoSourceConfig> VideoSources { get; set; }
}

public class VideoSourceConfig
{
    public string Url { get; set; }
    public string? Name { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/ViewModels/VideoStreamGridViewModel.cs`
- `Aesir.Client/Aesir.Client/Models/VideoGridConfiguration.cs`
- `Aesir.Client/Aesir.Client/Models/VideoSourceConfig.cs`

## Files to Modify
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/VideoStreamGrid.axaml` (bind to ViewModel)

## Dependencies
- VA-004 (VideoPlayerViewModel)
- VA-005 (VideoStreamGrid control)

## Testing
- [ ] AddStreamCommand adds new stream to collection
- [ ] RemoveStreamCommand removes stream and stops playback
- [ ] ChangeLayoutCommand switches grid layout
- [ ] ClearAllCommand stops all streams and clears collection
- [ ] AutoAdjustLayout selects appropriate grid size
- [ ] LoadConfigurationAsync restores saved configuration
- [ ] SaveConfigurationAsync persists configuration
- [ ] MaxStreams limit enforced
- [ ] Collection changes propagate to UI

## Notes
- Consider adding validation for stream URLs
- Implement retry logic for failed stream connections
- Add telemetry for stream health monitoring
- Consider stream priorities for bandwidth management
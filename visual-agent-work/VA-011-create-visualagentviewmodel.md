# VA-011: Create VisualAgentViewModel

**Epic**: VISUAL_AGENT_UX
**Phase**: 4 - Visual Agent Integration
**Priority**: High

## Description
Create the main orchestrating ViewModel that integrates video streaming, detection overlays, and API communication for visual agent functionality.

## Acceptance Criteria
- [ ] `VisualAgentViewModel` created
- [ ] Properties: `SelectedAgent`, `AnalyticsEnabled`, `CurrentDetections`, `EventHistory`
- [ ] Commands: `StartAnalyticsCommand`, `StopAnalyticsCommand`, `ConfigureAgentCommand`
- [ ] Integration with `VideoStreamGridViewModel`
- [ ] Subscribe to real-time analytics updates
- [ ] Update detection overlays from API stream
- [ ] Event history management (rolling window)

## Technical Details
```csharp
public partial class VisualAgentViewModel : ObservableRecipient
{
    [ObservableProperty]
    private AesirAgent? _selectedAgent;

    [ObservableProperty]
    private bool _analyticsEnabled;

    [ObservableProperty]
    private ObservableCollection<Detection> _currentDetections = new();

    [ObservableProperty]
    private ObservableCollection<AnalyticsEvent> _eventHistory = new();

    [ObservableProperty]
    private VideoStreamGridViewModel _videoGridViewModel;

    private readonly IVisualAgentService _visualAgentService;
    private IDisposable? _analyticsSubscription;

    [RelayCommand]
    private async Task StartAnalyticsAsync()
    {
        if (SelectedAgent == null) return;

        AnalyticsEnabled = true;

        _analyticsSubscription = await _visualAgentService
            .SubscribeToAnalyticsAsync(SelectedAgent.Id.Value, OnAnalyticsUpdate);
    }

    private void OnAnalyticsUpdate(VisualAgentResult result)
    {
        // Update detections on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            CurrentDetections.Clear();
            foreach (var detection in result.Detections)
            {
                CurrentDetections.Add(detection);
            }

            foreach (var evt in result.Events)
            {
                EventHistory.Insert(0, evt);
                if (EventHistory.Count > 100)
                    EventHistory.RemoveAt(EventHistory.Count - 1);
            }
        });
    }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/ViewModels/VisualAgentViewModel.cs`

## Dependencies
- VA-006 (VideoStreamGridViewModel)
- DEEPSTREAM_VISUAL_AGENT.md (API integration)

## Testing
- [ ] StartAnalytics subscribes to stream
- [ ] Detections update in real-time
- [ ] Event history maintains size limit
- [ ] StopAnalytics cleans up subscription
- [ ] Multiple agents can be switched
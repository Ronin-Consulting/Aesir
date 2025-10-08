# VA-005: Create VideoStreamGrid Control

**Epic**: VISUAL_AGENT_UX
**Phase**: 2 - Multi-Stream Support
**Priority**: High
**Estimate**: 5 hours

## Description
Create a grid container control that can host multiple `NativeVideoPlayerControl` instances with configurable layouts (1x1, 2x2, 3x3, etc.) for simultaneous multi-stream video display.

## Acceptance Criteria
- [ ] `VideoStreamGrid.axaml` created with responsive grid layout
- [ ] `VideoStreamGrid.axaml.cs` manages child video player controls
- [ ] Dynamic grid row/column configuration
- [ ] Support for 1x1, 2x2, 3x3, 4x4 layouts
- [ ] Proper sizing and spacing of video players
- [ ] Add/remove players dynamically
- [ ] Grid responds to window resize events

## Technical Details

### File Structure
```
Aesir.Client/Aesir.Client/Controls/VideoPlayer/
├── VideoStreamGrid.axaml
└── VideoStreamGrid.axaml.cs
```

### XAML Structure
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="clr-namespace:Aesir.Client.Controls.VideoPlayer"
             x:Class="Aesir.Client.Controls.VideoPlayer.VideoStreamGrid">
    <Grid Name="PlayerGrid" Background="#1a1a1a">
        <!-- Dynamic grid rows/columns defined in code-behind -->
        <!-- Video players added programmatically -->
    </Grid>
</UserControl>
```

### Grid Layout Enum
```csharp
public enum GridLayout
{
    Single = 1,      // 1x1
    FourUp = 4,      // 2x2
    NineUp = 9,      // 3x3
    SixteenUp = 16   // 4x4
}
```

### Code-Behind Implementation
```csharp
public partial class VideoStreamGrid : UserControl
{
    public static readonly StyledProperty<GridLayout> LayoutProperty =
        AvaloniaProperty.Register<VideoStreamGrid, GridLayout>(
            nameof(Layout),
            GridLayout.Single);

    public GridLayout Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    private readonly List<NativeVideoPlayerControl> _players = new();

    public VideoStreamGrid()
    {
        InitializeComponent();
        this.GetObservable(LayoutProperty).Subscribe(UpdateGridLayout);
    }

    private void UpdateGridLayout(GridLayout layout)
    {
        int gridSize = (int)Math.Sqrt((int)layout);

        PlayerGrid.RowDefinitions.Clear();
        PlayerGrid.ColumnDefinitions.Clear();

        for (int i = 0; i < gridSize; i++)
        {
            PlayerGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            PlayerGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
    }

    public void AddPlayer(NativeVideoPlayerControl player, int row, int column)
    {
        Grid.SetRow(player, row);
        Grid.SetColumn(player, column);
        player.Margin = new Thickness(2); // Spacing between players

        PlayerGrid.Children.Add(player);
        _players.Add(player);
    }

    public void RemovePlayer(NativeVideoPlayerControl player)
    {
        PlayerGrid.Children.Remove(player);
        _players.Remove(player);
        player.Dispose();
    }

    public void ClearAllPlayers()
    {
        foreach (var player in _players)
        {
            player.Dispose();
        }

        PlayerGrid.Children.Clear();
        _players.Clear();
    }

    public void SetPlayerSource(int index, string streamUrl)
    {
        if (index >= 0 && index < _players.Count)
        {
            _players[index].Source = streamUrl;
        }
    }
}
```

### Responsive Sizing
```csharp
protected override void OnSizeChanged(SizeChangedEventArgs e)
{
    base.OnSizeChanged(e);

    // Maintain aspect ratio for video players if needed
    foreach (var player in _players)
    {
        // Adjust player dimensions based on grid size
    }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/VideoStreamGrid.axaml`
- `Aesir.Client/Aesir.Client/Controls/VideoPlayer/VideoStreamGrid.axaml.cs`
- `Aesir.Client/Aesir.Client/Models/GridLayout.cs` (enum)

## Dependencies
- VA-002 (NativeVideoPlayerControl)
- VA-004 (VideoPlayerViewModel)

## Testing
- [ ] Single player displays correctly (1x1)
- [ ] Four players display in 2x2 grid
- [ ] Nine players display in 3x3 grid
- [ ] Grid layout switches without crashes
- [ ] Players resize proportionally with window
- [ ] Adding/removing players works dynamically
- [ ] Memory cleanup on ClearAllPlayers()
- [ ] Grid spacing looks correct

## Notes
- Consider adding borders/labels to identify streams
- May need double buffering for smooth resizing
- Test performance with maximum number of streams
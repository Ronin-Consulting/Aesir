# VA-010: Add Coordinate Transformation System

**Epic**: VISUAL_AGENT_UX
**Phase**: 3 - Detection Overlay
**Priority**: High

## Description
Implement coordinate transformation to accurately map detection coordinates from video space (e.g., 1920x1080) to screen space (actual control dimensions).

## Acceptance Criteria
- [ ] Transform video coordinates to screen coordinates
- [ ] Handle aspect ratio differences (letterboxing/pillarboxing)
- [ ] Account for video scaling and cropping
- [ ] Update transformations on window resize
- [ ] Maintain accuracy across different video resolutions

## Technical Details
```csharp
public class CoordinateTransformer
{
    private Size _videoSize;
    private Size _controlSize;

    public Point TransformToScreen(Point videoPoint)
    {
        double scaleX = _controlSize.Width / _videoSize.Width;
        double scaleY = _controlSize.Height / _videoSize.Height;

        // Handle letterboxing/pillarboxing
        double scale = Math.Min(scaleX, scaleY);

        double offsetX = (_controlSize.Width - _videoSize.Width * scale) / 2;
        double offsetY = (_controlSize.Height - _videoSize.Height * scale) / 2;

        return new Point(
            videoPoint.X * scale + offsetX,
            videoPoint.Y * scale + offsetY
        );
    }

    public Rect TransformToScreen(Rect videoRect) { ... }
}
```

## Files to Create
- `Aesir.Client/Aesir.Client/Services/CoordinateTransformer.cs`

## Dependencies
- VA-008 (BoundingBoxCanvas)

## Testing
- [ ] Coordinates accurate at various window sizes
- [ ] Letterboxing handled correctly
- [ ] Transformation updates on resize
- [ ] Accuracy within 2-pixel tolerance